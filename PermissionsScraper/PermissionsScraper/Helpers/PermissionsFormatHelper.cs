// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using PermissionsScraper.Common;

namespace PermissionsScraper.Helpers
{
    internal static class PermissionsFormatHelper
    {
        /// <summary>
        /// Replaces matched regex patterns with replacement strings for a given string value.
        /// </summary>
        /// <param name="value">The Service Principal JSON response data.</param>
        /// <param name="regexMatchPatterns">The regex patterns to match against.</param>
        /// <param name="regexReplacements">The values to replace the regex matches with.</param>
        /// <returns>The Service Principal JSON response data with the regex transformations applied.</returns>
        internal static string ReplaceRegexPatterns(string value,
                                                    Dictionary<string, string> regexMatchPatterns,
                                                    Dictionary<string, string> regexReplacements)
        {
            UtilityFunctions.CheckArgumentNullOrEmpty(value, nameof(value));
            UtilityFunctions.CheckArgumentNull(regexMatchPatterns, nameof(regexMatchPatterns));
            UtilityFunctions.CheckArgumentNull(regexReplacements, nameof(regexReplacements));

            string cleanValue = value;

            foreach (var item in regexMatchPatterns)
            {
                Regex regex = new Regex(item.Value, RegexOptions.IgnoreCase);
                if (regexReplacements.TryGetValue(item.Key, out var replacement))
                {
                    cleanValue = regex.Replace(cleanValue, replacement);
                }
            }

            return cleanValue;
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
