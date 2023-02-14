// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using PermissionsScraper.Common;
using PermissionsScraper.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;

public class OpenApiPathsService
{
    public async Task<OpenApiDocument> FetchOpenApiDocument()
    {
        var httpClient = HttpClientSingleton.Instance.HttpClient;
        var response = await httpClient.GetAsync("https://graphexplorerapi.azurewebsites.net/openapi?operationIds=*&format=json");
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var openApiDocument = JsonConvert.DeserializeObject<OpenApiDocument>(content);
            return openApiDocument;
        }

        return null;
    }

    /// <summary>
    /// Retrieves all paths and their operationTypes from an OpenAPI document.
    /// </summary>
    /// <param name="doc"></param>
    /// <returns>A dictionary of pathItems with the corresponding operation types supported.</returns>
    public Dictionary<string, List<string>> RetrievePathsFromOpenApiDocument(OpenApiDocument doc)
    {
        var pathItems = new Dictionary<string, List<string>>();
        
        // loop through the document and fetch paths
        foreach (var pathItem in doc.Paths)
        {
            string path = pathItem.Key;
            var operationTypes = new List<string>();

            // sanitize url
            path = path.RemoveIdPrefixes()
                       .UriTemplatePathFormat(true)
                       .ToLower();

            // loop through pathItem and fetch supported operation types
            foreach (var operation in pathItem.Value.Operations)
            {                
                var operationType = operation.Key.ToString().ToLower();
                operationTypes.Add(operationType);
            }

            pathItems.TryAdd(path, operationTypes);
        }

        return pathItems;
    }
        
}
