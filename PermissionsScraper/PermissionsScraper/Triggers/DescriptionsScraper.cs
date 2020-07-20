// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using GitHubContentUtility.Common;
using GitHubContentUtility.Operations;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PermissionsScraper.Helpers;
using PermissionsScraper.Services;
using PermissionsAppConfig =  PermissionsScraper.Common.ApplicationConfig;
using GitHubRepoAppConfig = GitHubContentUtility.Common.ApplicationConfig;

namespace PermissionsScraper.Triggers
{
    public static class DescriptionsScraper
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
        public static void Run([TimerTrigger("0 */2 * * * *")]TimerInfo myTimer, ILogger log)
        {
            try
            {
                PermissionsAppConfig config = PermissionsAppConfig.ReadFromJsonFile("local.settings.json");

                log.LogInformation($"Authenticating into the Web API... Time: {DateTime.UtcNow}");
                var result = AuthService.GetAuthentication(config);

                if (result == null)
                {
                    log.LogInformation($"Failed to get authentication into the Web API. Time: {DateTime.UtcNow}");
                    return;
                }
                log.LogInformation($"Successfully authenticated into the Web API. Time: {DateTime.UtcNow}");

                if (config.ApiVersions?.Length > 0)
                {
                    _uniqueScopes = new HashSet<string>();
                    _scopesDescriptions = new Dictionary<string, List<Dictionary<string, object>>>();

                    foreach (var item in config.ApiVersions)
                    {
                        log.LogInformation($"Fetching Service Principal permissions descriptions for {item}. Time: {DateTime.UtcNow}");
                        PopulateScopesDescriptions(config, result, item);
                        log.LogInformation($"Finished fetching Service Principal permissions descriptions for {item}. Time: {DateTime.UtcNow}");
                    }
                }

                var servicePrincipalScopes = JsonConvert.SerializeObject(_scopesDescriptions, Formatting.Indented);

                // Fetch permissions descriptions from GitHub repo
                var contentReader = new BlobContentReader();
                var gitHubAppConfig = new GitHubRepoAppConfig
                {
                    GitHubAppId = config.GitHubAppId,
                    GitHubOrganization = config.GitHubOrganization,
                    GitHubAppName = config.GitHubAppName,
                    GitHubRepoName = config.GitHubRepoName,
                    ReferenceBranch = config.ReferenceBranch,
                    FileContentPath = config.FileContentPath,
                    WorkingBranch = config.WorkingBranch,
                    Reviewers = config.Reviewers,
                    PullRequestTitle = config.PullRequestTitle,
                    PullRequestBody = config.PullRequestBody,
                    PullRequestLabel = config.PullRequestLabel,
                    PullRequestAssignee = config.PullRequestAssignee,
                    CommitMessage = config.CommitMessage,
                    TreeItemMode = Enums.TreeItemMode.Blob
                };

                log.LogInformation($"Fetching permissions descriptions from GitHub repository '{gitHubAppConfig.GitHubRepoName}'. Time: {DateTime.UtcNow}");
                var repoScopes = contentReader.ReadRepositoryBlobContentAsync(gitHubAppConfig, config.GitHubAppKey).GetAwaiter().GetResult();
                log.LogInformation($"Finished fetching permissions descriptions from GitHub repository '{gitHubAppConfig.GitHubRepoName}'. Time: {DateTime.UtcNow}");

                // Compare GitHub permissions descriptions to Service Principal permissions descriptions
                if (!servicePrincipalScopes.Equals(repoScopes, StringComparison.OrdinalIgnoreCase))
                {
                    // Push Service Principals to GitHub repo
                    log.LogInformation($"Creating PR for updated Service Principal permissions descriptions into GitHub repository '{gitHubAppConfig.GitHubRepoName}'" +
                        $" from branch '{gitHubAppConfig.WorkingBranch}' into branch '{gitHubAppConfig.ReferenceBranch}'. Time: {DateTime.UtcNow}");
                    gitHubAppConfig.FileContent = servicePrincipalScopes;

                    var contentWriter = new BlobContentWriter();

                    contentWriter.WriteToRepositoryAsync(gitHubAppConfig, config.GitHubAppKey).GetAwaiter().GetResult();
                    log.LogInformation($"Finished creating PR for updated Service Principal permissions descriptions into GitHub repository '{gitHubAppConfig.GitHubRepoName}'" +
                        $" from branch '{gitHubAppConfig.WorkingBranch}' into branch '{gitHubAppConfig.ReferenceBranch}'. Time: {DateTime.UtcNow}");
                }
            }
            catch (Exception ex)
            {
                log.LogInformation($"Exception occurred: {ex.InnerException?.Message ?? ex.Message}. Time: {DateTime.UtcNow}");
            }
        }

        /// <summary>
        /// Retrieves and populates permissions descriptions from a Service Principal.
        /// </summary>
        /// <param name="config">The application configuration settings.</param>
        /// <param name="result">The JSON response of the permissions and their descriptions retrieved from the Service Prinicpal.</param>
        /// <param name="version">The version of the API from which to fetch the scopes descriptions from the Service Principal.</param>
        private static void PopulateScopesDescriptions(PermissionsAppConfig config,
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

            var spJsonResponse = PermissionsFormatHelper.FormatServicePrincipalResponse(spJson, config);

            // Retrieve the top level scope dictionary
            var spValue = JsonConvert.DeserializeObject<JObject>(spJsonResponse).Value<JArray>(config.TopLevelDictionaryName);

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
    }
}
