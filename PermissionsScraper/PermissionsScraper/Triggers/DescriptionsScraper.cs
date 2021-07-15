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
        private static Dictionary<string, List<Dictionary<string, object>>> _servicePrincipalPermissions;
        private static Dictionary<string, List<Dictionary<string, object>>> _githubPermissions;

        /// <summary>
        /// Timer function that fetches permissions descriptions from a Service Principal
        /// and uploads them to a GitHub repo at specified times.
        /// </summary>
        /// <param name="myTimer">Trigger function every weekday at 9 AM UTC</param>
        /// <param name="log">Logger object used to log information, errors or warnings.</param>
        [FunctionName("DescriptionsScraper")]
        public static void Run([TimerTrigger("%ScheduleTriggerTime%")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"DescriptionsScraper function started. Time: {DateTime.UtcNow}");

            try
            {
                log.LogInformation($"Authenticating into the Web API... Time: {DateTime.UtcNow}");
                PermissionsAppConfig permissionsAppConfig = PermissionsAppConfig.ReadFromJsonFile("local.settings.json");
                var authResult = AuthService.GetAuthentication(permissionsAppConfig);

                if (authResult == null)
                {
                    log.LogInformation($"Failed to get authentication into the Web API. Time: {DateTime.UtcNow}");
                    return;
                }
                log.LogInformation($"Successfully authenticated into the Web API. Time: {DateTime.UtcNow}");

                _servicePrincipalPermissions = new Dictionary<string, List<Dictionary<string, object>>>();
                if (permissionsAppConfig.ApiVersions?.Length > 0)
                {
                    foreach (string apiVersion in permissionsAppConfig.ApiVersions)
                    {
                        log.LogInformation($"Fetching Service Principal permissions descriptions for {apiVersion}. Time: {DateTime.UtcNow}");
                        PopulatePermissionsDescriptions(permissionsAppConfig, authResult.AccessToken, apiVersion);
                        log.LogInformation($"Finished fetching Service Principal permissions descriptions for {apiVersion}. Time: {DateTime.UtcNow}");
                    }
                }

                if (!_servicePrincipalPermissions.Any())
                {
                    log.LogInformation($"{nameof(_servicePrincipalPermissions)} dictionary returned empty data. " +
                        $"Exiting function DescriptionsScraper Time: {DateTime.UtcNow}");
                    return;
                }

                // Fetch permissions descriptions from GitHub repo
                var gitHubAppConfig = new GitHubRepoAppConfig
                {
                    GitHubAppId = permissionsAppConfig.GitHubAppId,
                    GitHubOrganization = permissionsAppConfig.GitHubOrganization,
                    GitHubAppName = permissionsAppConfig.GitHubAppName,
                    GitHubRepoName = permissionsAppConfig.GitHubRepoName,
                    ReferenceBranch = permissionsAppConfig.ReferenceBranch,
                    FileContentPath = permissionsAppConfig.FileContentPath,
                    WorkingBranch = permissionsAppConfig.WorkingBranch,
                    Reviewers = permissionsAppConfig.Reviewers,
                    PullRequestTitle = permissionsAppConfig.PullRequestTitle,
                    PullRequestBody = permissionsAppConfig.PullRequestBody,
                    PullRequestLabels = permissionsAppConfig.PullRequestLabels,
                    PullRequestAssignees = permissionsAppConfig.PullRequestAssignees,
                    CommitMessage = permissionsAppConfig.CommitMessage,
                    TreeItemMode = Enums.TreeItemMode.Blob
                };

                log.LogInformation($"Fetching permissions descriptions from GitHub repository '{gitHubAppConfig.GitHubRepoName}', branch '{permissionsAppConfig.ReferenceBranch}'. " +
                    $"Time: {DateTime.UtcNow}");
                log.LogInformation($"Finished fetching permissions descriptions from GitHub repository '{gitHubAppConfig.GitHubRepoName}', branch '{permissionsAppConfig.ReferenceBranch}'. " +
                    $"Time: {DateTime.UtcNow}");

                log.LogInformation($"Comparing scopes from the Service Principal and the GitHub repository '{gitHubAppConfig.GitHubRepoName}', branch '{permissionsAppConfig.ReferenceBranch}' " +
                    $"for new updates... Time: {DateTime.UtcNow}");

                // Fetch permissions descriptions from repo.
                var githubPermissionsText = BlobContentReader.ReadRepositoryBlobContentAsync(gitHubAppConfig,
                                                                                  permissionsAppConfig.GitHubAppKey).GetAwaiter().GetResult();

                _githubPermissions = new Dictionary<string, List<Dictionary<string, object>>>();
                PermissionsProcessor.ExtractPermissionsDescriptionsIntoDictionary(permissionsAppConfig.ScopesNames, githubPermissionsText, ref _githubPermissions);

                bool permissionsUpdated = PermissionsProcessor.UpdatePermissionsDescriptions(_servicePrincipalPermissions, ref _githubPermissions);

                if (permissionsUpdated is false)
                {
                    log.LogInformation($"No permissions descriptions update required. Exiting function 'DescriptionsScraper'. Time: {DateTime.UtcNow}");
                    return;
                }

                githubPermissionsText = JsonConvert.SerializeObject(_githubPermissions, Formatting.Indented)
                    .Replace("\r", string.Empty); // Hack to avoid whitespace diff with GitHub source document (formatted with only \n)

                gitHubAppConfig.FileContent = githubPermissionsText;

                log.LogInformation($"Writing updated Service Principal permissions descriptions into GitHub repository '{gitHubAppConfig.GitHubRepoName}', " +
                    $"branch '{gitHubAppConfig.WorkingBranch}'. Time: {DateTime.UtcNow}");

                BlobContentWriter.WriteToRepositoryAsync(gitHubAppConfig, permissionsAppConfig.GitHubAppKey).GetAwaiter().GetResult();

                log.LogInformation($"Finished updating Service Principal permissions descriptions into GitHub repository '{gitHubAppConfig.GitHubRepoName}', " +
                    $"branch '{gitHubAppConfig.WorkingBranch}'. Time: {DateTime.UtcNow}");

                log.LogInformation($"Creating PR for updated Service Principal permissions descriptions in GitHub repository '{gitHubAppConfig.GitHubRepoName}'" +
                    $" from branch '{gitHubAppConfig.WorkingBranch}' into branch '{gitHubAppConfig.ReferenceBranch}'. Time: {DateTime.UtcNow}");

                PullRequestCreator.CreatePullRequestAsync(gitHubAppConfig, permissionsAppConfig.GitHubAppKey).GetAwaiter().GetResult();

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

            servicePrincipalResponse = PermissionsFormatHelper.ReplaceRegexPatterns(servicePrincipalResponse, config.RegexPatterns, config.RegexReplacements);
            PermissionsProcessor.ExtractPermissionsDescriptionsIntoDictionary(config.ScopesNames, servicePrincipalResponse, ref _servicePrincipalPermissions, config.TopLevelDictionaryName);
        }
    }
}
