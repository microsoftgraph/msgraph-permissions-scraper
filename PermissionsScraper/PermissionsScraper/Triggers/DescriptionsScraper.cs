// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using GitHubContentUtility.Common;
using GitHubContentUtility.Operations;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PermissionsScraper.Helpers;
using PermissionsScraper.Services;
using PermissionsAppConfig = PermissionsScraper.Common.ApplicationConfig;
using GitHubRepoAppConfig = GitHubContentUtility.Common.ApplicationConfig;
using System.Linq;
using Octokit;

namespace PermissionsScraper.Triggers
{
    public static class DescriptionsScraper
    {
        private static Dictionary<string, List<Dictionary<string, object>>> _spScopesDescriptions; // will hold permissions descriptions from the Service Principal
        private static Dictionary<string, List<Dictionary<string, object>>> _githubScopesDescriptions; // will hold permissions descriptions from GitHub

        /// <summary>
        /// Timer function that fetches permissions descriptions from a Service Principal
        /// and uploads them to a GitHub repo at specified times.
        /// </summary>
        /// <param name="myTimer">Trigger function every weekday at 9 AM UTC</param>
        /// <param name="log">Logger object used to log information, errors or warnings.</param>
        [FunctionName("DescriptionsScraper")]
        public static void Run([TimerTrigger("0 0 9 * * 1-5")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"DescriptionsScraper function started. Time: {DateTime.UtcNow}");

            try
            {
                log.LogInformation($"Authenticating into the Web API... Time: {DateTime.UtcNow}");
                PermissionsAppConfig permsAppConfig = PermissionsAppConfig.ReadFromJsonFile("local.settings.json");
                var authResult = AuthService.GetAuthentication(permsAppConfig);

                if (authResult == null)
                {
                    log.LogInformation($"Failed to get authentication into the Web API. Time: {DateTime.UtcNow}");
                    return;
                }
                log.LogInformation($"Successfully authenticated into the Web API. Time: {DateTime.UtcNow}");

                _spScopesDescriptions = new Dictionary<string, List<Dictionary<string, object>>>();
                if (permsAppConfig.ApiVersions?.Length > 0)
                {
                    foreach (string apiVersion in permsAppConfig.ApiVersions)
                    {
                        log.LogInformation($"Fetching Service Principal permissions descriptions for {apiVersion}. Time: {DateTime.UtcNow}");
                        PopulatePermissionsDescriptions(permsAppConfig, authResult.AccessToken, apiVersion);
                        log.LogInformation($"Finished fetching Service Principal permissions descriptions for {apiVersion}. Time: {DateTime.UtcNow}");
                    }
                }

                if (!_spScopesDescriptions.Any())
                {
                    log.LogInformation($"{nameof(_spScopesDescriptions)} dictionary returned empty data. " +
                        $"Exiting function DescriptionsScraper Time: {DateTime.UtcNow}");
                    return;
                }

                // Fetch permissions descriptions from GitHub repo
                var gitHubAppConfig = new GitHubRepoAppConfig
                {
                    GitHubAppId = permsAppConfig.GitHubAppId,
                    GitHubOrganization = permsAppConfig.GitHubOrganization,
                    GitHubAppName = permsAppConfig.GitHubAppName,
                    GitHubRepoName = permsAppConfig.GitHubRepoName,
                    ReferenceBranch = permsAppConfig.ReferenceBranch,
                    FileContentPath = permsAppConfig.FileContentPath,
                    WorkingBranch = permsAppConfig.WorkingBranch,
                    Reviewers = permsAppConfig.Reviewers,
                    PullRequestTitle = permsAppConfig.PullRequestTitle,
                    PullRequestBody = permsAppConfig.PullRequestBody,
                    PullRequestLabels = permsAppConfig.PullRequestLabels,
                    PullRequestAssignees = permsAppConfig.PullRequestAssignees,
                    CommitMessage = permsAppConfig.CommitMessage,
                    TreeItemMode = Enums.TreeItemMode.Blob
                };

                log.LogInformation($"Fetching permissions descriptions from GitHub repository '{gitHubAppConfig.GitHubRepoName}', branch '{permsAppConfig.ReferenceBranch}'. " +
                    $"Time: {DateTime.UtcNow}");
                log.LogInformation($"Finished fetching permissions descriptions from GitHub repository '{gitHubAppConfig.GitHubRepoName}', branch '{permsAppConfig.ReferenceBranch}'. " +
                    $"Time: {DateTime.UtcNow}");

                log.LogInformation($"Comparing scopes from the Service Principal and the GitHub repository '{gitHubAppConfig.GitHubRepoName}', branch '{permsAppConfig.ReferenceBranch}' " +
                    $"for new updates... Time: {DateTime.UtcNow}");


                var servicePrincipalScopes = JsonConvert.SerializeObject(_spScopesDescriptions, Formatting.Indented)
                    .Replace("\r", string.Empty); // Hack to avoid whitespace diff with GitHub source document (formatted with only \n)

                // Fetch permissions descriptions from repo.
                var repoScopes = BlobContentReader.ReadRepositoryBlobContentAsync(gitHubAppConfig,
                                                                                  permsAppConfig.GitHubAppKey).GetAwaiter().GetResult();

                _githubScopesDescriptions = new Dictionary<string, List<Dictionary<string, object>>>();
                ConvertPermissionsDescriptionsToDictionary(permsAppConfig, repoScopes, ref _githubScopesDescriptions);

                if (servicePrincipalScopes.Equals(repoScopes, StringComparison.OrdinalIgnoreCase))
                {
                    log.LogInformation($"No permissions descriptions update required. Exiting function 'DescriptionsScraper'. Time: {DateTime.UtcNow}");
                    return;
                }

                // Save the new Service Principal scopes
                gitHubAppConfig.FileContent = servicePrincipalScopes;

                log.LogInformation($"Writing updated Service Principal permissions descriptions into GitHub repository '{gitHubAppConfig.GitHubRepoName}', " +
                    $"branch '{gitHubAppConfig.WorkingBranch}'. Time: {DateTime.UtcNow}");

                // Write permissions descriptions to repo.
                BlobContentWriter.WriteToRepositoryAsync(gitHubAppConfig, permsAppConfig.GitHubAppKey).GetAwaiter().GetResult();

                log.LogInformation($"Finished updating Service Principal permissions descriptions into GitHub repository '{gitHubAppConfig.GitHubRepoName}', " +
                    $"branch '{gitHubAppConfig.WorkingBranch}'. Time: {DateTime.UtcNow}");

                // Create PR
                log.LogInformation($"Creating PR for updated Service Principal permissions descriptions in GitHub repository '{gitHubAppConfig.GitHubRepoName}'" +
                    $" from branch '{gitHubAppConfig.WorkingBranch}' into branch '{gitHubAppConfig.ReferenceBranch}'. Time: {DateTime.UtcNow}");

                PullRequestCreator.CreatePullRequestAsync(gitHubAppConfig, permsAppConfig.GitHubAppKey).GetAwaiter().GetResult();

                log.LogInformation($"Finished creating PR for updated Service Principal permissions descriptions in GitHub repository '{gitHubAppConfig.GitHubRepoName}'" +
                    $" from branch '{gitHubAppConfig.WorkingBranch}' into branch '{gitHubAppConfig.ReferenceBranch}'. Time: {DateTime.UtcNow}");

                log.LogInformation($"Exiting function DescriptionsScraper. Time: {DateTime.UtcNow}");
            }
            catch (ApiException ex)
            {
                if (ex.ApiError.Errors != null)
                {
                    foreach (var item in ex.ApiError.Errors)
                    {
                        log.LogInformation($"Exception occurred: {item.Message} Time: {DateTime.UtcNow}");
                    }
                    return;
                }

                log.LogInformation($"Exception occurred: {ex.ApiError.Message} Time: {DateTime.UtcNow}");
            }
            catch (Exception ex)
            {
                log.LogInformation($"Exception occurred: {ex.InnerException?.Message ?? ex.Message} Time: {DateTime.UtcNow}");
                log.LogInformation($"Exception stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Retrieves and populates permissions descriptions from a Service Principal.
        /// </summary>
        /// <param name="config">The application configuration settings.</param>
        /// <param name="accessToken"> Access Token that can be used as a bearer token to access the Graph API.</param>
        /// <param name="version">The version of the API from which to fetch the scopes descriptions from the Service Principal.</param>
        private static void PopulatePermissionsDescriptions(PermissionsAppConfig config,
                                                            string accessToken,
                                                            string version)
        {
            if (string.IsNullOrEmpty(accessToken)) return;
            if (string.IsNullOrEmpty(version)) return;

            string webApiUrl = $"{config.ApiUrl}{version}/serviceprincipals?$filter=appId eq '{config.ServicePrincipalId}'";
            var servicePrincipalResponse = ProtectedApiCallHelper
                                            .CallWebApiAsync(webApiUrl, accessToken)
                                            .GetAwaiter().GetResult();

            if (string.IsNullOrEmpty(servicePrincipalResponse))
            {
                throw new Exception($"The call to fetch the Service Principal returned empty data. URL: {webApiUrl} ");
            }

            servicePrincipalResponse = PermissionsFormatHelper.FormatServicePrincipalResponse(servicePrincipalResponse, config);
            ConvertPermissionsDescriptionsToDictionary(config, servicePrincipalResponse, ref _spScopesDescriptions);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="config"></param>
        /// <param name="permissionsDescriptionsText"></param>
        /// <param name="permissionsDictionary"></param>
        private static void ConvertPermissionsDescriptionsToDictionary(PermissionsAppConfig config, string permissionsDescriptionsText, ref Dictionary<string, List<Dictionary<string, object>>> permissionsDictionary)
        {
            if (permissionsDictionary == null) return;
            if (string.IsNullOrEmpty(permissionsDescriptionsText)) return;

            var permissionsDescriptionsToken = JsonConvert.DeserializeObject<JObject>(permissionsDescriptionsText).Value<JArray>(config.TopLevelDictionaryName)?.First ??
                                               JsonConvert.DeserializeObject<JObject>(permissionsDescriptionsText);
            ConvertPermissionsDescriptionsToDictionary(config, permissionsDescriptionsToken, ref permissionsDictionary);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="config"></param>
        /// <param name="permissionsDescriptionsToken"></param>
        /// <param name="permissionsDictionary"></param>
        private static void ConvertPermissionsDescriptionsToDictionary(PermissionsAppConfig config, JToken permissionsDescriptionsToken, ref Dictionary<string, List<Dictionary<string, object>>> permissionsDictionary)
        {
            if (permissionsDictionary == null) return;
            if (permissionsDescriptionsToken == null) return;

            foreach (string scopeName in config.ScopesNames)
            {
                var permissionsDescriptions = permissionsDescriptionsToken?.Value<JArray>(scopeName)?.ToObject<List<Dictionary<string, object>>>();
                if (permissionsDescriptions == null) continue;

                if (!permissionsDictionary.ContainsKey(scopeName))
                {
                    permissionsDictionary.Add(scopeName, new List<Dictionary<string, object>>());
                }

                foreach (var permissionDescription in permissionsDescriptions)
                {
                    var id = permissionDescription["id"];
                    var permissionExists = permissionsDictionary[scopeName].Exists(x => x.ContainsValue(id));
                    if (!permissionExists)
                    {
                        permissionsDictionary[scopeName].Add(permissionDescription);
                    }
                }
            }
        }
    }
}
