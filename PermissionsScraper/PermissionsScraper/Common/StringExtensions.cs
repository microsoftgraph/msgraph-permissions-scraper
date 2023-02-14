// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

namespace PermissionsScraper.Common
{
    public static class StringExtensions
    {
        /// <summary>
        /// Change the input string's line breaks
        /// </summary>
        /// <param name="rawString">The raw input string</param>
        /// <param name="newLine">The new line break.</param>
        /// <returns>The changed string.</returns>
        public static string ChangeLineBreaks(this string rawString, string newLine = "\n")
        {
            rawString = rawString.Trim('\n', '\r');
            rawString = rawString.Replace("\r\n", newLine);
            return rawString;
        }
    }
}
