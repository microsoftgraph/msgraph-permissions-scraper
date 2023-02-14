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
        /// Gets a dictionary of paths and http methods extracted from a permissions file located at the provided <paramref name="fileUrl"/>.
        /// </summary>
        /// <param name="fileUrl">The target url which contains the permissions file.</param>
        /// <returns>A dictionary of paths and http methods extracted from the permissions file retrieved at the provided <paramref name="fileUrl"/>.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<Dictionary<string, List<string>>> GetPathsDictionaryFromPermissionsFile(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
            {
                throw new ArgumentNullException(nameof(fileUrl));
            }
            
            // Extract permissions from devx-content repo
            var client = HttpClientSingleton.Instance;
            using var response = await client.HttpClient.GetAsync(fileUrl);
            var content = await response.Content.ReadAsStringAsync();      
            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentNullException(nameof(fileUrl), "No permissions data found for the given url " + fileUrl);
            }

            JObject permissionsObject = JObject.Parse(content);
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
                                            .ToLower();

                var methods = property.Children().OfType<JToken>()
                                                .SelectMany(x => x).OfType<JProperty>()
                                                .Select(x => x.Name.ToLower()).Distinct().ToList();

                permPathsDictionary.TryAdd(requestUrl, methods);
            }

            return permPathsDictionary;
        }
    }
}
