// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

// ReSharper disable ClassNeverInstantiated.Global

using System.Data;
using System.Data.Common;
using System.Text.Json.Serialization;

namespace EdFi.DmsConfigurationService.Backend.Repositories;

public interface IClaimsHierarchyRepository
{
    Task<ClaimsHierarchyGetResult> GetClaimsHierarchy();

    Task<ClaimsHierarchySaveResult> SaveClaimsHierarchy(
        List<Claim> claimsHierarchy,
        DateTime existingLastModifiedDate,
        DbTransaction? transaction = null
    );
}

public abstract record ClaimsHierarchyGetResult
{
    /// <summary>
    /// Successfully loaded and deserialized the claim set hierarchy.
    /// </summary>
    /// <param name="Claims"></param>
    public record Success(List<Claim> Claims, DateTime LastModifiedDate) : ClaimsHierarchyGetResult;

    /// <summary>
    /// Unexpected exception thrown and caught.
    /// </summary>
    public record FailureUnknown(string FailureMessage) : ClaimsHierarchyGetResult;

    /// <summary>
    /// There was more than one claims hierarchy, which is all that is currently expected.
    /// </summary>
    public record FailureHierarchyNotFound() : ClaimsHierarchyGetResult;

    /// <summary>
    /// There was more than one claims hierarchy, which is all that is currently expected.
    /// </summary>
    public record FailureMultipleHierarchiesFound() : ClaimsHierarchyGetResult;
}

public abstract record ClaimsHierarchySaveResult
{
    /// <summary>
    /// Successfully saved the claim set hierarchy.
    /// </summary>
    public record Success() : ClaimsHierarchySaveResult;

    /// <summary>
    /// Unexpected exception thrown and caught.
    /// </summary>
    public record FailureUnknown(string FailureMessage) : ClaimsHierarchySaveResult;

    /// <summary>
    /// The claims hierarchy was modified by another user.
    /// </summary>
    public record FailureMultiUserConflict() : ClaimsHierarchySaveResult;

    /// <summary>
    /// There was more than one claims hierarchy, which is all that is currently expected.
    /// </summary>
    public record FailureHierarchyNotFound() : ClaimsHierarchySaveResult;

    /// <summary>
    /// There was more than one claims hierarchy, which is all that is currently expected.
    /// </summary>
    public record FailureMultipleHierarchiesFound() : ClaimsHierarchySaveResult;
}

public class Claim
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("defaultAuthorization")]
    public DefaultAuthorization? DefaultAuthorization { get; set; }

    [JsonPropertyName("claimSets")]
    public List<ClaimSet> ClaimSets { get; set; } = [];

    private List<Claim> _claims = new();

    [JsonPropertyName("claims")]
    public List<Claim> Claims
    {
        get => _claims;
        set
        {
            _claims = value;

            // Provide navigability up the claims hierarchy
            foreach (Claim claim in _claims)
            {
                claim.Parent = this;
            }
        }
    }

    [JsonIgnore]
    public Claim? Parent { get; set; }
}

public class DefaultAuthorization
{
    [JsonPropertyName("actions")]
    public List<DefaultAction> Actions { get; set; } = [];
}

public class DefaultAction
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("authorizationStrategies")]
    public List<AuthorizationStrategy> AuthorizationStrategies { get; set; } = [];
}

public class ClaimSetAction
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("authorizationStrategyOverrides")]
    public List<AuthorizationStrategy> AuthorizationStrategyOverrides { get; set; } = [];
}

public class AuthorizationStrategy
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
}

public class ClaimSet
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("actions")]
    public List<ClaimSetAction> Actions { get; set; } = [];
}
