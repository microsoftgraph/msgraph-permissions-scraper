// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PermissionsScraper
{
    public static class PermsDescription
    {
        private static HashSet<string> _uniqueScopes; // for ensuring unique scopes are populated
        private static Dictionary<string, List<Dictionary<string, object>>> _scopesDescriptions; // will hold scopes descriptions from the Service Principal

        /// <summary>
        /// Timer function that fetches permissions descriptions from a Service Principal
        /// and uploads them to a GitHub repo at specified times.
        /// </summary>
        /// <param name="myTimer">Trigger function every weekday at 9 AM UTC</param>
        /// <param name="log">Logger object used to log information, errors or warnings.</param>
        [FunctionName("DescriptionsScraper")]
        public static void Run([TimerTrigger("0 0 9 * * 1-5")]TimerInfo myTimer, ILogger log)
        {
            try
            {
                ApplicationConfig config = ApplicationConfig.ReadFromJsonFile("local.settings.json");

                var result = GetAuthentication(config);

                if (result != null)
                {
                    if (config.ApiVersions?.Length > 0)
                    {
                        _uniqueScopes = new HashSet<string>();
                        _scopesDescriptions = new Dictionary<string, List<Dictionary<string, object>>>();

                        foreach (var item in config.ApiVersions)
                        {
                            PopulateScopesDescriptions(config, result, item);
                        }
                    }

                    var servicePrincipalScopes = JsonConvert.SerializeObject(_scopesDescriptions, Formatting.Indented);

                    /* TODO:
                     * Fetch permissions descriptions from GitHub
                     * Compare GitHub permissions descriptions to Service Principal permissions descriptions retrieved above
                     * PR into GitHub if there is a variance between the two
                     */
                }
                else
                {
                    log.LogInformation($"Failed to get authentication at: {DateTime.UtcNow}");
                }
            }
            catch (Exception ex)
            {
                log.LogInformation($"Exception occurred: {ex.InnerException?.Message ?? ex.Message}\r\n Time of occurrence: {DateTime.UtcNow}");
            }
        }

        /// <summary>
        /// Gets authentication to a protected Web API.
        /// </summary>
        /// <param name="config">The application configuration settings.</param>
        /// <returns>An authentication result, if successful.</returns>
        private static AuthenticationResult GetAuthentication(ApplicationConfig config)
        {
            IConfidentialClientApplication app;
            app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                  .WithClientSecret(config.ClientSecret)
                  .WithAuthority(new Uri(config.Authority))
                  .Build();

            string[] scopes = new string[] { $"{config.ApiUrl}.default" };

            return app.AcquireTokenForClient(scopes)
              .ExecuteAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves and populates permissions descriptions from a Service Principal.
        /// </summary>
        /// <param name="config">The application configuration settings.</param>
        /// <param name="result">The JSON response of the permissions and their descriptions retrieved from the Service Prinicpal.</param>
        /// <param name="version">The version of the API from which to fetch the scopes descriptions from the Service Principal.</param>
        private static void PopulateScopesDescriptions(ApplicationConfig config,
                                                       AuthenticationResult result,
                                                       string version)
        {
            string webApiUrl = $"{config.ApiUrl}{version}/serviceprincipals?$filter=appId eq '{config.ServicePrincipalId}'";
            var spJson = ProtectedApiCallHelper
                    .CallWebApiAsync(webApiUrl, result.AccessToken)
                    .GetAwaiter().GetResult();

            if (string.IsNullOrEmpty(spJson))
            {
                throw new ArgumentNullException(nameof(spJson), $"The call to fetch the Service Principal returned empty data. URL: {webApiUrl} ");
            }

            var cleanedSpJson = CleanJsonData(spJson, config);

            // Retrieve the top level scope dictionary
            var spValue = JsonConvert.DeserializeObject<JObject>(cleanedSpJson).Value<JArray>(config.TopLevelDictionaryName);

            if (spValue == null)
            {
                throw new ArgumentNullException(nameof(config.TopLevelDictionaryName), $"Attempt to retrieve the top-level dictionary returned empty data." +
                    $"Name: {config.TopLevelDictionaryName}");
            }

            /* Fetch permissions defined in the second level dictionary(ies),
             * e.g. appRoles, oauth2PermissionScopes --> 2nd level dictionary keys
             */
            foreach (var scopeName in config.ScopesNames)
            {
                // Retrieve all scopes descriptions for a given 2nd level dictionary retrieved from the Service Principal
                var scopeDescriptions = spValue.First.Value<JArray>(scopeName)?.ToObject<List<Dictionary<string, object>>>();

                // Add a key to the reference dictionary (if not present)
                if (!_scopesDescriptions.ContainsKey(scopeName))
                {
                    _scopesDescriptions.Add(scopeName, new List<Dictionary<string, object>>());
                }

                /* Add each of the scope description from SP to the current key in the
                 * reference dictionary
                 */
                foreach (var scopeDesc in scopeDescriptions)
                {
                    /* Add only unique scopes (there might be duplicated scopes in both v1.0 and beta)
                     * Uniqueness identified by id of the scope description
                     */
                    bool newScope = _uniqueScopes.Add(scopeDesc["id"].ToString());
                    if (newScope)
                    {
                        _scopesDescriptions[scopeName].Add(scopeDesc);
                    }
                }
            }
        }

        /// <summary>
        /// Applies regex patterns to clean up the Service Principal JSON response data
        /// </summary>
        /// <param name="spJson">The Service Principal JSON response data.</param>
        /// <param name="config">The application configuration settings.</param>
        /// <returns></returns>
        private static string CleanJsonData(string spJson, ApplicationConfig config)
        {
            string cleanedSpJson = spJson;

            foreach (var item in config.RegexPatterns)
            {
                Regex regex = new Regex(item.Value, RegexOptions.IgnoreCase);
                var replacement = config.RegexReplacements[item.Key];
                cleanedSpJson = regex.Replace(cleanedSpJson, replacement);
            }

            return cleanedSpJson;
        }
    }
}
