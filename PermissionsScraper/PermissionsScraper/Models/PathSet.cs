// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace PermissionsScraper.Models
{
    internal class PathSet
    {
        [JsonProperty(PropertyName = "schemeKeys")]
        public HashSet<string> SchemeKeys
        {
            get; set;
        } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        [JsonProperty(PropertyName = "methods")]
        public HashSet<string> Methods
        {
            get; set;
        } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        [JsonProperty(PropertyName = "excludedProperties")]
        public HashSet<string> ExcludedProperties
        {
            get; set;
        } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        [JsonProperty(PropertyName = "includedProperties")]
        public HashSet<string> IncludedProperties
        {
            get; set;
        } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// A dictionary of paths and the schemes where the permission is least privileged 
        /// </summary>
        [JsonProperty(PropertyName = "paths")]
        public Dictionary<string, string> Paths
        {
            get; set;
        } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
