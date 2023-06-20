// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using PermissionsScraper.Common;
using PermissionsScraper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Octokit;

namespace PermissionsScraper.Services
{
    internal class PermissionsProcessor
    {
        private const string DelegatedWork = "DelegatedWork";
        private const string DelegatedPersonal = "DelegatedPersonal";
        private const string Application = "Application";

        /// <summary>
        /// Extracts permissions descriptions from a string input source
        /// and adds them to a target permissions descriptions dictionary.
        /// </summary>
        /// <param name="scopesNames">The scope names to retrieve from the permissions descriptions.</param>
        /// <param name="permissionsDescriptionsText">The string input with permissions descriptions.</param>
        /// <param name="referencePermissionsDictionary">The target permissions descriptions dictionary which the extracted permissions will be added into.</param>
        public static void ExtractPermissionsDescriptionsIntoDictionary(string[] scopesNames,
                                                                        string permissionsDescriptionsText,
                                                                        ref Dictionary<string, List<Dictionary<string, object>>> referencePermissionsDictionary,
                                                                        string topLevelDictionaryName = null)
        {
            UtilityFunctions.CheckArgumentNull(scopesNames, nameof(scopesNames));
            UtilityFunctions.CheckArgumentNull(referencePermissionsDictionary, nameof(referencePermissionsDictionary));
            UtilityFunctions.CheckArgumentNullOrEmpty(permissionsDescriptionsText, nameof(permissionsDescriptionsText));

            var permissionsDescriptionsToken = topLevelDictionaryName is null
                ? JsonConvert.DeserializeObject<JObject>(permissionsDescriptionsText)
                : JsonConvert.DeserializeObject<JObject>(permissionsDescriptionsText).Value<JArray>(topLevelDictionaryName)?.First;

            ExtractPermissionsDescriptionsIntoDictionary(scopesNames, permissionsDescriptionsToken, ref referencePermissionsDictionary);
        }

        /// <summary>
        /// Extracts permissions descriptions from schemes source and adds them to a target permissions descriptions dictionary.
        /// </summary>
        /// <param name="permissionsDocument">The <see cref="PermissionsDocument"/> input with permissions descriptions.</param>
        /// <returns>A dictionary of all permissions grouped by permission scheme.</returns>
        public static Dictionary<string, List<ScopeInformation>> ExtractPermissionDescriptionsIntoDictionary(
            PermissionsDocument permissionsDocument)
        {
            UtilityFunctions.CheckArgumentNull(permissionsDocument, nameof(permissionsDocument));

            var permissionDescriptions = new Dictionary<string, List<ScopeInformation>>(StringComparer.OrdinalIgnoreCase)
            {
                { DelegatedWork, new List<ScopeInformation>() },
                { DelegatedPersonal, new List<ScopeInformation>() },
                { Application, new List<ScopeInformation>() }
            };

            foreach (var permission in permissionsDocument.Permissions)
            {
                foreach (var schemesDescriptions in permission.Value.Schemes)
                {
                    if (!permissionDescriptions.TryGetValue(schemesDescriptions.Key, out var allSchemePermissions))
                    {
                        throw new InvalidOperationException($"Invalid scheme key {schemesDescriptions.Key}");
                    }

                    var scopeInformation = new ScopeInformation()
                    {
                        ScopeName = permission.Key,
                        AdminDisplayName = schemesDescriptions.Value.AdminDisplayName,
                        AdminDescription= schemesDescriptions.Value.AdminDescription,
                        ConsentDisplayName = schemesDescriptions.Value.UserDisplayName,
                        ConsentDescription = schemesDescriptions.Value.UserDescription,
                        IsAdmin = schemesDescriptions.Value.RequiresAdminConsent,
                        IsHidden = permission.Value.ProvisioningInfo.IsHidden
                    };
                    allSchemePermissions.Add(scopeInformation);
                }
            }

            foreach (var schemeScopes in permissionDescriptions.Values)
            {
                schemeScopes.Sort((x, y) => x.ScopeName.CompareTo(y.ScopeName));
            }

            return permissionDescriptions;
        }

