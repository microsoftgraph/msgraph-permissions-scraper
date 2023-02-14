// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using PermissionsScraper.Common;
using PermissionsScraper.Helpers;
using System;
using System.IO;
using Xunit;

namespace PermissionsScraper.Tests.Services
{
    public class OpenApiPathsServiceTests
    {
        private readonly OpenApiPathsService _openApiService;
        public OpenApiDocument OpenApiDocument { get; set; }
        
        public OpenApiPathsServiceTests()
        {
            _openApiService = new OpenApiPathsService();
            using var stream = new FileStream(Path.Combine(Environment.CurrentDirectory, "Files", "openapi.json"), FileMode.Open);
            OpenApiDocument = new OpenApiStreamReader().Read(stream, out var context);
        }

        [Fact]
        public void RetrievePathsFromOpenApiDocumentShouldSucceed()
        {            
            // Arrange
            using var stream = new FileStream(Path.Combine(Environment.CurrentDirectory, "Files", "openapi.json"), FileMode.Open);

            // Act
            var paths = _openApiService.RetrievePathsFromOpenApiDocument(OpenApiDocument);
            
            // Assert
            Assert.NotEmpty(paths);
            Assert.Equal(7641, paths.Count);
        }

        [Theory]
        [InlineData("/applications/microsoft.graph.delta()", "/applications/delta")]
        [InlineData("/drives/{drive-id}/items/{driveItem-id}/microsoft.graph.getActivitiesByInterval()",
    "/drives/{id}/items/{id}/getActivitiesByInterval")]
        [InlineData("/admin/serviceAnnouncement/healthOverviews/{serviceHealth-id}/issues/{serviceHealthIssue-id}/microsoft.graph.incidentReport()",
    "/admin/serviceAnnouncement/healthOverviews/{id}/issues/{id}/incidentReport")]
        public void FormatUrlPathsShouldSucceed(string path, string expected)
        {
            // Arrange
            var actual = path.RemoveIdPrefixes()
                             .UriTemplatePathFormat(true);

            // Assert
            Assert.Equal(expected, actual);
        }
    }
}
