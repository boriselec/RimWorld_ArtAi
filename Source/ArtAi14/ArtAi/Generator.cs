using System;
using System.IO;
using System.Net;
using System.Text;
using ArtAi.data;
using Steamworks;
using UnityEngine;
using Verse;

namespace ArtAi
{
    public abstract class Generator
    {
        public static GeneratedImage Generate(Description description)
        {
            try
            {
                var steamAccountID = SteamUser.GetSteamID().GetAccountID().m_AccountID;
                var request = MakeRequest(description.ArtDescription, description.ThingDescription, steamAccountID);

                using (var response = request.GetResponse())
                {
                    using (var rsDataStream = response.GetResponseStream())
                    {
                        return ProcessResponse(rsDataStream, response.ContentType);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return GeneratedImage.Error();
            }
        }

        private static WebRequest MakeRequest(string artDescription, string thingDescription, uint steamAccountID)
        {
            var serverUrl = ArtAiSettings.ServerUrl;
            var request = WebRequest.Create(serverUrl);
            request.Method = "POST";
            var postData = artDescription + ';' + thingDescription + ';' + steamAccountID;
            var byteArray = Encoding.UTF8.GetBytes(postData);
            request.ContentType = "text/plain";
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
                case "text/plain":
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