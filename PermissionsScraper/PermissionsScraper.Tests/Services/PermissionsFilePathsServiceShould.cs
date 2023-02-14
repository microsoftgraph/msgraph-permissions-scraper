// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------using PermissionsScraper.Services;

using PermissionsScraper.Services;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace PermissionsScraper.Tests.Services
{
    public class PermissionsFilePathsServiceShould
    {
        private string _v1FileContents;
        public PermissionsFilePathsServiceShould()
        {
            using var stream = new FileStream(Path.Combine(Environment.CurrentDirectory, "Files", "permissions-v1.0.json"), FileMode.Open);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            _v1FileContents = reader.ReadToEnd();
        }
        
        [Fact]
        public void GetPermissionsPaths()
        {
            // Arrange
            PermissionsFilePathsService service = new PermissionsFilePathsService();
            service.GetPathsDictionaryFromPermissionsFileContents(_v1FileContents);
        }
    }
}
