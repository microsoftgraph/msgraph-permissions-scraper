// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace PermissionsScraper.Tests
{
    internal static class Resources
    {
        /// <summary>
        /// Reads the contents of a provided file.
        /// </summary>
        /// <param name="filePathSource">The directory path name of the file.</param>
        /// <returns>The contents of the file.</returns>
        public static async Task<string> GetFileContents(string filePathSource)
        {
            Assert.NotNull(filePathSource);

            try
            {
                using var streamReader = new StreamReader(filePathSource);
                return await streamReader.ReadToEndAsync();
            }
            catch
            {
                throw;
            }
        }
    }
}
