// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Text.RegularExpressions;
using PermissionsScraper.Common;

namespace PermissionsScraper.Helpers
{
    internal static class PermissionsFormatHelper
    {
        /// <summary>
        /// Applies regex patterns to clean up the Service Principal JSON response data
        /// </summary>
        /// <param name="spJsonResponse">The Service Principal JSON response data.</param>
        /// <param name="config">The application configuration settings.</param>
        /// <returns>The Service Principal JSON response data with the regex transformations applied.</returns>
        internal static string FormatServicePrincipalResponse(string spJsonResponse, ApplicationConfig config)
        {
            if (string.IsNullOrEmpty(spJsonResponse))
            {
                throw new ArgumentNullException(nameof(spJsonResponse), "Parameter cannot be null or empty");
            }

            string cleanedSpJson = spJsonResponse;

            foreach (var item in config.RegexPatterns)
            {
                Regex regex = new Regex(item.Value, RegexOptions.IgnoreCase);
                var replacement = config.RegexReplacements[item.Key];
                cleanedSpJson = regex.Replace(cleanedSpJson, replacement);
            }

            return cleanedSpJson;
        }

        /// <summary>
        /// Formats the private key to add newline characters appropriately.
        /// </summary>
        /// <param name="key">The private key.</param>
        /// <returns>The private key with newline characters appropriately added.</returns>
        internal static string FormatPrivateKey(this string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key), "Parameter cannot be null or empty");
            }

            var sections = key.Split("-----BEGIN RSA PRIVATE KEY-----", StringSplitOptions.RemoveEmptyEntries);
            sections = sections[0].Split("-----END RSA PRIVATE KEY-----", StringSplitOptions.RemoveEmptyEntries);

            return "-----BEGIN RSA PRIVATE KEY-----\r\n" + sections[0] + "\r\n-----END RSA PRIVATE KEY-----";
        }
    }
}
