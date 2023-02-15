using System.IO;
using System.Net;
using System.Text;
using ArtAi.data;
using UnityEngine;

namespace ArtAi
{
    public abstract class Generator
    {
        public static GeneratedImage Generate(Description description)
        {
            var request = MakeRequest(description.ArtDescription, description.ThingDescription);

            using (var response = request.GetResponse())
            {
                using (var rsDataStream = response.GetResponseStream())
                {
                    return ProcessResponse(rsDataStream, response.ContentType);
                }
            }
        }

        private static WebRequest MakeRequest(string artDescription, string thingDescription)
        {
            var request = WebRequest.Create("http://localhost:8080/generate");
            request.Method = "POST";
            var postData = artDescription + ';' + thingDescription;
            var byteArray = Encoding.UTF8.GetBytes(postData);
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteArray.Length;
            using (var rqDataStream = request.GetRequestStream())
            {
                rqDataStream.Write(byteArray, 0, byteArray.Length);
                rqDataStream.Close();
            }
            return request;
        }

        private static GeneratedImage ProcessResponse(Stream response, string contentType)
        {
            if (response == null)
            {
                return GeneratedImage.Error();
            }

            switch (contentType)
            {
                case "text":
                    using (var reader = new StreamReader(response))
                    {
                        var responseFromServer = reader.ReadToEnd();
                        return GeneratedImage.InProgress(responseFromServer);
                    }
                case "image/png":
                    using (var ms = new MemoryStream())
                    {
                        response.CopyTo(ms);
                        var array = ms.ToArray();
                        Texture2D tex = new Texture2D(2, 2, TextureFormat.Alpha8, true);
                        tex.LoadImage(array);
                        tex.Apply();
                        return GeneratedImage.Done(tex);
                    }
                default:
                    return GeneratedImage.Error();
            }
        }
    }
}