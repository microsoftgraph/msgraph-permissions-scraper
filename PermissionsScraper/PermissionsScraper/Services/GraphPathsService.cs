using System.Collections.Generic;
using PermissionsAppConfig = PermissionsScraper.Common.ApplicationConfig;
using PermissionsScraper.Common;
using System;
using Newtonsoft.Json;
using Microsoft.OpenApi.Models;

namespace PermissionsScraper.Services
{
    public class GraphPathsService
    {
        private readonly OpenApiPathsService _openApiPathsService = new OpenApiPathsService();
        private readonly PermissionsFilePathsService _permissionsFilePathsService = new PermissionsFilePathsService();
        private readonly PathsComparerService _pathsComparerService = new PathsComparerService();
        private readonly PermissionsAppConfig _permissionsAppConfig;
        private Dictionary<string, List<string>> _prodOpenApiPathsDict_V1;
        private Dictionary<string, List<string>> _prodOpenApiPathsDict_Beta;
        private Dictionary<string, List<string>> _prodPermissionsPathDict_V1;
        private Dictionary<string, List<string>> _prodPermissionsPathDict_Beta;
        
        private string _simpOpenApiPathsDict_V1;
        private string _simpOpenApiPathsDict_Beta;
        private string _simpPermissionsPathDict_V1;
        private string _simpPermissionsPathDict_Beta;

        private string _permissionsFileContent_V1;
        private string _permissionsFileContent_Beta;
        private string _openApiFileContent_V1;
        private string _openApiFileContent_Beta;

        private string _missingPathsInOpenApi_V1;
        private string _missingPathsInOpenApi_Beta;
        private string _missingPathsInPermissions_V1;
        private string _missingPathsInPermissions_Beta;


        public GraphPathsService (PermissionsAppConfig permissionsAppConfig)
        {
            _permissionsAppConfig = permissionsAppConfig;
        }

        private async void FetchSourceFiles()
        {
            #region Production Paths Dictionaries

            // Retrieve the OpenAPI and permissions files from production sources
            _prodOpenApiPathsDict_V1 ??= GetOpenApiPathsDictionary(_permissionsAppConfig.GraphOpenApiFilePaths[Constants.V1]);

            _prodOpenApiPathsDict_Beta ??= GetOpenApiPathsDictionary(_permissionsAppConfig.GraphOpenApiFilePaths[Constants.Beta]);

            _prodPermissionsPathDict_V1 ??= GetPermissionsPathsDictionary(_permissionsAppConfig.GraphPermissionsFilePaths[Constants.V1]);

            _prodPermissionsPathDict_Beta ??= GetPermissionsPathsDictionary(_permissionsAppConfig.GraphPermissionsFilePaths[Constants.Beta]);

            #endregion

            //#region Simplified Paths Dictionaries

            // Retrieve the simplified OpenAPI and permissions files from the Github repo.
            //_simpOpenApiPathsDict_V1 ??= _openApiPathsService.GetSerializedPathsDictionaryFromOpenApiFileUrl(_permissionsAppConfig.GraphOpenApiFilePaths[Constants.SimplifiedV1_0]);

            //_simpOpenApiPathsDict_Beta ??= _openApiPathsService.GetSerializedPathsDictionaryFromOpenApiFileUrl(_permissionsAppConfig.GraphOpenApiFilePaths[Constants.SimplifiedBeta]);

            //_simpPermissionsPathDict_V1 ??= _permissionsFilePathsService.GetSerializedPathsDictionaryFromPermissionsFileUrl(_permissionsAppConfig.GraphPermissionsFilePaths[Constants.SimplifiedV1_0]);

            //_simpPermissionsPathDict_Beta ??= _permissionsFilePathsService.GetSerializedPathsDictionaryFromPermissionsFileUrl(_permissionsAppConfig.GraphPermissionsFilePaths[Constants.SimplifiedBeta]);

            //#endregion
        }

        public Dictionary<string, List<string>> GetOpenApiPathsDictionary(string url)
        {
            var openApiDoc = _openApiPathsService.FetchOpenApiDocument(url).GetAwaiter().GetResult();
            return _openApiPathsService.RetrievePathsFromOpenApiDocument(openApiDoc);
        }

        public Dictionary<string, List<string>> GetPermissionsPathsDictionary(string url)
        {
            return _permissionsFilePathsService.GetPathsDictionaryFromPermissionsFileUrlAsync(url).GetAwaiter().GetResult();
        }

        private void RetrievePermissionsAndOpenAPIFileContents()
        {
            FetchSourceFiles();
            
            // Permissions and OpenAPI file contents
            _permissionsFileContent_V1 = JsonConvert.SerializeObject(_prodPermissionsPathDict_V1, Formatting.Indented);
            _permissionsFileContent_Beta = JsonConvert.SerializeObject(_prodPermissionsPathDict_Beta, Formatting.Indented);
            _openApiFileContent_V1 = JsonConvert.SerializeObject(_prodOpenApiPathsDict_V1, Formatting.Indented);
            _openApiFileContent_Beta = JsonConvert.SerializeObject(_prodOpenApiPathsDict_Beta, Formatting.Indented);
        }
        
