using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ArtAi.util
{
    public static class HttpUtil
    {
        private static readonly HttpClient client = new HttpClient {
            Timeout = TimeSpan.FromSeconds(10)
        };

        public static string DoPost(string url, string postData)
        {
            BgLog.Message("POST " + url);
            var content = new StringContent(
                postData,
                Encoding.UTF8,
                "application/json");
            var response = Send(() => client.PostAsync(url, content));
            ThrowIfError(response.StatusCode);
            return response.Content.ReadAsStringAsync().Result;
        }

        public static string DoGetText(string url)
        {
            BgLog.Message("GET " + url);
            var response = Send(() => client.GetAsync(url));
            ThrowIfError(response.StatusCode);
            return response.Content.ReadAsStringAsync().Result;
        }

        public static byte[] DoGetImage(string url)
        {
            BgLog.Message("GET " + url);
            var response = Send(() => client.GetAsync(url));
            ThrowIfError(response.StatusCode);
            return response.Content.ReadAsByteArrayAsync().Result;
        }

        // Connection errors and timeouts come wrapped in AggregateException from .Result
        private static HttpResponseMessage Send(Func<Task<HttpResponseMessage>> request)
        {
            try
            {
                return request().Result;
            }
            catch (AggregateException e)
            {
                throw new ServerUnavailableException(e.GetBaseException().Message);
            }
        }

        private static void ThrowIfError(HttpStatusCode code)
        {
            if ((int)code >= 400)
            {
                throw new HttpRequestException("Error code: " + code);
            }
        }
    }

    // The server did not answer at all (not running, unreachable, timeout)
    public class ServerUnavailableException : Exception
    {
        public ServerUnavailableException(string message) : base(message)
        {
        }
    }
}
