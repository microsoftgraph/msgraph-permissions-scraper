// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PermissionsScrapper
{
    public static class PermsDescription
    {
        static string _scopesDescriptionsJson = null;

        [FunctionName("DescriptionsScrapper")]
        public static void Run([TimerTrigger("0 */2 * * * *")]TimerInfo myTimer, ILogger log)
        {
            try
            {
                ApplicationConfig config = ApplicationConfig.ReadFromJsonFile("local.settings.json");

                var result = GetAuthentication(config);

                if (result != null)
                {
                    var scopesDescriptionsJson = GetScopesDescriptionsJson(config, result);

                    if (!scopesDescriptionsJson.Equals(_scopesDescriptionsJson, StringComparison.OrdinalIgnoreCase))
                    {
                        _scopesDescriptionsJson = scopesDescriptionsJson;

                        // Save file to directory/call API with json file to upload to GitHub
                    }
                }
                else
                {
                    log.LogInformation($"Failed to get authentication at: {DateTime.Now}");
                }
            }
            catch (Exception ex)
            {
                log.LogInformation($"Exception occurred: {ex.InnerException.Message ?? ex.Message}\r\n Time of occurrence {DateTime.Now}");
            }
        }

        private static AuthenticationResult GetAuthentication(ApplicationConfig config)
        {
            IConfidentialClientApplication app;
            app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                  .WithClientSecret(config.ClientSecret)
                  .WithAuthority(new Uri(config.Authority))
                  .Build();

            string[] scopes = new string[] { $"{config.ApiUrl}.default" };

            try
            {
                return app.AcquireTokenForClient(scopes)
               .ExecuteAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static string GetScopesDescriptionsJson(ApplicationConfig config, AuthenticationResult result)
        {
            try
            {
                var httpClient = new HttpClient();
                var apiCaller = new ProtectedApiCallHelper(httpClient);
                var spJson = apiCaller
                    .CallWebApiAsync($"{config.ApiUrl}{config.ApiVersions[0]}/serviceprincipals?$filter=appId eq '{config.ServicePrincipalId}'", result.AccessToken)
                    .GetAwaiter().GetResult(); // fetch for v1.0 ; no business case for fetching for beta yet

                var cleanedSpJson = CleanJsonData(spJson, config);


                // Retrieve the respective scopes
                var spValue = JsonConvert.DeserializeObject<JObject>(cleanedSpJson).Value<JArray>("value");

                var scopesDescriptions = new Dictionary<string, List<Dictionary<string, object>>>();

                foreach (var item in config.ScopesNamesList)
                {
                    var scopeDescriptions = spValue.First.Value<JArray>(item).ToObject<List<Dictionary<string, object>>>();
                    scopesDescriptions[item] = scopeDescriptions;
                }

                return JsonConvert.SerializeObject(scopesDescriptions, Formatting.Indented);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static string CleanJsonData(string spJson, ApplicationConfig config)
        {
            string cleanedSpJson = spJson;

            foreach (var item in config.RegexPatterns)
            {
                Regex regex = new Regex(item.Value, RegexOptions.IgnoreCase);
                var replacement = config.RegexReplacements[item.Key];
                cleanedSpJson = regex.Replace(cleanedSpJson, replacement);
            }

            return cleanedSpJson;
        }
    }
}
