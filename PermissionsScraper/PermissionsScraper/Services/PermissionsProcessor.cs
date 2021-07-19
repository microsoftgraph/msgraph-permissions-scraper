// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PermissionsScraper.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PermissionsScraper.Services
{
    internal class PermissionsProcessor
    {
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

            foreach (string scopeName in scopesNames)
            {
                var permissionsDescriptions = permissionsDescriptionsToken?.Value<JArray>(scopeName)?.ToObject<List<Dictionary<string, object>>>();
                if (permissionsDescriptions == null) continue;

                if (referencePermissionsDescriptions.ContainsKey(scopeName) is false)
                {
                    referencePermissionsDescriptions.Add(scopeName, new List<Dictionary<string, object>>());
                }

                foreach (var permissionDescription in permissionsDescriptions)
                {
                    var id = permissionDescription["id"];
                    var permissionExists = referencePermissionsDescriptions[scopeName].Exists(x => x.ContainsValue(id));
                    if (permissionExists is false)
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

                        if (referencePermissionsText.Equals(updatablePermissionText, StringComparison.OrdinalIgnoreCase) is not true)
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

                foreach (var missingRefPermission in missingRefPermissions)
                {
                    updatablePermissions[refPermissionKey].Remove(missingRefPermission);
                    permissionsUpdated = true;
                }
            }

            return permissionsUpdated;
        }
    }
}
