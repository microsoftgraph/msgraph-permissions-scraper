// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.OpenApi.Models;
using PermissionsScraper.Services;
using System.Collections.Generic;
using Xunit;

namespace PermissionsScraper.Tests.Services
{
    public class PathsComparerCheckerShould
    {
        PermissionsFilePathsServiceShould _permissionsPathsTest = new PermissionsFilePathsServiceShould();
        OpenApiPathsServiceTests _openApiPathsTest = new OpenApiPathsServiceTests();
        string _v1FileContents;
        OpenApiDocument _openApiDocument;


        public PathsComparerCheckerShould()
        {
            // Compare the paths from the permissions file with the paths from the OpenAPI file
            _v1FileContents = _permissionsPathsTest.V1FileContents;
            _openApiDocument = _openApiPathsTest.OpenApiDocument;
        }

        [Fact]
        public void ComparePaths()
        {
            // Arrange
            var openApiService = new OpenApiPathsService();
            var permissionsService = new PermissionsFilePathsService();
            var pathsComparerService = new PathsComparerService();

            Dictionary<string, List<string>> openApiPathsDict = openApiService.RetrievePathsFromOpenApiDocument(_openApiDocument);
            Dictionary<string, List<string>> permissionsPathDict = permissionsService.GetPathsDictionaryFromPermissionsFileContents(_v1FileContents);

            // Act
            var missingPathsInOpenAPIDocument = pathsComparerService.GetPathsInOpenAPIDocumentNotInPermissionsFile(openApiPathsDict, permissionsPathDict);
            var missingPathsInPermissionsFile = pathsComparerService.GetPathsInPermissionsFileNotInOpenAPIDocument(openApiPathsDict, permissionsPathDict);

            // Assert
            Assert.NotNull(missingPathsInOpenAPIDocument);
            Assert.NotEmpty(missingPathsInPermissionsFile);          
        }
    }
}
