// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Newtonsoft.Json.Linq;
using PermissionsScraper.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PermissionsScraper.Services
{
    /// <summary>
    /// A service for retrieving a dictionary of paths and http methods extracted from a permissions file.
    /// </summary>
    public class PermissionsFilePathsService
    {
        /// <summary>
        /// Gets the permission file from a url.
        /// </summary>
        /// <param name="fileUrl">The url of the permissions file.</param>
        /// <returns>The file contents of the permissions file.</returns>
        /// <exception cref="ArgumentNullException">Exception thrown when the <paramref name="fileUrl"/> is empty or null.</exception>
        public async Task<string> GetPermissionsFileFromUrl(string fileUrl)
        {
            UtilityFunctions.CheckArgumentNullOrEmpty(fileUrl, nameof(fileUrl));
           
            var client = HttpClientSingleton.Instance;
            using var response = await client.HttpClient.GetAsync(fileUrl);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to get the file at the provided url: " + fileUrl);
            }

            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentNullException(nameof(fileUrl), "No permissions data found for the provided url " + fileUrl);
            }

            return content.ToString();
        }
        
        /// <summary>
        /// Gets a dictionary of paths and http methods extracted from a given permissions file.
        /// </summary>
        /// <param name="fileContent">The contents of the permissions file.</param>
        /// <returns>A dictionary of paths and http methods extracted from the permissions file.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public Dictionary<string, List<string>> GetPathsDictionaryFromPermissionsFileContents(string fileContent)
        {
            UtilityFunctions.CheckArgumentNullOrEmpty(fileContent, nameof(fileContent));
            
            JObject permissionsObject = JObject.Parse(fileContent);
            if (permissionsObject.Count < 1)
            {
                throw new InvalidOperationException("No permissions data found.");
            }

            var permPathsDictionary = new Dictionary<string, List<string>>();
            JToken apiPermissions = permissionsObject.First.First;

            foreach (JProperty property in apiPermissions.OfType<JProperty>())
            {
                // Remove any '(...)' from the request url and set to lowercase for uniformity
                string requestUrl = property.Name
                                            .UriTemplatePathFormat(true)
                                            .RemoveGuids()
                                            .Replace(":", string.Empty)
                                            .ToLower();

                var methods = property.Children().OfType<JToken>()
                                                .SelectMany(x => x).OfType<JProperty>()
                                                .Select(x => x.Name.ToLower()).Distinct().ToList();

                permPathsDictionary.TryAdd(requestUrl, methods);
            }

            return permPathsDictionary;
        }

        /// <summary>
        /// Gets a dictionary of paths and http methods extracted from a file at the given url. 
        /// </summary>
        /// <param name="fileUrl">The url of the permissions file.</param>
        /// <returns>A dictionary of paths and http methods extracted from the permissions file located at the provided <paramref name="fileUrl"/>.</returns>
        public async Task<Dictionary<string, List<string>>> GetPathsDictionaryFromPermissionsFileUrl(string fileUrl)
        {
            UtilityFunctions.CheckArgumentNull(fileUrl, nameof(fileUrl));

            var permissionsFile = await GetPermissionsFileFromUrl(fileUrl);
            return GetPathsDictionaryFromPermissionsFileContents(permissionsFile);
        }
    }
}