        /// <summary>
        /// Extracts permissions descriptions from a <see cref="JToken"/> source
        /// and adds them to a target permissions descriptions dictionary.
        /// </summary>
        /// <param name="scopesNames">The scope names to retrieve from the permissions descriptions.</param>
        /// <param name="permissionsDescriptionsToken">The <see cref="JToken"/> input with permissions descriptions.</param>
        /// <param name="referencePermissionsDescriptions">The target permissions descriptions dictionary which the extracted permissions will be added into.</param>
        private static void ExtractPermissionsDescriptionsIntoDictionary(string[] scopesNames,
                                                                         JToken permissionsDescriptionsToken,
                                                                         ref Dictionary<string, List<Dictionary<string, object>>> referencePermissionsDescriptions)
        {
            UtilityFunctions.CheckArgumentNull(scopesNames, nameof(scopesNames));
            UtilityFunctions.CheckArgumentNull(permissionsDescriptionsToken, nameof(permissionsDescriptionsToken));
            UtilityFunctions.CheckArgumentNull(referencePermissionsDescriptions, nameof(referencePermissionsDescriptions));

            foreach (var scopeName in scopesNames)
            {
                var permissionsDescriptions = permissionsDescriptionsToken?.Value<JArray>(scopeName)?.ToObject<List<Dictionary<string, object>>>();
                if (permissionsDescriptions == null) continue;

                if (!referencePermissionsDescriptions.ContainsKey(scopeName))
                {
                    referencePermissionsDescriptions.Add(scopeName, new List<Dictionary<string, object>>());
                }

                foreach (var permissionDescription in permissionsDescriptions)
                {
                    var id = permissionDescription["id"];
                    var permissionExists = referencePermissionsDescriptions[scopeName].Exists(x => x.ContainsValue(id));
                    if (!permissionExists)
                    {
                        referencePermissionsDescriptions[scopeName].Add(permissionDescription);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the permissions descriptions in the target source from the reference source if there is variance
        /// between the two sets of permissions descriptions sources.
        /// </summary>
        /// <param name="referencePermissions">The reference permissions descriptions source to compare from.</param>
        /// <param name="updatablePermissions">The target permissions descriptions source to compare against.</param>
        /// <returns>True, if permissions have been updated in the target source, otherwise false.</returns>
        public static bool UpdatePermissionsDescriptions(Dictionary<string, List<Dictionary<string, object>>> referencePermissions,
                                                         ref Dictionary<string, List<Dictionary<string, object>>> updatablePermissions)
        {
            UtilityFunctions.CheckArgumentNull(referencePermissions, nameof(referencePermissions));
            UtilityFunctions.CheckArgumentNull(updatablePermissions, nameof(updatablePermissions));

            bool permissionsUpdated = false;

            /* Search for permissions from the reference permissions dictionary
             * that are either missing or different (with same id)
             * from the updatable permissions dictionary.
             */
            foreach (var refPermissionKey in referencePermissions.Keys)
            {
                foreach (var referencePermission in referencePermissions[refPermissionKey])
                {
                    var id = referencePermission["id"];
                    var updatablePermission = updatablePermissions[refPermissionKey].FirstOrDefault(x => x["id"].Equals(id));
                    if (updatablePermission is null)
                    {
                        // New permission in reference - add
                        updatablePermissions[refPermissionKey].Insert(0, referencePermission);
                        permissionsUpdated = true;
                    }
                    else
                    {
                        // Permissions match by id - check whether contents need updating
                        var referencePermissionsText = JsonConvert.SerializeObject(referencePermission, Formatting.Indented);
                        var updatablePermissionText = JsonConvert.SerializeObject(updatablePermission, Formatting.Indented);

                        if (!referencePermissionsText.Equals(updatablePermissionText, StringComparison.OrdinalIgnoreCase))
                        {
                            // Permission updated in reference - remove then add
                            var index = updatablePermissions[refPermissionKey].FindIndex(x => x["id"].Equals(id));
                            updatablePermissions[refPermissionKey].RemoveAt(index);
                            updatablePermissions[refPermissionKey].Insert(index, referencePermission);
                            permissionsUpdated = true;
                        }
                    }
                }

                /* Search for permissions from the updatable permissions dictionary
                 * that are missing from the reference permissions dictionary.
                 * These need to be removed from the updatable permissions dictionary.
                 */
                var missingRefPermissions = new List<Dictionary<string, object>>();
                foreach (var updatablePermission in updatablePermissions[refPermissionKey])
                {
                    var id = updatablePermission["id"];
                    var referencePermission = referencePermissions[refPermissionKey].FirstOrDefault(x => x["id"].Equals(id));
                    if (referencePermission is null)
                    {
                        missingRefPermissions.Add(updatablePermission);
                    }
                }

                updatablePermissions[refPermissionKey].RemoveAll(PermissionDeleted);

                bool PermissionDeleted(Dictionary<string, object> permission)
                    => missingRefPermissions.Exists(x => x.ContainsValue(permission["id"]));
            }

            return permissionsUpdated;
        }

        /// <summary>
        /// Creates permissions reverse lookup table from permissions document obtained from workloads repo.
        /// </summary>
        /// <param name="permissionsDocument">The <see cref="PermissionsDocument"/> input with permissions descriptions.</param>
        /// <returns>Permissions reverse lookup table with the key as the request URL.</returns>
        public static Dictionary<string, Dictionary<string, Dictionary<string, SchemePermissions>>> CreatePermissionsReverseLookupTable(
            PermissionsDocument permissionsDocument)
        {
            UtilityFunctions.CheckArgumentNull(permissionsDocument, nameof(permissionsDocument));
        
            var allLeastPrivilegePermissions = new Dictionary<string, Dictionary<string, Dictionary<string, HashSet<string>>>>(); // path => scheme => methods => permission
            var reverseLookupTable = new Dictionary<string, Dictionary<string, Dictionary<string, SchemePermissions>>>();
            foreach (var permission in permissionsDocument.Permissions)
            {
                foreach (var pathSet in permission.Value.PathSets)
                {
                    foreach (var path in pathSet.Paths)
                    {
                        if (!reverseLookupTable.TryGetValue(path.Key, out var pathPermissions))
                        {
                            pathPermissions = new Dictionary<string, Dictionary<string, SchemePermissions>>();
                            reverseLookupTable.Add(path.Key, pathPermissions);
                        }

                        var leastPrivilegeSchemes = !string.IsNullOrEmpty(path.Value) 
                            ? path.Value.Split("=")[1].Split(',', StringSplitOptions.RemoveEmptyEntries) 
                            : new string[0];

                        foreach (var method in pathSet.Methods)
                        {
                            if (!pathPermissions.TryGetValue(method, out var supportedMethodPermissions))
                            {
                                supportedMethodPermissions = new Dictionary<string, SchemePermissions>();
                                pathPermissions.Add(method, supportedMethodPermissions);
                            }

                            foreach (var schemeKey in pathSet.SchemeKeys)
                            {
                                if (!supportedMethodPermissions.TryGetValue(schemeKey, out var schemePermissions))
                                {
                                    schemePermissions = new SchemePermissions();
                                    supportedMethodPermissions.Add(schemeKey, schemePermissions);
                                }

                                schemePermissions.AllPermissions.Add(permission.Key);
                                if (leastPrivilegeSchemes.Contains(schemeKey))
                                {
                                    if (!allLeastPrivilegePermissions.TryGetValue(path.Key, out var schemesLeastPrivilegePermissions))
                                    {
                                        schemesLeastPrivilegePermissions = new Dictionary<string, Dictionary<string, HashSet<string>>>();
                                        allLeastPrivilegePermissions.Add(path.Key, schemesLeastPrivilegePermissions);
                                    }

                                   if(!schemesLeastPrivilegePermissions.TryGetValue(schemeKey, out var methodLeastPrivilegePermissions))
                                   {
                                        methodLeastPrivilegePermissions = new Dictionary<string, HashSet<string>>();
                                        schemesLeastPrivilegePermissions.Add(schemeKey, methodLeastPrivilegePermissions);
                                   }

                                   var combinedMethods = string.Join(",", pathSet.Methods);
                                   if (!methodLeastPrivilegePermissions.TryGetValue(combinedMethods, out var leastPrivilegePermissions))
                                   {
                                        leastPrivilegePermissions = new HashSet<string>();
                                        methodLeastPrivilegePermissions.Add(combinedMethods, leastPrivilegePermissions);
                                   }
                                   leastPrivilegePermissions.Add(permission.Key);
                                }
                            }
                        }
                    }
                }
            }
            PopulateLeastPrivilegePermissions(ref reverseLookupTable, allLeastPrivilegePermissions);
            return reverseLookupTable.OrderBy(x => x.Key).ToDictionary(k => k.Key, v => v.Value);
        }

        public static void PopulateLeastPrivilegePermissions(
            ref Dictionary<string, Dictionary<string, Dictionary<string, SchemePermissions>>> reverseLookupTable,
            Dictionary<string, Dictionary<string, Dictionary<string, HashSet<string>>>> allLeastPrivilegePermissions)
        {
            foreach (var pathLeastPrivilegePermissions in allLeastPrivilegePermissions)
            {
                var path = pathLeastPrivilegePermissions.Key;
                foreach (var schemeLeastPrivilegePermissions in pathLeastPrivilegePermissions.Value)
                {
                    var schemeKey = schemeLeastPrivilegePermissions.Key;
                    var coveredMethods = new HashSet<string>();
                    foreach (var methodLeastPrivilegePermissions in schemeLeastPrivilegePermissions.Value.OrderBy(x => x.Key.Length))
                    {
                        var supportedMethods = methodLeastPrivilegePermissions.Key.Split(",").ToList();
                        supportedMethods = supportedMethods.Except(coveredMethods).ToList();
                        foreach (var method in supportedMethods)
                        {
                            foreach (var permission in methodLeastPrivilegePermissions.Value)
                            {
                                reverseLookupTable[path][method][schemeKey].LeastPrivilegePermissions.Add(permission);

                            }
                            coveredMethods.Add(method);
                        }
                    }
                }
            }
        }
    }
}
