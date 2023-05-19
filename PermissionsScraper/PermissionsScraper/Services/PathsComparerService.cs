// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text;

namespace PermissionsScraper.Services
{
    /// <summary>
    /// Service that compares the paths in the OpenAPI document to the paths in the Permissions file.
    /// </summary>
    public class PathsComparerService
    {
        /// <summary>
        /// Compare the paths and methods in the permissions file to the paths and operations in the OpenAPI document.
        /// </summary>
        /// <param name="openApiPathsDict">The dictionary of OpenAPI document paths and methods.</param>
        /// <param name="permissionsPathDict">The dictionary of permissions file paths and methods.</param>
        /// <returns>A list of string value of paths.</returns>
        public string GetPathsInPermissionsFileNotInOpenAPIDocument(Dictionary<string, List<string>> openApiPathsDict, Dictionary<string, List<string>> permissionsPathDict, string version = null)
        {
            var prefix = version == null ? null : $" - {version}";
            var strBuilder = new StringBuilder();
            strBuilder.AppendLine("Paths and operations in the Permissions file not present in the OpenAPI document" + prefix);
            strBuilder.AppendLine("--------------------------------------------------------------------------------");
            var totalPaths = 0;

            // Compare permissions paths with OpenAPI paths
            foreach (var path in permissionsPathDict)
            {
                var permissionsPathKey = path.Key;
                var permissionsPathValue = path.Value;
                var pathKeyAppended = false;
                var separatorApended = false;
                var prevOpAppended = false;                

                if (openApiPathsDict.ContainsKey(permissionsPathKey))
                {
                    var openApiPathValue = openApiPathsDict[permissionsPathKey];

                    foreach (var operation in permissionsPathValue)
                    {
                        if (!openApiPathValue.Contains(operation))
                        {
                            if (!pathKeyAppended)
                            {
                                strBuilder.AppendLine();
                                strBuilder.Append(permissionsPathKey);
                                totalPaths++;
                                pathKeyAppended = true;
                            }

                            if (!separatorApended)
                            {
                                strBuilder.Append(" ---> ");
                                separatorApended = true;
                            }
                            if (prevOpAppended)
                            {
                                strBuilder.Append($", {operation}"); ;
                            }
                            else
                            {
                                strBuilder.Append($" {operation}");
                                prevOpAppended = true;
                            }
                        }
                    }
                }
                else
                {
                    strBuilder.AppendLine();
                    strBuilder.Append(permissionsPathKey);
                    totalPaths++;
                }
            }

            strBuilder.AppendLine();
            strBuilder.AppendLine();
            strBuilder.AppendLine("Total Paths --> " + totalPaths);

            return strBuilder.ToString();
        }

        /// <summary>
        /// Compare the paths and operations in the OpenAPI document to paths and methods in the permissions file.
        /// </summary>
        /// <param name="openApiPathsDict">The dictionary of OpenAPI document paths and methods</param>
        /// <param name="permissionsPathDict">The dictionary of permissions file paths and methods.</param>
        /// <returns>A list of string value of paths that a</returns>
        public string GetPathsInOpenAPIDocumentNotInPermissionsFile(Dictionary<string, List<string>> openApiPathsDict, Dictionary<string, List<string>> permissionsPathDict, string version = null)
        {
            var prefix = version == null ? null : $" - {version}";
            var strBuilder = new StringBuilder();
            strBuilder.AppendLine("Paths and operations in the OpenAPI document not present in the permissions file" + prefix);
            strBuilder.AppendLine("--------------------------------------------------------------------------------");
            var totalPaths = 0;

            // Compare OpenAPI paths with permissions paths
            foreach (var path in openApiPathsDict)
            {
                var openApiPathKey = path.Key;
                var openApiPathValue = path.Value;
                var pathKeyAppended = false;
                var separatorApended = false;
                var prevValueAppended = false;

                if (permissionsPathDict.ContainsKey(openApiPathKey))
                {
                    var permissionsPathValue = permissionsPathDict[openApiPathKey];

                    foreach (var operation in openApiPathValue)
                    {
                        if (!permissionsPathValue.Contains(operation))
                        {
                            if (!pathKeyAppended)
                            {
                                strBuilder.AppendLine();
                                strBuilder.Append(openApiPathKey);
                                totalPaths++;
                                pathKeyAppended = true;
                            }

                            if (!separatorApended)
                            {
                                strBuilder.Append(" ---> ");
                                separatorApended = true;
                            }
                            if (prevValueAppended)
                            {
                                strBuilder.Append($", {operation}"); ;
                            }
                            else
                            {
                                strBuilder.Append($" {operation}");
                                prevValueAppended = true;
                            }
                        }
                    }
                }
                else
                {
                    strBuilder.AppendLine();
                    strBuilder.Append(openApiPathKey);
                    totalPaths++;
                }
            }

            strBuilder.AppendLine();
            strBuilder.AppendLine();
            strBuilder.AppendLine("Total Paths --> " + totalPaths);

            return strBuilder.ToString();
        }
    }
}