        private void RetrieveMissingPathsFileContents()
        {
            FetchSourceFiles();
            
            // Missing paths in OpenAPI and permissions files for both v1 and beta
            _missingPathsInOpenApi_V1 = _pathsComparerService.GetPathsInPermissionsFileNotInOpenAPIDocument(_prodOpenApiPathsDict_V1, _prodPermissionsPathDict_V1, Constants.V1);
            _missingPathsInOpenApi_Beta = _pathsComparerService.GetPathsInPermissionsFileNotInOpenAPIDocument(_prodOpenApiPathsDict_Beta, _prodPermissionsPathDict_Beta, Constants.Beta);
            _missingPathsInPermissions_V1 = _pathsComparerService.GetPathsInOpenAPIDocumentNotInPermissionsFile(_prodOpenApiPathsDict_V1, _prodPermissionsPathDict_V1, Constants.V1);
            _missingPathsInPermissions_Beta = _pathsComparerService.GetPathsInOpenAPIDocumentNotInPermissionsFile(_prodOpenApiPathsDict_Beta, _prodPermissionsPathDict_Beta, Constants.Beta);

        }

        public Dictionary<string, string> GetGraphPathsFileContents(Dictionary<string, string> fileContents)
        {
            //FetchSourceFiles();

            //// Permissions and OpenAPI file contents
            //var permissionsFileContent_V1 = JsonConvert.SerializeObject(_prodPermissionsPathDict_V1, Formatting.Indented);
            //var permissionsFileContent_Beta = JsonConvert.SerializeObject(_prodPermissionsPathDict_Beta, Formatting.Indented);
            //var openApiFileContent_V1 = JsonConvert.SerializeObject(_prodOpenApiPathsDict_V1, Formatting.Indented);
            //var openApiFileContent_Beta = JsonConvert.SerializeObject(_prodOpenApiPathsDict_Beta, Formatting.Indented);

            //// Missing paths in OpenAPI and permissions files for both v1 and beta
            //var missingPathsInOpenApi_V1 = _pathsComparerService.GetPathsInPermissionsFileNotInOpenAPIDocument(_prodOpenApiPathsDict_V1, _prodPermissionsPathDict_V1, Constants.V1);
            //var missingPathsInOpenApi_Beta = _pathsComparerService.GetPathsInPermissionsFileNotInOpenAPIDocument(_prodOpenApiPathsDict_Beta, _prodPermissionsPathDict_Beta, Constants.Beta);
            //var missingPathsInPermissions_V1 = _pathsComparerService.GetPathsInOpenAPIDocumentNotInPermissionsFile(_prodOpenApiPathsDict_V1, _prodPermissionsPathDict_V1, Constants.V1);
            //var missingPathsInPermissions_Beta = _pathsComparerService.GetPathsInOpenAPIDocumentNotInPermissionsFile(_prodOpenApiPathsDict_Beta, _prodPermissionsPathDict_Beta, Constants.Beta);
            
            RetrievePermissionsAndOpenAPIFileContents();
            RetrieveMissingPathsFileContents();
            
            var contentDictionary = new Dictionary<string, string>
                {
                    { Constants.OpenApiPathsV1, _openApiFileContent_V1 },
                    { Constants.OpenApiPathsBeta, _openApiFileContent_Beta },
                    { Constants.PermissionsPathsV1, _permissionsFileContent_V1 },
                    { Constants.PermissionsPathsBeta, _permissionsFileContent_Beta },
                    { Constants.MissingPathsInOpenApiDocumentV1, _missingPathsInOpenApi_V1 },
                    { Constants.MissingPathsInOpenApiDocumentBeta, _missingPathsInOpenApi_Beta },
                    { Constants.MissingPathsInPermissionsFileV1, _missingPathsInPermissions_V1 },
                    { Constants.MissingPathsInPermissionsFileBeta, _missingPathsInPermissions_Beta }
                };

            foreach (var item in contentDictionary)
            {
                fileContents.Add(item.Key, item.Value);
            }

            return fileContents;
        }

        public bool ShouldGraphPathFilesBeUpdated() 
        {
            FetchSourceFiles();
            
            return 
                !JsonConvert.SerializeObject(_prodPermissionsPathDict_V1, Formatting.Indented)
                .Equals(_simpPermissionsPathDict_V1) ||
                !JsonConvert.SerializeObject(_prodPermissionsPathDict_Beta, Formatting.Indented)
                .Equals(_simpPermissionsPathDict_Beta) ||
                !JsonConvert.SerializeObject(_prodOpenApiPathsDict_V1, Formatting.Indented)
                .Equals(_simpOpenApiPathsDict_V1) ||
                !JsonConvert.SerializeObject(_prodOpenApiPathsDict_Beta, Formatting.Indented)
                .Equals(_simpOpenApiPathsDict_Beta);
        }
    }
}
