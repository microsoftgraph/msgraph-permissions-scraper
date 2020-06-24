// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace PermissionsScrapper
{
    /// <summary>
    /// Describes the application configuration settings
    /// </summary>
    public class ApplicationConfig
    {
        private Dictionary<string, string> _regexReplacements;

        public string Instance { get; set; } = "https://login.microsoftonline.com/{0}";

        public string ApiUrl { get; set; } = "https://graph.microsoft.com/";

        public string TenantId { get; set; }

        public string ClientId { get; set; }

        public string Authority
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, Instance, TenantId);
            }
        }

        public string ClientSecret { get; set; }

        public string ServicePrincipalId { get; set; }

        /// <summary>
        /// The scope names to retrieve from the Service Principal
        /// </summary>
        public string[] ScopesNames { get; set; }

        /// <summary>
        /// The regex pattern to match on the retrieved Service Principal JSON data
        /// </summary>
        public Dictionary<string, string> RegexPatterns { get; set; }

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

