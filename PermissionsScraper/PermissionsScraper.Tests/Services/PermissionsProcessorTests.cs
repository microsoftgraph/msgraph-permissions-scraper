// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using PermissionsScraper.Helpers;
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
        public void ExtractPermissionsDescriptionsIntoDictionaryWorks()
        {
            // Arrange & Act
            (var servicePrincipalPermissionsDict, var gitHubPermissionsDict) = GetPermissionsDescriptionsDictionaries();

            // Assert
            Assert.Equal(292, servicePrincipalPermissionsDict[DelegatedScopes].Count);
            Assert.Equal(222, servicePrincipalPermissionsDict[ApplicationScopes].Count);
            Assert.Equal(296, gitHubPermissionsDict[DelegatedScopes].Count);
            Assert.Equal(225, gitHubPermissionsDict[ApplicationScopes].Count);
        }

        [Fact]
        public void UpdatePermissionsDescriptionsWorks()
        {
            // Arrange
            (var servicePrincipalPermissionsDict, var gitHubPermissionsDict) = GetPermissionsDescriptionsDictionaries();

            // Act
            PermissionsProcessor.UpdatePermissionsDescriptions(referencePermissions: servicePrincipalPermissionsDict,
                                                               updatablePermissions: ref gitHubPermissionsDict);

            // Assert
            Assert.Equal(gitHubPermissionsDict[DelegatedScopes].Count, servicePrincipalPermissionsDict[DelegatedScopes].Count);
            Assert.Equal(gitHubPermissionsDict[ApplicationScopes].Count, servicePrincipalPermissionsDict[ApplicationScopes].Count);
        }

        private static void ExtractPermissionsDescriptionsIntoDictionary(string[] scopesNames,
                                                                         string permissionsDescriptionsText,
                                                                         ref Dictionary<string, List<Dictionary<string, object>>> referencePermissionsDictionary,
                                                                         string topLevelDictionaryName = null)
        {

            PermissionsProcessor.ExtractPermissionsDescriptionsIntoDictionary(scopesNames, permissionsDescriptionsText, ref referencePermissionsDictionary, topLevelDictionaryName);
            Assert.NotEmpty(referencePermissionsDictionary);
        }

        private static async Task<string> GetGitHubPermissionsDescriptionsText()
        {
            return await Resources.GetFileContents(".\\Resources\\GitHubScopesDescriptions.json");
        }

        private static async Task<string> GetServicePrincipalPermissionsDescriptionsText()
        {
            var servicePrincipalDescriptions = await Resources.GetFileContents(".\\Resources\\ServicePrinicipalScopesDescriptions.json");

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

        private static (Dictionary<string, List<Dictionary<string, object>>> servicePrincipalPermissionsDict,
                        Dictionary<string, List<Dictionary<string, object>>> gitHubPermissionsDict)
                        GetPermissionsDescriptionsDictionaries()
        {
            var servicePrincipalPermissionsText = GetServicePrincipalPermissionsDescriptionsText().GetAwaiter().GetResult();
            var gitHubPermissionsText = GetGitHubPermissionsDescriptionsText().GetAwaiter().GetResult();
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
