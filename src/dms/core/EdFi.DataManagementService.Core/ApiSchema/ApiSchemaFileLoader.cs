// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Reflection;
using System.Text.Json.Nodes;
using EdFi.DataManagementService.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EdFi.DataManagementService.Core.ApiSchema;

/// <summary>
/// Loads and parses the ApiSchema.json from a file.
/// </summary>
internal class ApiSchemaFileLoader(ILogger<ApiSchemaFileLoader> _logger, IOptions<AppSettings> appSettings)
    : IApiSchemaProvider
{
    private readonly Lazy<JsonNode> _apiSchemaRootNode =
        new(() =>
        {
            _logger.LogDebug("Entering ApiSchemaFileLoader");

            string jsonContent;

            if (appSettings.Value.UseLocalApiSchemaJson)
            {
                jsonContent = File.ReadAllText(
                    Path.Combine(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "",
                        "ApiSchema",
                        "ApiSchema.json"
                    )
                );
            }
            else
            {
                var assembly =
                    Assembly.GetAssembly(typeof(DataStandard51.ApiSchema.Marker))
                    ?? throw new InvalidOperationException("Could not load the ApiSchema library");

                var resourceName = assembly
                    .GetManifestResourceNames()
                    .Single(str => str.EndsWith("ApiSchema.json"));

                using Stream stream =
                    assembly.GetManifestResourceStream(resourceName)
                    ?? throw new InvalidOperationException("Could not load ApiSchema resource");
                using StreamReader reader = new(stream);

                jsonContent = reader.ReadToEnd();
            }

            JsonNode? rootNodeFromFile = JsonNode.Parse(jsonContent);
            if (rootNodeFromFile == null)
            {
                _logger.LogCritical("Unable to read and parse Api Schema file");
                throw new InvalidOperationException("Unable to read and parse Api Schema file.");
            }
            return rootNodeFromFile;
        });

    public JsonNode ApiSchemaRootNode => _apiSchemaRootNode.Value;
}
