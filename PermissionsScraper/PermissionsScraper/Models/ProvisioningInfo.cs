// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PermissionsScraper.Models
{
    internal class ProvisioningInfo
    {
        [JsonProperty(PropertyName = "isHidden")]
        public bool IsHidden
        {
            get; set;
        } = false;

        [JsonProperty(PropertyName = "requiredEnvironments")]
        public List<string> RequiredEnvironments
        {
            get; set;
        } = new List<string>();

        [JsonProperty(PropertyName = "resourceAppId")]
        public string ResourceAppId
        {
            get; set;
        }

        [JsonProperty(PropertyName = "ownerSecurityGroup")]
        public string OwnerSecurityGroup
        {
            get; set;
        }
    }
}
