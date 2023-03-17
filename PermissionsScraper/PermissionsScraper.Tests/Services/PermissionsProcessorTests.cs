// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Newtonsoft.Json;
using PermissionsScraper.Common;
using PermissionsScraper.Helpers;
using PermissionsScraper.Models;
using PermissionsScraper.Tests;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace PermissionsScraper.Services.Tests
{
    public class PermissionsProcessorTests
    {
        private const string DelegatedScopes = "delegatedScopesList";
        private const string ApplicationScopes = "applicationScopesList";

        [Fact]
        public async Task ExtractPermissionsDescriptionsFromServicePrincipalIntoDictionaryWorks()
        {
            // Arrange & Act
            (var servicePrincipalPermissionsDict, var gitHubPermissionsDict) = await GetPermissionsDescriptionsDictionariesAsync();

            // Assert
            Assert.Equal(292, servicePrincipalPermissionsDict[DelegatedScopes].Count);
            Assert.Equal(222, servicePrincipalPermissionsDict[ApplicationScopes].Count);
            Assert.Equal(296, gitHubPermissionsDict[DelegatedScopes].Count);
            Assert.Equal(225, gitHubPermissionsDict[ApplicationScopes].Count);
        }

        [Fact]
        public async Task UpdatePermissionsDescriptionsWorks()
        {
            // Arrange
            (var servicePrincipalPermissionsDict, var gitHubPermissionsDict) = await GetPermissionsDescriptionsDictionariesAsync();

            // Act
            PermissionsProcessor.UpdatePermissionsDescriptions(referencePermissions: servicePrincipalPermissionsDict,
                                                               updatablePermissions: ref gitHubPermissionsDict);

            // Assert
            Assert.Equal(gitHubPermissionsDict[DelegatedScopes].Count, servicePrincipalPermissionsDict[DelegatedScopes].Count);
            Assert.Equal(gitHubPermissionsDict[ApplicationScopes].Count, servicePrincipalPermissionsDict[ApplicationScopes].Count);
        }

        [Fact]
        public async Task CreatePermissionsReverseLookupTableWorks()
        {
            // Arrange
            var permissionsDocumentText = await GetWorkloadsPermissionsDocumentTextAsync();
            var permissionsDocument = PermissionsDocument.Load(permissionsDocumentText);
            
            // Act
            var reverseLookupTable = PermissionsProcessor.CreatePermissionsReverseLookupTable(permissionsDocument);
            string jsonResult = JsonConvert.SerializeObject(reverseLookupTable, Formatting.Indented);

            // Assert
            var reverseLookupTableText = await Resources.GetFileContents(".\\Resources\\PermissionsReverseLookupTable.json");
            Assert.Equal(reverseLookupTableText.ChangeLineBreaks(), jsonResult.ChangeLineBreaks());
            Assert.Equal(118, reverseLookupTable.Keys.Count);
        }

        [Fact]
        public async Task ExtractPermissionDescriptionsFromWorkloadsFileIntoDictionaryWorks()
        {
            // Arrange
            var permissionsDocumentText = await GetWorkloadsPermissionsDocumentTextAsync();
            var permissionsDocument = PermissionsDocument.Load(permissionsDocumentText);

            // Act
            var permissionDescriptions = PermissionsProcessor.ExtractPermissionDescriptionsIntoDictionary(permissionsDocument);
            string jsonResult = JsonConvert.SerializeObject(permissionDescriptions, Formatting.Indented);

            // Assert
            Assert.NotEmpty(permissionDescriptions);
            Assert.Equal(3, permissionDescriptions.Keys.Count); // Scheme Keys
            Assert.Collection(permissionDescriptions,
                item =>
                {
                    Assert.Equal("DelegatedWork", item.Key);
                    Assert.Equal(2, item.Value.Count);
                },
                item =>
                {
                    Assert.Equal("DelegatedPersonal", item.Key);
                    Assert.Single(item.Value);
                },
                item =>
                {
                    Assert.Equal("Application", item.Key);
                    Assert.Equal(3, item.Value.Count);
                }
            );       
            var permissionDescriptionsText = await GetGitHubPermissionsDescriptionsFromWorkloadsTextAsync();
            Assert.Equal(permissionDescriptionsText.ChangeLineBreaks(), jsonResult.ChangeLineBreaks());
        }

        private static async Task<string> GetWorkloadsPermissionsDocumentTextAsync()
        {
            return await Resources.GetFileContents(".\\Resources\\WorkloadsPermissionsDocument.json");
        }

        private static async Task<string> GetGitHubPermissionsDescriptionsFromServicePrincipalTextAsync()
        {
            return await Resources.GetFileContents(".\\Resources\\GitHubScopesDescriptionsFromServicePrincipal.json");
        }

        private static async Task<string> GetGitHubPermissionsDescriptionsFromWorkloadsTextAsync()
        {
            return await Resources.GetFileContents(".\\Resources\\GitHubScopesDescriptionsFromWorkloads.json");
        }

        private static void ExtractPermissionsDescriptionsIntoDictionary(string[] scopesNames,
                                                                         string permissionsDescriptionsText,
                                                                         ref Dictionary<string, List<Dictionary<string, object>>> referencePermissionsDictionary,
                                                                         string topLevelDictionaryName = null)
        {

            PermissionsProcessor.ExtractPermissionsDescriptionsIntoDictionary(scopesNames, permissionsDescriptionsText, ref referencePermissionsDictionary, topLevelDictionaryName);
            Assert.NotEmpty(referencePermissionsDictionary);
        }

        private static async Task<string> GetServicePrincipalPermissionsDescriptionsTextAsync()
        {
            var servicePrincipalDescriptions = await Resources.GetFileContents(".\\Resources\\ServicePrincipalScopesDescriptions.json");

            var regexMatchPatterns = new Dictionary<string, string>()
            {
                { "1", "\"oauth2PermissionScopes\""},
                { "2", "\"appRoles\""},
                { "3", "\"description\"" },
                { "4", "\"displayName\"" },
                { "5", "\"type\":\"Admin\"" },
                { "6", "\"type\":\"User\"" },
                { "7", "\"userConsentDescription\"" },
                { "8", "\"userConsentDisplayName\"" },
                { "9", "\"origin\":\"Application\"," }
            };

            var regexReplacements = new Dictionary<string, string>()
            {
                { "1", "delegatedScopesList" },
                { "2", "applicationScopesList" },
                { "3", "\"consentDescription\"" },
                { "4", "\"consentDisplayName\"" },
                { "5", "\"isAdmin\": true" },
                { "6", "\"isAdmin\": false" },
                { "7", "\"consentDescription\"" },
                { "8", "\"consentDisplayName\"" },
                { "9", "\"isAdmin\": false," }
            };

            return PermissionsFormatHelper.ReplaceRegexPatterns(servicePrincipalDescriptions, regexMatchPatterns, regexReplacements);
        }

        private static async Task<(Dictionary<string, List<Dictionary<string, object>>> servicePrincipalPermissionsDict,
            Dictionary<string, List<Dictionary<string, object>>> gitHubPermissionsDict)>
            GetPermissionsDescriptionsDictionariesAsync()
        {
            var servicePrincipalPermissionsText = await GetServicePrincipalPermissionsDescriptionsTextAsync();
            var gitHubPermissionsText = await GetGitHubPermissionsDescriptionsFromServicePrincipalTextAsync();
            Assert.NotNull(servicePrincipalPermissionsText);
            Assert.NotNull(gitHubPermissionsText);

            var servicePrincipalPermissionsDict = new Dictionary<string, List<Dictionary<string, object>>>();
            var gitHubPermissionsDict = new Dictionary<string, List<Dictionary<string, object>>>();
            var scopesNames = new[] { DelegatedScopes, ApplicationScopes };

            ExtractPermissionsDescriptionsIntoDictionary(scopesNames, servicePrincipalPermissionsText, ref servicePrincipalPermissionsDict, "value");
            ExtractPermissionsDescriptionsIntoDictionary(scopesNames, gitHubPermissionsText, ref gitHubPermissionsDict);

            return (servicePrincipalPermissionsDict, gitHubPermissionsDict);
        }
    }
}
