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
                var language = LanguageDatabase.activeLanguage.folderName;
                var request = MakeRequest(description.ArtDescription, description.ThingDescription,
                    steamAccountID, language);

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
                Log.Error("ArtAi Generator " + e.Message);
                return GeneratedImage.Error();
            }
        }

        private static WebRequest MakeRequest(string artDescription, string thingDescription, uint steamAccountID,
            string language)
        {
            var serverUrl = ArtAiSettings.ServerUrl;
            var request = WebRequest.Create(serverUrl);
            request.Method = "POST";
            var postData = artDescription + ';' + thingDescription + ';' + steamAccountID + ';' + language;
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
                Log.Message("ArtAi Generator error");
                return GeneratedImage.Error();
            }

            switch (contentType)
            {
                case "text":
                case "text/plain":
                    Log.Message("ArtAi Generator in progress");
                    using (var reader = new StreamReader(response))
                    {
                        var responseFromServer = reader.ReadToEnd();
                        return GeneratedImage.InProgress(responseFromServer);
                    }
                case "image/png":
                    Log.Message("ArtAi Generator get image");
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
                    Log.Message("ArtAi Generator error");
                    return GeneratedImage.Error();
            }
        }
    }
}