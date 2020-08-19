// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.Identity.Client;
using System;
using PermissionsScraper.Common;

namespace PermissionsScraper.Services
{
    internal static class AuthService
    {
        /// <summary>
        /// Gets authentication to a protected Web API.
        /// </summary>
        /// <param name="config">The application configuration settings.</param>
        /// <returns>An authentication result, if successful.</returns>
        internal static AuthenticationResult GetAuthentication(ApplicationConfig config)
        {
            IConfidentialClientApplication app;
            app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                  .WithClientSecret(config.ClientSecret)
                  .WithAuthority(new Uri(config.Authority))
                  .Build();

            string[] scopes = new string[] { $"{config.ApiUrl}.default" };

            return app.AcquireTokenForClient(scopes)
              .ExecuteAsync().GetAwaiter().GetResult();
        }
    }
}
