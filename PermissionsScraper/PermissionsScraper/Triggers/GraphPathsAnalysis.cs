using System;
using System.Collections.Generic;
using GitHubContentUtility.Operations;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using PermissionsScraper.Services;
using PermissionsAppConfig = PermissionsScraper.Common.ApplicationConfig;
using Octokit;
using PermissionsScraper.Common;

namespace PermissionsScraper.Triggers
{
    public class GraphPathsAnalysis
    {
        private static readonly OpenApiPathsService _openApiPathsService = new OpenApiPathsService();
        private static readonly PermissionsFilePathsService _permissionsFilePathsService = new PermissionsFilePathsService();
        private static readonly PathsComparerService _pathsComparerService = new PathsComparerService();

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

                var gitHubAppConfig = GithubConfigurationProvider.SetGitHubConfiguration(permissionsAppConfig);

                gitHubAppConfig.FileContents = GetGraphPathsFileContent(permissionsAppConfig, gitHubAppConfig.FileContents);

                log.LogInformation($"Writing updated Graph paths files into GitHub repository '{gitHubAppConfig.GitHubRepoName}', " +
                    $"branch '{gitHubAppConfig.WorkingBranch}'. Time: {DateTime.UtcNow}");

              //  BlobContentWriter.WriteToRepositoryAsync(gitHubAppConfig, permissionsAppConfig.GitHubAppKey).GetAwaiter().GetResult();

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

        private static Dictionary<string, string> GetGraphPathsFileContent(PermissionsAppConfig permissionsAppConfig, Dictionary<string, string> fileContents)
        {
            // Graph version urls for both openApi and permissions files
            var permissionsFileV1Url = permissionsAppConfig.GraphPermissionsFilePaths[Constants.V1];
            var permissionsFileBetaUrl = permissionsAppConfig.GraphPermissionsFilePaths[Constants.Beta];
            var openApiFileV1Url = permissionsAppConfig.GraphOpenApiFilePaths[Constants.V1];
            //  var openApiFileBetaUrl = permissionsAppConfig.GraphOpenApiFilePaths[Constants.Beta];
             var openApiDocV1 = _openApiPathsService.FetchOpenApiDocument(openApiFileV1Url).GetAwaiter().GetResult();

            // Permissions and OpenApi file contents
            var permissionsFileContent_V1 = _permissionsFilePathsService.GetSerializedPathsDictionaryFromPermissionsFileUrl(permissionsFileV1Url);
            var permissionsFileContentBeta = _permissionsFilePathsService.GetSerializedPathsDictionaryFromPermissionsFileUrl(permissionsFileBetaUrl);
            var openApiFileContentV1 = _openApiPathsService.GetSerializedPathsDictionaryFromOpenApiFileUrlAsync(openApiDocV1);
          //  var openApiFileContentBeta = _openApiPathsService.GetSerializedPathsDictionaryFromOpenApiFileUrlAsync(openApiFileBetaUrl).Result;

            // Permissions and OpenApi paths dictionaries
            var permissionsPathDict_V1 = _permissionsFilePathsService.DeserializePermissionsPathDictionary(permissionsFileContent_V1);
            // var permissionsPathDictBeta = _permissionsFilePathsService.DeserializePermissionsPathDictionary(permissionsFileBetaUrl);
            var openApiPathsDictV1 = _openApiPathsService.RetrievePathsFromOpenApiDocument(openApiDocV1);
           // var openApiPathsDictBeta = _openApiPathsService.RetrievePathsFromOpenApiDocument(openApiFileBetaUrl).Result;

            // missing paths in openApi and permissions files for both v1 and beta
            var missingPathsInOpenApiV1 = _pathsComparerService.GetPathsInPermissionsFileNotInOpenAPIDocument(openApiPathsDictV1, permissionsPathDict_V1);
          //  var missingPathsInOpenApiBeta = _pathsComparerService.GetPathsInPermissionsFileNotInOpenAPIDocument(permissionsPathDictBeta, openApiPathsDictBeta);
            var missingPathsInPermissionsV1 = _pathsComparerService.GetPathsInOpenAPIDocumentNotInPermissionsFile(openApiPathsDictV1, permissionsPathDict_V1);
          //  var missingPathsInPermissionsBeta = _pathsComparerService.GetPathsInOpenAPIDocumentNotInPermissionsFile(permissionsPathDictBeta, openApiPathsDictBeta);

            var contentDictionary = new Dictionary<string, string>
                {
                    { Constants.OpenApiPathsV1 , openApiFileContentV1 },
                  //  { Constants.OpenApiPathsBeta, openApiFileContentBeta },
                    { Constants.PermissionsPathsV1, permissionsFileContent_V1 },
                    { Constants.PermissionsPathsBeta, permissionsFileContentBeta },
                    { Constants.PathsNotInOpenApiDocumentV1, missingPathsInOpenApiV1 },
                  //  { Constants.PathsNotInOpenApiDocumentBeta, missingPathsInOpenApiBeta },
                    { Constants.PathsNotInPermissionsFileV1, missingPathsInPermissionsV1 },
                  //  { Constants.PathsNotInPermissionsFileBeta, missingPathsInPermissionsBeta }
                };

            foreach (var item in contentDictionary)
            {
                fileContents.Add(item.Key, item.Value);
            }

            return fileContents;
        }
    }
}
