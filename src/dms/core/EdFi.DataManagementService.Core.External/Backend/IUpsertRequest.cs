// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.
using System.Text.Json.Nodes;
using EdFi.DataManagementService.Core.External.Model;

namespace EdFi.DataManagementService.Core.External.Backend;

/// <summary>
/// An upsert request to a document repository. This extends UpdateRequest because
/// sometimes upserts are actually updates.
/// </summary>
public interface IUpsertRequest
{
    /// <summary>
    /// The ResourceInfo of the document to upsert
    /// </summary>
    ResourceInfo ResourceInfo { get; }

    /// <summary>
    /// The DocumentInfo of the document to upsert
    /// </summary>
    DocumentInfo DocumentInfo { get; }

    /// <summary>
    /// The EdfiDoc of the document to upsert, as a JsonNode
    /// </summary>
    JsonNode EdfiDoc { get; }

    /// <summary>
    /// Request Header provided by the frontend service as a dictionary
    /// </summary>
    Dictionary<string, string> Headers { get; }

    /// <summary>
    /// The elements extracted from the document that are being secured on
    /// </summary>
    DocumentSecurityElements DocumentSecurityElements { get; }

    /// <summary>
    /// The request TraceId
    /// </summary>
    TraceId TraceId { get; }

    /// <summary>
    /// A candidate DocumentUuid of the document to upsert, used only
    /// if the upsert happens as an insert
    /// </summary>
    DocumentUuid DocumentUuid { get; }

    /// <summary>
    /// This callback will modify the EdFiDoc of a referencing
    /// resource when the referenced resource's identifying
    /// values are modified
    /// </summary>
    IUpdateCascadeHandler UpdateCascadeHandler { get; }

    /// <summary>
    /// The backend should use this handler to determine whether the client is
    /// authorized to upsert the document
    /// </summary>
    IResourceAuthorizationHandler ResourceAuthorizationHandler { get; }
}
