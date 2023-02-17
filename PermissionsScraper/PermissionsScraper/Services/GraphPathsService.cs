using System.Collections.Generic;
using PermissionsAppConfig = PermissionsScraper.Common.ApplicationConfig;
using PermissionsScraper.Common;

namespace PermissionsScraper.Services
{
    public static class GraphPathsService
    {
        private static readonly OpenApiPathsService _openApiPathsService = new OpenApiPathsService();
        private static readonly PermissionsFilePathsService _permissionsFilePathsService = new PermissionsFilePathsService();
        private static readonly PathsComparerService _pathsComparerService = new PathsComparerService();
        
        public static Dictionary<string, string> GetGraphPathsFileContent(PermissionsAppConfig permissionsAppConfig, Dictionary<string, string> fileContents)
        {
            // Graph version urls for both openApi and permissions files
            var permissionsFileV1Url = permissionsAppConfig.GraphPermissionsFilePaths[Constants.V1];
            var permissionsFileBetaUrl = permissionsAppConfig.GraphPermissionsFilePaths[Constants.Beta];
            var openApiFileV1Url = permissionsAppConfig.GraphOpenApiFilePaths[Constants.V1];
            var openApiFileBetaUrl = permissionsAppConfig.GraphOpenApiFilePaths[Constants.Beta];
            var openApiDoc_V1 = _openApiPathsService.FetchOpenApiDocument(openApiFileV1Url).GetAwaiter().GetResult();
            var openApiDoc_Beta = _openApiPathsService.FetchOpenApiDocument(openApiFileBetaUrl).GetAwaiter().GetResult();

            // Permissions and OpenApi file contents
            var permissionsFileContent_V1 = _permissionsFilePathsService.GetSerializedPathsDictionaryFromPermissionsFileUrl(permissionsFileV1Url);
            var permissionsFileContent_Beta = _permissionsFilePathsService.GetSerializedPathsDictionaryFromPermissionsFileUrl(permissionsFileBetaUrl);
            var openApiFileContent_V1 = _openApiPathsService.GetSerializedPathsDictionaryFromOpenApiFileUrlAsync(openApiDoc_V1);
            var openApiFileContent_Beta = _openApiPathsService.GetSerializedPathsDictionaryFromOpenApiFileUrlAsync(openApiDoc_Beta);

            // Permissions and OpenApi paths dictionaries
            var permissionsPathDict_V1 = _permissionsFilePathsService.DeserializePermissionsPathDictionary(permissionsFileContent_V1);
            var permissionsPathDict_Beta = _permissionsFilePathsService.DeserializePermissionsPathDictionary(permissionsFileContent_Beta);
            var openApiPathsDict_V1 = _openApiPathsService.RetrievePathsFromOpenApiDocument(openApiDoc_V1);
            var openApiPathsDict_Beta = _openApiPathsService.RetrievePathsFromOpenApiDocument(openApiDoc_Beta);

            // missing paths in openApi and permissions files for both v1 and beta
            var missingPathsInOpenApi_V1 = _pathsComparerService.GetPathsInPermissionsFileNotInOpenAPIDocument(openApiPathsDict_V1, permissionsPathDict_V1);
            var missingPathsInOpenApi_Beta = _pathsComparerService.GetPathsInPermissionsFileNotInOpenAPIDocument(openApiPathsDict_Beta, permissionsPathDict_Beta);
            var missingPathsInPermissions_V1 = _pathsComparerService.GetPathsInOpenAPIDocumentNotInPermissionsFile(openApiPathsDict_V1, permissionsPathDict_V1);
            var missingPathsInPermissions_Beta = _pathsComparerService.GetPathsInOpenAPIDocumentNotInPermissionsFile(openApiPathsDict_Beta, permissionsPathDict_Beta);

            var contentDictionary = new Dictionary<string, string>
                {
                    { Constants.OpenApiPathsV1 , openApiFileContent_V1 },
                    { Constants.OpenApiPathsBeta, openApiFileContent_Beta },
                    { Constants.PermissionsPathsV1, permissionsFileContent_V1 },
                    { Constants.PermissionsPathsBeta, permissionsFileContent_Beta },
                    { Constants.MissingPathsInOpenApiDocumentV1, missingPathsInOpenApi_V1 },
                    { Constants.MissingPathsInOpenApiDocumentBeta, missingPathsInOpenApi_Beta },
                    { Constants.MissingPathsInPermissionsFileV1, missingPathsInPermissions_V1 },
                    { Constants.MissingPathsInPermissionsFileBeta, missingPathsInPermissions_Beta }
                };

            foreach (var item in contentDictionary)
            {
                fileContents.Add(item.Key, item.Value);
            }

            return fileContents;
        }
    }
}
