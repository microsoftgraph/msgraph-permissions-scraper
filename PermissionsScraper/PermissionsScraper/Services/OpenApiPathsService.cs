// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using PermissionsScraper.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

public class OpenApiPathsService
{
    public async Task<OpenApiDocument> FetchOpenApiDocument(string fileUrl)
    {
        var httpClient = HttpClientSingleton.Instance.HttpClient;
        var response = await httpClient.GetAsync(fileUrl);
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
    public async Task<Dictionary<string, List<string>>> RetrievePathsFromOpenApiDocument(string fileUrl)
    {
        var doc = await FetchOpenApiDocument(fileUrl);
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

    /// <summary>
    /// Gets a serialized dictionary of paths and http methods extracted from a file at the given url. 
    /// </summary>
    /// <param name="fileUrl">The url of the permissions file.</param>
    /// <returns>A serialized dictionary of paths and http methods extracted from the permissions file located at the provided <paramref name="fileUrl"/>.</returns>
    public async Task<string> GetSerializedPathsDictionaryFromOpenApiFileUrlAsync(string fileUrl)
    {
        var pathsDictionary = await RetrievePathsFromOpenApiDocument(fileUrl);
        return JsonConvert.SerializeObject(pathsDictionary, Formatting.Indented);
    }

}
