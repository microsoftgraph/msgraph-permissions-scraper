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

        public string Tenant { get; set; }

        public string ClientId { get; set; }

        public string Authority
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, Instance, Tenant);
            }
        }

        public string ClientSecret { get; set; }

        public string ServicePrincipalId { get; set; }

        public string[] ScopesNamesList { get; set; }

        public Dictionary<string, string> RegexPatterns { get; set; }

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

        public string[] ApiVersions { get; set; }

        /// <summary>
        /// Reads the configuration from a json file
        /// </summary>
        /// <param name="path">Path to the configuration json file</param>
        /// <returns>AuthenticationConfig read from the json file</returns>
        public static ApplicationConfig ReadFromJsonFile(string path)
        {
            IConfigurationRoot Configuration;

            var builder = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(path);

            Configuration = builder.Build();
            return Configuration.Get<ApplicationConfig>();
        }
    }
}

