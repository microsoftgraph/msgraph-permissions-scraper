// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using GitHubContentUtility.Common;
using Microsoft.Extensions.Logging;
using System;
using GitHubRepoAppConfig = GitHubContentUtility.Common.ApplicationConfig;
using PermissionsAppConfig = PermissionsScraper.Common.ApplicationConfig;

namespace PermissionsScraper.Services
{
    internal class GithubConfigurationProvider
    {
        internal static PermissionsAppConfig Authenticate(ILogger log)
        {
            log.LogInformation($"Authenticating into the Web API... Time: {DateTime.UtcNow}");

            PermissionsAppConfig permissionsAppConfig = PermissionsAppConfig.ReadFromJsonFile("local.settings.json");
            var authResult = AuthService.GetAuthentication(permissionsAppConfig);

            if (authResult == null)
            {
                log.LogInformation($"Failed to get authentication into the Web API. Time: {DateTime.UtcNow}");
                return null;
            }

            log.LogInformation($"Successfully authenticated into the Web API. Time: {DateTime.UtcNow}");
            return permissionsAppConfig;
        }
        
        internal static GitHubRepoAppConfig SetGitHubConfiguration(PermissionsAppConfig permissionsAppConfig)
        {
            var gitHubAppConfig = new GitHubRepoAppConfig
            {
                GitHubAppId = permissionsAppConfig.GitHubAppId,
                GitHubOrganization = permissionsAppConfig.GitHubOrganization,
                GitHubAppName = permissionsAppConfig.GitHubAppName,
                GitHubRepoName = permissionsAppConfig.GitHubRepoName,
                ReferenceBranch = permissionsAppConfig.ReferenceBranch,
                FileContentPaths = permissionsAppConfig.FileContentPaths,
                WorkingBranch = permissionsAppConfig.WorkingBranch,
                Reviewers = permissionsAppConfig.Reviewers,
                PullRequestTitle = permissionsAppConfig.PullRequestTitle,
                PullRequestBody = permissionsAppConfig.PullRequestBody,
                PullRequestLabels = permissionsAppConfig.PullRequestLabels,
                PullRequestAssignees = permissionsAppConfig.PullRequestAssignees,
                CommitMessage = permissionsAppConfig.CommitMessage,
                TreeItemMode = Enums.TreeItemMode.Blob
            };

            return gitHubAppConfig;
        }
    }
}
