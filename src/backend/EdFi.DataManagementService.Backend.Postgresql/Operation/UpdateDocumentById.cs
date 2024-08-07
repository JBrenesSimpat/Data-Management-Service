// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.Json;
using EdFi.DataManagementService.Backend.Postgresql.Model;
using EdFi.DataManagementService.Core.External.Backend;
using EdFi.DataManagementService.Core.External.Model;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Npgsql;
using static EdFi.DataManagementService.Backend.PartitionUtility;
using static EdFi.DataManagementService.Backend.Postgresql.ReferenceHelper;
using Document = EdFi.DataManagementService.Backend.Postgresql.Model.Document;

namespace EdFi.DataManagementService.Backend.Postgresql.Operation;

public interface IUpdateDocumentById
{
    public Task<UpdateResult> UpdateById(
        IUpdateRequest updateRequest,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction
    );
}

public class UpdateDocumentById(ISqlAction _sqlAction, ILogger<UpdateDocumentById> _logger)
    : IUpdateDocumentById
{

    private static readonly string _beforeInsertReferences = "BeforeInsertReferences";

    /// <summary>
    /// Takes an UpdateRequest and connection + transaction and returns the result of an update operation.
    ///
    /// Connections and transactions are always managed by the caller based on the result.
    /// </summary>
    public async Task<UpdateResult> UpdateById(
        IUpdateRequest updateRequest,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction
    )
    {
        _logger.LogDebug("Entering UpdateDocumentById.UpdateById - {TraceId}", updateRequest.TraceId);
        var documentPartitionKey = PartitionKeyFor(updateRequest.DocumentUuid);

        DocumentReferenceIds documentReferenceIds = DocumentReferenceIdsFrom(updateRequest.DocumentInfo.DocumentReferences);
        try
        {
            var validationResult = await _sqlAction.UpdateDocumentValidation(
                updateRequest.DocumentUuid,
                documentPartitionKey,
                updateRequest.DocumentInfo.ReferentialId,
                PartitionKeyFor(updateRequest.DocumentInfo.ReferentialId),
                connection,
                transaction,
                LockOption.BlockAll
            );

            if (!validationResult.DocumentExists)
            {
                // Document does not exist
                return new UpdateResult.UpdateFailureNotExists();
            }

            if (!validationResult.ReferentialIdExists)
            {
                // Extracted referential id does not match stored. Must be attempting to change natural key.
                _logger.LogInformation(
                    "Failure: Natural key does not match on update - {TraceId}",
                    updateRequest.TraceId
                );
                return new UpdateResult.UpdateFailureImmutableIdentity(
                    $"Identifying values for the {updateRequest.ResourceInfo.ResourceName.Value} resource cannot be changed. Delete and recreate the resource item instead."
                );
            }

            int rowsAffected = await _sqlAction.UpdateDocumentEdfiDoc(
                PartitionKeyFor(updateRequest.DocumentUuid).Value,
                updateRequest.DocumentUuid.Value,
                JsonSerializer.Deserialize<JsonElement>(updateRequest.EdfiDoc),
                connection,
                transaction
            );

            switch (rowsAffected)
            {
                case 1:
                    if (documentReferenceIds.ReferentialIds.Length > 0)
                    {
                        Document? documentFromDb;
                        try
                        {
                            // Attempt to get the document, to get the ID for references
                            documentFromDb = await _sqlAction.FindDocumentByReferentialId(
                                updateRequest.DocumentInfo.ReferentialId,
                                PartitionKeyFor(updateRequest.DocumentInfo.ReferentialId),
                                connection,
                                transaction,
                                LockOption.BlockUpdateDelete
                            );
                        }
                        catch (PostgresException pe) when (pe.SqlState == PostgresErrorCodes.SerializationFailure)
                        {
                            _logger.LogDebug(
                                pe,
                                "Transaction conflict on Documents table read - {TraceId}",
                                updateRequest.TraceId
                            );
                            return new UpdateResult.UpdateFailureWriteConflict();
                        }

                        long documentId = 0;
                        if (documentFromDb != null)
                        {
                            documentId =
                                documentFromDb.Id
                                ?? throw new InvalidOperationException("documentFromDb.Id should never be null");
                        }

                        await _sqlAction.DeleteReferencesByDocumentUuid(
                        documentPartitionKey.Value,
                        updateRequest.DocumentUuid.Value,
                            connection,
                            transaction
                        );

                        // Create a transaction savepoint in case insert into References fails due to invalid references
                        await transaction.SaveAsync(_beforeInsertReferences);
                        int numberOfRowsInserted = await _sqlAction.InsertReferences(
                            new(
                                ParentDocumentPartitionKey: documentPartitionKey.Value,
                                ParentDocumentId: documentId,
                                ReferentialIds: documentReferenceIds.ReferentialIds,
                                ReferentialPartitionKeys: documentReferenceIds.ReferentialPartitionKeys
                            ),
                            connection,
                            transaction
                        );

                        if (numberOfRowsInserted != documentReferenceIds.ReferentialIds.Length)
                        {
                            throw new InvalidOperationException("Database did not insert all references");
                        }
                    }

                    return new UpdateResult.UpdateSuccess(updateRequest.DocumentUuid);

                case 0:
                    _logger.LogInformation(
                        "Failure: Record to update does not exist - {TraceId}",
                        updateRequest.TraceId
                    );
                    return new UpdateResult.UpdateFailureNotExists();
                default:
                    _logger.LogCritical(
                        "UpdateDocumentById rows affected was '{RowsAffected}' for {DocumentUuid} - Should never happen - {TraceId}",
                        rowsAffected,
                        updateRequest.DocumentUuid,
                        updateRequest.TraceId
                    );
                    return new UpdateResult.UnknownFailure("Unknown Failure");
            }
        }
        catch (PostgresException pe) when (pe.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            _logger.LogDebug(pe, "Transaction conflict on UpdateById - {TraceId}", updateRequest.TraceId);
            return new UpdateResult.UpdateFailureWriteConflict();
        }
        catch (PostgresException pe)
            when (pe.SqlState == PostgresErrorCodes.ForeignKeyViolation
                  && pe.ConstraintName == SqlAction.FK_Reference_ReferenceAlias
                 )
        {
            _logger.LogDebug(pe, "Foreign key violation on Update - {TraceId}", updateRequest.TraceId);

            // Restore transaction savepoint to continue using transaction
            await transaction.RollbackAsync(_beforeInsertReferences);

            Guid[] invalidReferentialIds = await _sqlAction.FindInvalidReferentialIds(
                documentReferenceIds,
                connection,
                transaction
            );

            ResourceName[] invalidResourceNames = ResourceNamesFrom(
                updateRequest.DocumentInfo.DocumentReferences,
                invalidReferentialIds
            );

            return new UpdateResult.UpdateFailureReference(invalidResourceNames);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failure on Documents table update - {TraceId}", updateRequest.TraceId);
            return new UpdateResult.UnknownFailure("Update failure");
        }
    }
}
