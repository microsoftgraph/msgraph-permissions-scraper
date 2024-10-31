// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Newtonsoft.Json;

namespace PermissionsScraper.Models
{
    internal class ScopeInformation
    {
        [JsonProperty(PropertyName = "value")]
        public string ScopeName
        {
            get; set;
        }

        [JsonProperty(PropertyName = "adminDisplayName")]
        public string AdminDisplayName
        {
            get; set;
        }

        [JsonProperty(PropertyName = "adminDescription")]
        public string AdminDescription
        {
            get; set;
        }

        [JsonProperty(PropertyName = "consentDisplayName")]
        public string ConsentDisplayName
        {
            get; set;
        }

        [JsonProperty(PropertyName = "consentDescription")]
        public string ConsentDescription
        {
            get; set;
        }

        [JsonProperty(PropertyName = "isAdmin")]
        public bool IsAdmin
        {
            get; set;
        }

        [JsonProperty(PropertyName = "isHidden")]
        public bool IsHidden
        {
            get; set;
        } = false;
    }
}
