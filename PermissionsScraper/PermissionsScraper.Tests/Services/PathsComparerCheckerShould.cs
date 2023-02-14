// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using PermissionsScraper.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
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

            var openApiPathsDict = openApiService.RetrievePathsFromOpenApiDocument(_openApiDocument);
            var permissionsPathDict = permissionsService.GetPathsDictionaryFromPermissionsFileContents(_v1FileContents);
            var totalPaths = 0;
                       
            var strBuilder = new StringBuilder();
            strBuilder.AppendLine("Paths and operations in the Permissions file not present in the OpenAPI document");
            strBuilder.AppendLine("--------------------------------------------------------------------------------");
                

            // Compare permissions paths with OpenAPI paths
            foreach (var path in permissionsPathDict)
            {
                var pathKey = path.Key;
                var pathValue = path.Value;
                var pathKeyAppended = false;
                var separatorApended = false;

                if (openApiPathsDict.ContainsKey(pathKey))
                {
                    var openApiPathValue = openApiPathsDict[pathKey];

                    foreach (var operation in pathValue)
                    {
                        if (!openApiPathValue.Contains(operation))
                        {
                            strBuilder.AppendLine();
                            if (!pathKeyAppended)
                            {
                                strBuilder.Append(pathKey);
                                totalPaths++;
                                pathKeyAppended = true;
                            }

                            if (!separatorApended)
                            {
                                strBuilder.Append(" ---> ");
                                separatorApended = true;
                            }

                            strBuilder.Append($" {operation} |");
                        }
                    }
                }
                else
                {
                    strBuilder.AppendLine();
                    strBuilder.Append(pathKey);
                    totalPaths++;
                }
            }

            strBuilder.AppendLine();
            strBuilder.AppendLine();
            strBuilder.AppendLine("Total Paths --> " + totalPaths);

            var output = strBuilder.ToString();            
        }
    }
}
