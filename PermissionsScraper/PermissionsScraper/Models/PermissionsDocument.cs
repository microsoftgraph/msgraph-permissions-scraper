// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace PermissionsScraper.Models
{
    internal class PermissionsDocument
    {
        [JsonProperty(PropertyName = "$schema")]
        public string Schema
        {
            get; set;
        }

        [JsonProperty(PropertyName = "permissions")]
        public Dictionary<string, PermissionInfo> Permissions
        {
            get; set;
        } = new Dictionary<string, PermissionInfo>();

        public static PermissionsDocument Load(string jsonText)
        {
            return JsonConvert.DeserializeObject<PermissionsDocument>(jsonText);
        }
    }

    internal class PermissionInfo
    {

        [JsonProperty(PropertyName = "schemes")]
        public Dictionary<string, SchemeInformation> Schemes
        {
            get; set;
        } = new Dictionary<string, SchemeInformation>();


        [JsonProperty(PropertyName = "pathSets")]
        public List<PathSet> PathSets
        {
            get; set;
        } = new List<PathSet>();


        [JsonProperty(PropertyName = "provisioningInfo")]
        public ProvisioningInfo ProvisioningInfo
        {
            get; set;
        } = new ProvisioningInfo();
    }
}
