// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using GitHubContentUtility.Common;
using GitHubContentUtility.Operations;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PermissionsScraper.Common;
using PermissionsScraper.Models;
using PermissionsScraper.Services;
using System;
using GitHubRepoAppConfig = GitHubContentUtility.Common.ApplicationConfig;
using PermissionsAppConfig = PermissionsScraper.Common.ApplicationConfig;

namespace PermissionsScraper.Triggers
{
    public static class PermissionsReverseLookupTableGenerator
    {
        private const string PermissionsDocumentFile = "PermissionsDocumentFile";
        private const string ReverseLookupTable = "ReverseLookupTable";

        [FunctionName("PermissionsReverseLookupTableGenerator")]
        public static void Run([TimerTrigger("%ScheduleTriggerTime_ReverseLookupTable%")] TimerInfo myTimer, ILogger logger)
        {
            logger.LogInformation($"{DateTime.UtcNow}: PermissionsReverseLookupTableGenerator function started.");
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
                    PullRequestTitle = permissionsAppConfig.PullRequestTitles[ReverseLookupTable],
                    PullRequestBody = permissionsAppConfig.PullRequestBodies[ReverseLookupTable],
                    WorkingBranch = permissionsAppConfig.WorkingBranches[ReverseLookupTable],
                    Reviewers = permissionsAppConfig.Reviewers,
                    CommitMessage = permissionsAppConfig.CommitMessages[ReverseLookupTable],
                    PullRequestLabels = permissionsAppConfig.PullRequestLabels,
                    PullRequestAssignees = permissionsAppConfig.PullRequestAssignees,
                    TreeItemMode = Enums.TreeItemMode.Blob
                };

                logger.LogInformation($"Fetching workloads permissions file from GitHub repository '{gitHubAppConfig.GitHubRepoName}', branch '{gitHubAppConfig.ReferenceBranch}'");

                gitHubAppConfig.FileContentPath = permissionsAppConfig.FileContentPaths[PermissionsDocumentFile];
                var permissionsDocumentText = BlobContentReader.ReadRepositoryBlobContentAsync(gitHubAppConfig, permissionsAppConfig.GitHubAppKey).GetAwaiter().GetResult();

                if (string.IsNullOrEmpty(permissionsDocumentText))
                    throw new InvalidOperationException("Workloads permissions file was not found or is empty");

                logger.LogInformation($"Finished fetching workloads permissions file from GitHub repository '{gitHubAppConfig.GitHubRepoName}', branch '{gitHubAppConfig.ReferenceBranch}'");

                logger.LogInformation($"Converting permissions document to reverse lookup table");

                var permissionsDocument = PermissionsDocument.Load(permissionsDocumentText);
                gitHubAppConfig.FileContentPath = permissionsAppConfig.FileContentPaths[ReverseLookupTable];

                var reverseLookupTable = PermissionsProcessor.CreatePermissionsReverseLookupTable(permissionsDocument);
                var newPermissionsText = JsonConvert.SerializeObject(reverseLookupTable, Formatting.Indented).ChangeLineBreaks();

                var existingPermissionsText = BlobContentReader.ReadRepositoryBlobContentAsync(gitHubAppConfig,
                    permissionsAppConfig.GitHubAppKey).GetAwaiter().GetResult();
                if (existingPermissionsText == newPermissionsText)
                {
                    logger.LogInformation($"Permissions reverse lookup table update not required. Exiting function 'PermissionsConverter'.");
                    return;
                }

                logger.LogInformation($"Writing permissions reverse lookup table into GitHub repository '{gitHubAppConfig.GitHubRepoName}', branch '{gitHubAppConfig.WorkingBranch}'");

                gitHubAppConfig.FileContent = newPermissionsText;
                BlobContentWriter.WriteToRepositoryAsync(gitHubAppConfig, permissionsAppConfig.GitHubAppKey).GetAwaiter().GetResult();

                logger.LogInformation($"Creating PR for updated permissions reverse lookup table in GitHub repository '{gitHubAppConfig.GitHubRepoName}' from branch '{gitHubAppConfig.WorkingBranch}' into branch '{gitHubAppConfig.ReferenceBranch}'.");

                PullRequestCreator.CreatePullRequestAsync(gitHubAppConfig, permissionsAppConfig.GitHubAppKey).GetAwaiter().GetResult();

                logger.LogInformation($"Exiting function PermissionsReverseLookupTableGenerator.");
            }
            catch (Exception ex)
            {
                logger.LogInformation($"Exception occurred: {ex.InnerException?.Message ?? ex.Message}");
                logger.LogInformation($"Exception stack trace: {ex.StackTrace}");
            }
        }
    }
}
