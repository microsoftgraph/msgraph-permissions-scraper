using System.Collections.Generic;
using PermissionsAppConfig = PermissionsScraper.Common.ApplicationConfig;
using PermissionsScraper.Common;
using System;
using Newtonsoft.Json;

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
        private Dictionary<string, List<string>> _simpOpenApiPathsDict_V1;
        private Dictionary<string, List<string>> _simpOpenApiPathsDict_Beta;
        private Dictionary<string, List<string>> _simpPermissionsPathDict_V1;
        private Dictionary<string, List<string>> _simpPermissionsPathDict_Beta;

        #region Production Paths Dictionaries
        public Dictionary<string, List<string>> ProdOpenApiPathsDict_V1
        {
            get => _prodOpenApiPathsDict_V1;
            set => _prodOpenApiPathsDict_V1 = GetOpenApiPathsDictionary(Constants.V1,
                _permissionsAppConfig.ProductionOpenApiFilePaths[Constants.V1]);
        }

        public Dictionary<string, List<string>> ProdOpenApiPathsDict_Beta
        {
            get => _prodOpenApiPathsDict_Beta;
            set => _prodOpenApiPathsDict_Beta = GetOpenApiPathsDictionary(Constants.Beta,
                _permissionsAppConfig.ProductionOpenApiFilePaths[Constants.Beta]);
        }

        public Dictionary<string, List<string>> ProdPermissionsPathDict_V1
        {
            get => _prodPermissionsPathDict_V1;
            set => _prodPermissionsPathDict_V1 = GetPermissionsPathsDictionary(Constants.V1,
                _permissionsAppConfig.ProductionPermissionsFilePaths[Constants.V1]);
        }

        public Dictionary<string, List<string>> ProdPermissionsPathDict_Beta
        {
            get => _prodPermissionsPathDict_Beta;
            set => _prodPermissionsPathDict_Beta = GetPermissionsPathsDictionary(Constants.Beta,
                _permissionsAppConfig.ProductionPermissionsFilePaths[Constants.Beta]);
        }

        #endregion

        #region Simplified Paths Dictionaries
        public Dictionary<string, List<string>> SimpOpenApiPathsDict_V1
        {
            get => _simpOpenApiPathsDict_V1;
            set => _simpOpenApiPathsDict_V1 = GetOpenApiPathsDictionary(Constants.V1,
                _permissionsAppConfig.SimplifiedOpenApiFilePaths[Constants.V1]);
        }

        public Dictionary<string, List<string>> SimpOpenApiPathsDict_Beta
        {
            get => _simpOpenApiPathsDict_Beta;
            set => _simpOpenApiPathsDict_Beta = GetOpenApiPathsDictionary(Constants.Beta,
                _permissionsAppConfig.SimplifiedOpenApiFilePaths[Constants.Beta]);
        }

        public Dictionary<string, List<string>> SimpPermissionsPathDict_V1
        {
            get => _simpPermissionsPathDict_V1;
            set => _simpPermissionsPathDict_V1 = GetPermissionsPathsDictionary(Constants.V1,
                _permissionsAppConfig.SimplifiedPermissionsFilePaths[Constants.V1]);
        }

        public Dictionary<string, List<string>> SimpPermissionsPathDict_Beta
        {
            get => _simpPermissionsPathDict_Beta;
            set => _simpPermissionsPathDict_Beta = GetPermissionsPathsDictionary(Constants.Beta,
                _permissionsAppConfig.SimplifiedPermissionsFilePaths[Constants.Beta]);
        }

        #endregion

        public GraphPathsService (PermissionsAppConfig permissionsAppConfig)
        {
            _permissionsAppConfig = permissionsAppConfig;
        }
        
        public Dictionary<string, List<string>> GetOpenApiPathsDictionary(string version, string url)
        {
            if (version.Equals(Constants.V1, StringComparison.OrdinalIgnoreCase))
            {
                var openApiDoc_V1 = _openApiPathsService.FetchOpenApiDocument(url).GetAwaiter().GetResult();
                return _openApiPathsService.RetrievePathsFromOpenApiDocument(openApiDoc_V1);
            }
            else if (version.Equals(Constants.Beta, StringComparison.OrdinalIgnoreCase))
            {
                var openApiDoc_Beta = _openApiPathsService.FetchOpenApiDocument(url).GetAwaiter().GetResult();
                return _openApiPathsService.RetrievePathsFromOpenApiDocument(openApiDoc_Beta);
            }
            
            return null;
        }

        public Dictionary<string, List<string>> GetPermissionsPathsDictionary(string version, string url)
        {
            if (version.Equals(Constants.V1, StringComparison.OrdinalIgnoreCase))
            {
                return _permissionsFilePathsService.GetPathsDictionaryFromPermissionsFileUrlAsync(url).GetAwaiter().GetResult();
            }
            else if (version.Equals(Constants.Beta, StringComparison.OrdinalIgnoreCase))
            {
                return _permissionsFilePathsService.GetPathsDictionaryFromPermissionsFileUrlAsync(url).GetAwaiter().GetResult();
            }

            return null;
        }

        public Dictionary<string, string> GetGraphPathsFileContents(Dictionary<string, string> fileContents)
        {            
            // Permissions and OpenAPI file contents
            var permissionsFileContent_V1 = JsonConvert.SerializeObject(ProdPermissionsPathDict_V1, Formatting.Indented);
            var permissionsFileContent_Beta = JsonConvert.SerializeObject(ProdPermissionsPathDict_Beta, Formatting.Indented);
            var openApiFileContent_V1 = JsonConvert.SerializeObject(ProdOpenApiPathsDict_V1, Formatting.Indented);
            var openApiFileContent_Beta = JsonConvert.SerializeObject(ProdOpenApiPathsDict_Beta, Formatting.Indented);

            // Missing paths in OpenAPI and permissions files for both v1 and beta
            var missingPathsInOpenApi_V1 = _pathsComparerService.GetPathsInPermissionsFileNotInOpenAPIDocument(ProdOpenApiPathsDict_V1, ProdPermissionsPathDict_V1);
            var missingPathsInOpenApi_Beta = _pathsComparerService.GetPathsInPermissionsFileNotInOpenAPIDocument(ProdOpenApiPathsDict_Beta, ProdPermissionsPathDict_Beta);
            var missingPathsInPermissions_V1 = _pathsComparerService.GetPathsInOpenAPIDocumentNotInPermissionsFile(ProdOpenApiPathsDict_V1, ProdPermissionsPathDict_V1);
            var missingPathsInPermissions_Beta = _pathsComparerService.GetPathsInOpenAPIDocumentNotInPermissionsFile(ProdOpenApiPathsDict_Beta, ProdPermissionsPathDict_Beta);

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

        public bool ShouldGraphPathFilesBeUpdated() => 
                !JsonConvert.SerializeObject(ProdPermissionsPathDict_V1, Formatting.Indented)
                .Equals(JsonConvert.SerializeObject(SimpPermissionsPathDict_V1, Formatting.Indented)) ||
                !JsonConvert.SerializeObject(ProdPermissionsPathDict_Beta, Formatting.Indented)
                .Equals(JsonConvert.SerializeObject(SimpPermissionsPathDict_Beta, Formatting.Indented)) ||
                !JsonConvert.SerializeObject(ProdOpenApiPathsDict_V1, Formatting.Indented)
                .Equals(JsonConvert.SerializeObject(SimpOpenApiPathsDict_V1, Formatting.Indented)) ||
                !JsonConvert.SerializeObject(ProdOpenApiPathsDict_Beta, Formatting.Indented)
                .Equals(JsonConvert.SerializeObject(SimpOpenApiPathsDict_Beta, Formatting.Indented));

    }
}
