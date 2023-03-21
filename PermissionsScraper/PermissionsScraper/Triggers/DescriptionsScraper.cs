// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using GitHubContentUtility.Common;
using GitHubContentUtility.Operations;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Octokit;
using PermissionsScraper.Common;
using PermissionsScraper.Helpers;
using PermissionsScraper.Models;
using PermissionsScraper.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using GitHubRepoAppConfig = GitHubContentUtility.Common.ApplicationConfig;
using PermissionsAppConfig = PermissionsScraper.Common.ApplicationConfig;

namespace PermissionsScraper.Triggers
{
    public static class DescriptionsScraper
    {
        private const string PermissionsDocumentFile = "PermissionsDocumentFile";
        private const string PermissionsDescriptions = "PermissionsDescriptions";
        private static Dictionary<string, List<Dictionary<string, object>>> _servicePrincipalPermissions;
        private static Dictionary<string, List<Dictionary<string, object>>> _updatedGithubPermissions;

        /// <summary>
        /// Timer function that fetches permissions descriptions from a Service Principal or workloads permissions file
        /// and uploads them to a GitHub repo at specified times.
        /// </summary>
        /// <param name="myTimer">Trigger function execution according to the specified cron time.</param>
        /// <param name="logger">Logger object used to log information, errors or warnings.</param>
        [FunctionName("DescriptionsScraper")]
        public static void Run([TimerTrigger("%ScheduleTriggerTime:PermissionsDescriptions%")] TimerInfo myTimer, ILogger logger)
        {
            logger.LogInformation($"DescriptionsScraper function started. Time: {DateTime.UtcNow}");

            try
            {
                PermissionsAppConfig permissionsAppConfig = PermissionsAppConfig.ReadFromJsonFile("local.settings.json");
                var gitHubAppConfig = new GitHubRepoAppConfig
                {
                    GitHubAppId = permissionsAppConfig.GitHubAppId,
                    GitHubOrganization = permissionsAppConfig.GitHubOrganization,
                    GitHubAppName = permissionsAppConfig.GitHubAppName,
                    GitHubRepoName = permissionsAppConfig.GitHubRepoName,
                    ReferenceBranch = permissionsAppConfig.ReferenceBranch,
                    FileContentPath = permissionsAppConfig.FileContentPaths[PermissionsDescriptions],
                    WorkingBranch = permissionsAppConfig.WorkingBranches[PermissionsDescriptions],
                    Reviewers = permissionsAppConfig.Reviewers,
                    PullRequestTitle = permissionsAppConfig.PullRequestTitles[PermissionsDescriptions],
                    PullRequestBody = permissionsAppConfig.PullRequestBodies[PermissionsDescriptions],
                    PullRequestLabels = permissionsAppConfig.PullRequestLabels,
                    PullRequestAssignees = permissionsAppConfig.PullRequestAssignees,
                    CommitMessage = permissionsAppConfig.CommitMessages[PermissionsDescriptions],
                    TreeItemMode = Enums.TreeItemMode.Blob
                };

                var existingPermissionsDescriptionsText = BlobContentReader.ReadRepositoryBlobContentAsync(gitHubAppConfig,
                       permissionsAppConfig.GitHubAppKey).GetAwaiter().GetResult();

                if (permissionsAppConfig.UseServicePrincipalPermissionDescriptions)
                {
                    logger.LogInformation($"Authenticating into the Web API... Time: {DateTime.UtcNow}");

                    var authResult = AuthService.GetAuthentication(permissionsAppConfig);

                    if (authResult == null)
                    {
                        logger.LogInformation($"Failed to get authentication into the Web API. Time: {DateTime.UtcNow}");
                        return;
                    }

                    logger.LogInformation($"Successfully authenticated into the Web API. Time: {DateTime.UtcNow}");

                    _servicePrincipalPermissions = new Dictionary<string, List<Dictionary<string, object>>>();
                    if (permissionsAppConfig.ApiVersions?.Length > 0)
                    {
                        foreach (string apiVersion in permissionsAppConfig.ApiVersions)
                        {
                            logger.LogInformation($"Fetching Service Principal permissions descriptions for {apiVersion}. Time: {DateTime.UtcNow}");

                            var spPermissionsDescriptions = FetchServicePrincipalPermissionsDescriptions(permissionsAppConfig, authResult.AccessToken, apiVersion);
                            PermissionsProcessor.ExtractPermissionsDescriptionsIntoDictionary(permissionsAppConfig.ScopesNames,
                                                                                              spPermissionsDescriptions,
                                                                                              ref _servicePrincipalPermissions,
                                                                                              permissionsAppConfig.TopLevelDictionaryName);

                            logger.LogInformation($"Finished fetching Service Principal permissions descriptions for {apiVersion}. Time: {DateTime.UtcNow}");
                        }
                    }

                    if (!_servicePrincipalPermissions.Any())
                    {
                        logger.LogInformation($"{nameof(_servicePrincipalPermissions)} dictionary returned empty data. " +
                            $"Exiting function DescriptionsScraper. Time: {DateTime.UtcNow}");
                        return;
                    }

                    logger.LogInformation($"Fetching permissions descriptions from GitHub repository '{gitHubAppConfig.GitHubRepoName}', branch '{permissionsAppConfig.ReferenceBranch}'. " +
                        $"Time: {DateTime.UtcNow}");

                    _updatedGithubPermissions = new Dictionary<string, List<Dictionary<string, object>>>();
                    PermissionsProcessor.ExtractPermissionsDescriptionsIntoDictionary(permissionsAppConfig.ScopesNames,
                                                                                      existingPermissionsDescriptionsText,
                                                                                      ref _updatedGithubPermissions);

                    logger.LogInformation($"Finished fetching permissions descriptions from GitHub repository '{gitHubAppConfig.GitHubRepoName}', branch '{permissionsAppConfig.ReferenceBranch}'. " +
                        $"Time: {DateTime.UtcNow}");

                    logger.LogInformation($"Comparing scopes from the Service Principal and the GitHub repository '{gitHubAppConfig.GitHubRepoName}', branch '{permissionsAppConfig.ReferenceBranch}' " +
                        $"for new updates... Time: {DateTime.UtcNow}");

                    bool permissionsUpdated = PermissionsProcessor.UpdatePermissionsDescriptions(_servicePrincipalPermissions, ref _updatedGithubPermissions);
                    if (permissionsUpdated is false)
                    {
                        logger.LogInformation($"No permissions descriptions update required. Exiting function 'DescriptionsScraper'. Time: {DateTime.UtcNow}");
                        return;
                    }

                    gitHubAppConfig.FileContent = JsonConvert.SerializeObject(_updatedGithubPermissions, Formatting.Indented).ChangeLineBreaks();
                }
                else // use descriptions from workloads permissions file
                {
                    gitHubAppConfig.FileContentPath = permissionsAppConfig.FileContentPaths[PermissionsDocumentFile];
                    var permissionsDocumentText = BlobContentReader.ReadRepositoryBlobContentAsync(gitHubAppConfig, permissionsAppConfig.GitHubAppKey).GetAwaiter().GetResult();

                    if (string.IsNullOrEmpty(permissionsDocumentText))
                        throw new InvalidOperationException("Workloads permissions file was not found or is empty");

                    logger.LogInformation($"Finished fetching workloads permissions file from GitHub repository '{gitHubAppConfig.GitHubRepoName}', branch '{gitHubAppConfig.ReferenceBranch}'. Time: {DateTime.UtcNow}");

                    var permissionsDocument = PermissionsDocument.Load(permissionsDocumentText);

                    logger.LogInformation($"Extracting permissions descriptions into dictionary. Time: {DateTime.UtcNow}");
                    var permissionsDescriptionsDictionary = PermissionsProcessor.ExtractPermissionDescriptionsIntoDictionary(permissionsDocument);
                    string newPermissionsDescriptionsText = JsonConvert.SerializeObject(permissionsDescriptionsDictionary, Formatting.Indented).ChangeLineBreaks();

                    if (newPermissionsDescriptionsText == existingPermissionsDescriptionsText.ChangeLineBreaks())
                    {
                        logger.LogInformation($"No permissions descriptions update required. Exiting function 'PermissionsConverter'. Time: {DateTime.UtcNow}");
                        return;
                    }

                    gitHubAppConfig.FileContent = newPermissionsDescriptionsText;
                }

                gitHubAppConfig.FileContentPath = permissionsAppConfig.FileContentPaths[PermissionsDescriptions];

                logger.LogInformation($"Writing updated permissions descriptions into GitHub repository '{gitHubAppConfig.GitHubRepoName}', " +
                    $"branch '{gitHubAppConfig.WorkingBranch}'. Time: {DateTime.UtcNow}");

                BlobContentWriter.WriteToRepositoryAsync(gitHubAppConfig, permissionsAppConfig.GitHubAppKey).GetAwaiter().GetResult();

                logger.LogInformation($"Finished updating permissions descriptions into GitHub repository '{gitHubAppConfig.GitHubRepoName}', " +
                    $"branch '{gitHubAppConfig.WorkingBranch}'. Time: {DateTime.UtcNow}");

                logger.LogInformation($"Creating PR for updated permissions descriptions in GitHub repository '{gitHubAppConfig.GitHubRepoName}'" +
                    $" from branch '{gitHubAppConfig.WorkingBranch}' into branch '{gitHubAppConfig.ReferenceBranch}'. Time: {DateTime.UtcNow}");

                PullRequestCreator.CreatePullRequestAsync(gitHubAppConfig, permissionsAppConfig.GitHubAppKey).GetAwaiter().GetResult();

                logger.LogInformation($"Finished creating PR for updated permissions descriptions in GitHub repository '{gitHubAppConfig.GitHubRepoName}'" +
                    $" from branch '{gitHubAppConfig.WorkingBranch}' into branch '{gitHubAppConfig.ReferenceBranch}'. Time: {DateTime.UtcNow}");

                logger.LogInformation($"Exiting function DescriptionsScraper. Time: {DateTime.UtcNow}");
            }
            catch (ApiException ex)
            {
                if (ex.ApiError.Errors != null)
                {
                    foreach (var item in ex.ApiError.Errors)
                    {
                        logger.LogInformation($"Exception occurred: {item.Message} Time: {DateTime.UtcNow}");
                    }
                    return;
                }

                logger.LogInformation($"Exception occurred: {ex.ApiError.Message} Time: {DateTime.UtcNow}");
            }
            catch (Exception ex)
            {
                logger.LogInformation($"Exception occurred: {ex.InnerException?.Message ?? ex.Message} Time: {DateTime.UtcNow}");
                logger.LogInformation($"Exception stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Retrieves permissions descriptions from a Service Principal.
        /// </summary>
        /// <param name="config">The application configuration settings.</param>
        /// <param name="accessToken"> Access Token that can be used as a bearer token to access the Graph API.</param>
        /// <param name="version">The version of the API from which to fetch the scopes descriptions from the Service Principal.</param>
        private static string FetchServicePrincipalPermissionsDescriptions(PermissionsAppConfig config,
                                                                           string accessToken,
                                                                           string version)
        {
            UtilityFunctions.CheckArgumentNull(config, nameof(config));
            UtilityFunctions.CheckArgumentNullOrEmpty(accessToken, nameof(accessToken));
            UtilityFunctions.CheckArgumentNullOrEmpty(version, nameof(version));

            var webApiUrl = $"{config.ApiUrl}{version}/serviceprincipals?$filter=appId eq '{config.ServicePrincipalId}'";
            var servicePrincipalResponse = ProtectedApiCallHelper
                                            .CallWebApiAsync(webApiUrl, accessToken)
                                            .GetAwaiter().GetResult();

            // Need to replace certain key words in the raw response with our business domain key words
            return string.IsNullOrEmpty(servicePrincipalResponse)
                ? throw new Exception($"The call to fetch the Service Principal returned empty data. URL: {webApiUrl} ")
                : PermissionsFormatHelper.ReplaceRegexPatterns(servicePrincipalResponse, config.RegexPatterns, config.RegexReplacements);
        }
    }
}
