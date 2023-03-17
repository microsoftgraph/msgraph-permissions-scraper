// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Newtonsoft.Json;

namespace PermissionsScraper.Models
{
    internal class SchemeInformation
    {
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

        [JsonProperty(PropertyName = "userDisplayName")]
        public string UserDisplayName
        {
            get; set;
        }

        [JsonProperty(PropertyName = "userDescription")]
        public string UserDescription
        {
            get; set;
        }

        [JsonProperty(PropertyName = "requiresAdminConsent")]
        public bool RequiresAdminConsent
        {
            get; set;
        }
    }
}
