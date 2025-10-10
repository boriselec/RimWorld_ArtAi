using System;
using System.Net;
using System.Net.Http;
using System.Text;

namespace ArtAi.util
{
    public static class HttpUtil
    {
        private static readonly HttpClient client = new HttpClient {
            Timeout = TimeSpan.FromSeconds(3)
        };

        public static string DoPost(string path, string postData)
        {
            var url = GetBaseUrl() + path;
            var content = new StringContent(
                postData,
                Encoding.UTF8,
                "application/json");
            var response = client.PostAsync(url, content).Result;
            ThrowIfError(response.StatusCode);
            return response.Content.ReadAsStringAsync().Result;
        }

        public static string DoGetText(string path)
        {
            var url = GetBaseUrl() + path;
            var response = client.GetAsync(url).Result;
            ThrowIfError(response.StatusCode);
            return response.Content.ReadAsStringAsync().Result;
        }

        public static byte[] DoGetImage(string path)
        {
            var url = GetBaseUrl() + path;
            var response = client.GetAsync(url).Result;
            ThrowIfError(response.StatusCode);
            return response.Content.ReadAsByteArrayAsync().Result;
        }

        private static string GetBaseUrl() => ArtAiSettings.ServerUrl.TrimEnd('/');

        private static void ThrowIfError(HttpStatusCode code)
        {
            if ((int)code >= 400)
            {
                throw new HttpRequestException("Error code: " + code);
            }
        }
    }
}
