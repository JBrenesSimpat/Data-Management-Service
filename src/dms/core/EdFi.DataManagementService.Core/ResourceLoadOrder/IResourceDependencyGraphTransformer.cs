// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using QuickGraph;

namespace EdFi.DataManagementService.Core.ResourceLoadOrder;

/// <summary>
/// Provides a mean to modify the vertices and edges of the resource dependency graph.
/// </summary>
internal interface IResourceDependencyGraphTransformer
{
    /// <summary>
    /// A hook to modify the vertices and edges of the resource dependency graph <b>before</b>
    /// executing topological sorting.
    /// </summary>
    void Transform(
        BidirectionalGraph<ResourceDependencyGraphVertex, ResourceDependencyGraphEdge> resourceGraph
    );
}
