// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System.Net.Http;

namespace PermissionsScraper.Common
{
    internal class HttpClientSingleton
    {
        private static HttpClientSingleton _instance;
        private static readonly object _lock = new object();
        private HttpClient _httpClient;

        private HttpClientSingleton()
        {
            _httpClient = new HttpClient();
        }

        public static HttpClientSingleton Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new HttpClientSingleton();
                    }

                    return _instance;
                }
            }
        }

        public HttpClient HttpClient
        {
            get
            {
                return _httpClient;
            }
        }
    }
}
