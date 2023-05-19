using System;
using GitHubContentUtility.Operations;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using PermissionsScraper.Services;
using Octokit;
using PermissionsScraper.Common;

namespace PermissionsScraper.Triggers
{
    public class GraphPathsScraper
    {
        /// <summary>
        /// This function uploads Graph paths analysis files to DevX Content repo.
        /// </summary>
        /// <param name="myTimer">Trigger function execution according to the specified cron time.</param>
        /// <param name="log">Logger object used to log information, errors or warnings.</param>
        [FunctionName("GraphPathsAnalysis")]
        public static void Run([TimerTrigger("%GraphPathsAnalysisScheduleTriggerTime%")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"GraphPathsAnalysis function executed at: {DateTime.Now}");
            try
            {
                var permissionsAppConfig = GithubConfigurationProvider.Authenticate(log);

                var resourceType = Constants.GraphPathsFiles;
                var gitHubAppConfig = GithubConfigurationProvider.SetGitHubConfiguration(permissionsAppConfig, resourceType);
                GraphPathsService graphPathsService = new GraphPathsService(permissionsAppConfig);
                
                gitHubAppConfig.FileContents = graphPathsService.GetGraphPathsFileContents(gitHubAppConfig.FileContents);

                log.LogInformation($"Writing updated Graph paths files into GitHub repository '{gitHubAppConfig.GitHubRepoName}', " +
                    $"branch '{gitHubAppConfig.WorkingBranch}'. Time: {DateTime.UtcNow}");

                BlobContentWriter.WriteToRepositoryAsync(gitHubAppConfig, permissionsAppConfig.GitHubAppKey).GetAwaiter().GetResult();

                log.LogInformation($"Finished updating Graph paths files into GitHub repository '{gitHubAppConfig.GitHubRepoName}', " +
                    $"branch '{gitHubAppConfig.WorkingBranch}'. Time: {DateTime.UtcNow}");

                log.LogInformation($"Creating PR for updated Graph paths files in GitHub repository '{gitHubAppConfig.GitHubRepoName}'" +
                    $" from branch '{gitHubAppConfig.WorkingBranch}' into branch '{gitHubAppConfig.ReferenceBranch}'. Time: {DateTime.UtcNow}");

                PullRequestCreator.CreatePullRequestAsync(gitHubAppConfig, permissionsAppConfig.GitHubAppKey).GetAwaiter().GetResult();

                log.LogInformation($"Finished creating PR for updated Graph paths files in GitHub repository '{gitHubAppConfig.GitHubRepoName}'" +
                    $" from branch '{gitHubAppConfig.WorkingBranch}' into branch '{gitHubAppConfig.ReferenceBranch}'. Time: {DateTime.UtcNow}");

                log.LogInformation($"Exiting function GraphPathsAnalysis. Time: {DateTime.UtcNow}");
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
    }
}
