// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using PermissionsScraper.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace PermissionsScraper.Common
{
    /// <summary>
    /// Describes the application configuration settings
    /// </summary>
    public class ApplicationConfig
    {
        private string _gitHubAppKey;
        private Dictionary<string, string> _regexReplacements;

        public string Instance { get; set; } = "https://login.microsoftonline.com/{0}";

        public string ApiUrl { get; set; } = "https://graph.microsoft.com/";

        public string TenantId { get; set; }

        public string ClientId { get; set; }

        public string GitHubAppKey
        {
            get => _gitHubAppKey;
            set
            {
                _gitHubAppKey = value.FormatPrivateKey();
            }
        }

        public string Authority
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, Instance, TenantId);
            }
        }

        public string ClientSecret { get; set; }

        public string ServicePrincipalId { get; set; }

        public string TopLevelDictionaryName { get; set; }

        /// <summary>
        /// The scope names to retrieve from the Service Principal
        /// </summary>
        public string[] ScopesNames { get; set; }

        /// <summary>
        /// The regex pattern to match on the retrieved Service Principal JSON data
        /// </summary>
        public Dictionary<string, string> RegexPatterns { get; set; }

        /// <summary>
        /// Graph paths from Permissions file in production 
        /// </summary>
        public Dictionary<string, string> ProductionPermissionsFilePaths { get; set; }

        /// <summary>
        /// Graph paths from the simplified Permissions file
        /// </summary>
        public Dictionary<string, string> SimplifiedPermissionsFilePaths { get; set; }

        /// <summary>
        /// Graph paths from OpenApi file in production 
        /// </summary>
        public Dictionary<string, string> ProductionOpenApiFilePaths { get; set; }

        /// <summary>
        /// Graph paths from the simplified OpenAPI file
        /// </summary>
        public Dictionary<string, string> SimplifiedOpenApiFilePaths { get; set; }

        /// <summary>
        /// The string value to replace the matched regex pattern
        /// </summary>
        public Dictionary<string, string> RegexReplacements
        {
            get => _regexReplacements;
            set
            {
                if (value.Count != RegexPatterns.Count)
                {
                    throw new ArgumentException("The RegexReplacements dictionary needs to have equal number of elements as the RegexPatterns dictionary.",
                        nameof(RegexReplacements));
                }
                _regexReplacements = value;
            }
        }

        /// <summary>
        /// Versions of the target API
        /// </summary>
        public string[] ApiVersions { get; set; }

        /// <summary>
        /// The name of the GitHub app
        /// </summary>
        public string GitHubAppName { get; set; }

        /// <summary>
        /// The owner of the GitHub repository
        /// </summary>
        public string GitHubOrganization { get; set; }

        /// <summary>
        /// The name of the GitHub repository
        /// </summary>
        public string GitHubRepoName { get; set; }

        /// <summary>
        /// The GitHub app Id
        /// </summary>
        public int GitHubAppId { get; set; }

        /// <summary>
        /// The remote branch where commits are made into
        /// </summary>
        public string WorkingBranch { get; set; }

        /// <summary>
        /// The remote branch which is the base reference of the <see cref="WorkingBranch"/>
        /// </summary>
        public string ReferenceBranch { get; set; }

        /// <summary>
        /// The paths of file contents in a repository branch
        /// </summary>
        public Dictionary<string, string> FileContentPaths { get; set; }

        /// <summary>
        /// The string contents of files in a respository branch
        /// </summary> 
        public Dictionary<string, string> FileContents { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// The commit message
        /// </summary>
        public string CommitMessage { get; set; }

        /// <summary>
        /// Pull request titles
        /// </summary>
        public Dictionary<string, string> PullRequestTitles { get; set; }

        /// <summary>
        /// Pull request messages
        /// </summary>
        public Dictionary<string, string> PullRequestBodies { get; set; }

        /// <summary>
        /// List of pull request reviewers
        /// </summary>
        public List<string> Reviewers { get; set; }

        /// <summary>
        /// Pull request assignee
        /// </summary>
        public List<string> PullRequestAssignees { get; set; }

        /// <summary>
        /// Pull request label
        /// </summary>
        public List<string> PullRequestLabels { get; set; }

        /// <summary>
        /// Reads the app configurations from a json file or environment variables.
        /// </summary>
        /// <param name="path">Path to the configuration json file.</param>
        /// <returns>ApplicationConfig read from the json file or environment variables.</returns>
        public static ApplicationConfig ReadFromJsonFile(string path)
        {
            IConfigurationRoot Configuration;

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(path, optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
            return Configuration.Get<ApplicationConfig>();
        }
    }
}

