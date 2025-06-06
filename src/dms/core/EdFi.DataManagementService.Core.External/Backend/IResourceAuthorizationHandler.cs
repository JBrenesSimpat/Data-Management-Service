// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.DataManagementService.Core.External.Model;

namespace EdFi.DataManagementService.Core.External.Backend;

/// <summary>
/// A handler to determine if the client is authorized for the resource.
/// </summary>
public interface IResourceAuthorizationHandler
{
    /// <summary>
    /// Determines whether a client is authorized to perform a specific operation on a resource.
    /// </summary>
    /// <returns></returns>
    Task<ResourceAuthorizationResult> Authorize(
        DocumentSecurityElements documentSecurityElements,
        OperationType operationType,
        TraceId traceId
    );
}
