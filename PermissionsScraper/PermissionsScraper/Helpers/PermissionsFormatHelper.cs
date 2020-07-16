using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using PermissionsScraper.Common;

namespace PermissionsScraper.Helpers
{
    public static class PermissionsFormatHelper
    {
        /// <summary>
        /// Applies regex patterns to clean up the Service Principal JSON response data
        /// </summary>
        /// <param name="spJsonResponse">The Service Principal JSON response data.</param>
        /// <param name="config">The application configuration settings.</param>
        /// <returns>The Service Principal JSON response data with the regex transformations applied.</returns>
        public static string FormatServicePrincipalResponse(string spJsonResponse, ApplicationConfig config)
        {
            string cleanedSpJson = spJsonResponse;

            foreach (var item in config.RegexPatterns)
            {
                Regex regex = new Regex(item.Value, RegexOptions.IgnoreCase);
                var replacement = config.RegexReplacements[item.Key];
                cleanedSpJson = regex.Replace(cleanedSpJson, replacement);
            }

            return cleanedSpJson;
        }
    }
}
