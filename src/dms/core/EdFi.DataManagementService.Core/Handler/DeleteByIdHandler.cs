// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.DataManagementService.Core.Backend;
using EdFi.DataManagementService.Core.External.Interface;
using EdFi.DataManagementService.Core.Model;
using EdFi.DataManagementService.Core.Pipeline;
using EdFi.DataManagementService.Core.Response;
using EdFi.DataManagementService.Core.Security;
using Microsoft.Extensions.Logging;
using Polly;
using static EdFi.DataManagementService.Core.External.Backend.DeleteResult;
using static EdFi.DataManagementService.Core.Handler.Utility;

namespace EdFi.DataManagementService.Core.Handler;

/// <summary>
/// Handles a delete request that has made it through the middleware pipeline steps.
/// </summary>
internal class DeleteByIdHandler(
    IDocumentStoreRepository _documentStoreRepository,
    ILogger _logger,
    ResiliencePipeline _resiliencePipeline,
    IAuthorizationServiceFactory authorizationServiceFactory
) : IPipelineStep
{
    public async Task Execute(PipelineContext context, Func<Task> next)
    {
        _logger.LogDebug("Entering DeleteByIdHandler - {TraceId}", context.FrontendRequest.TraceId.Value);

        var deleteResult = await _resiliencePipeline.ExecuteAsync(async t =>
            await _documentStoreRepository.DeleteDocumentById(
                new DeleteRequest(
                    DocumentUuid: context.PathComponents.DocumentUuid,
                    ResourceInfo: context.ResourceInfo,
                    TraceId: context.FrontendRequest.TraceId,
                    ResourceAuthorizationHandler: new ResourceAuthorizationHandler(
                        context.AuthorizationStrategyEvaluators,
                        context.AuthorizationSecurableInfo,
                        authorizationServiceFactory,
                        _logger
                    ),
                    ResourceAuthorizationPathways: context.AuthorizationPathways,
                    DeleteInEdOrgHierarchy: (
                        context.ProjectSchema.EducationOrganizationTypes.Contains(
                            context.ResourceSchema.ResourceName
                        )
                    )
                )
            )
        );

        _logger.LogDebug(
            "Document store DeleteDocumentById returned {DeleteResult}- {TraceId}",
            deleteResult.GetType().FullName,
            context.FrontendRequest.TraceId.Value
        );

        context.FrontendResponse = deleteResult switch
        {
            DeleteSuccess => new FrontendResponse(StatusCode: 204, Body: null, Headers: []),
            DeleteFailureNotExists => new FrontendResponse(StatusCode: 404, Body: null, Headers: []),
            DeleteFailureNotAuthorized notAuthorized => new FrontendResponse(
                StatusCode: 403,
                Body: FailureResponse.ForForbidden(
                    traceId: context.FrontendRequest.TraceId,
                    errors: notAuthorized.ErrorMessages
                ),
                Headers: []
            ),
            DeleteFailureReference failure => new FrontendResponse(
                StatusCode: 409,
                Body: FailureResponse.ForDataConflict(
                    failure.ReferencingDocumentResourceNames,
                    traceId: context.FrontendRequest.TraceId
                ),
                Headers: []
            ),
            DeleteFailureWriteConflict => new FrontendResponse(StatusCode: 409, Body: null, Headers: []),
            UnknownFailure failure => new(
                StatusCode: 500,
                Body: ToJsonError(failure.FailureMessage, context.FrontendRequest.TraceId),
                Headers: []
            ),
            _ => new(
                StatusCode: 500,
                Body: ToJsonError("Unknown DeleteResult", context.FrontendRequest.TraceId),
                Headers: []
            ),
        };
    }
}
