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
                Log.Message("AI Art request");
                var steamAccountID = SteamAccountID();
                var request = MakeRequest(description.ArtDescription, description.ThingDescription,
                    steamAccountID, description.Language);

                using (var response = request.GetResponse())
                {
                    using (var rsDataStream = response.GetResponseStream())
                    {
                        return ProcessResponse(rsDataStream, response.ContentType, description);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return GeneratedImage.Error();
            }
        }

        private static WebRequest MakeRequest(string artDescription, string thingDescription, string steamAccountID,
            string language)
        {
            var serverUrl = ArtAiSettings.ServerUrl;
            var request = WebRequest.Create(serverUrl);
            request.Method = "POST";
            var postData = Serialize(artDescription, thingDescription, steamAccountID, language);
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

        private static string Serialize(string artDescription, string thingDescription, string steamAccountID,
            string language)
        {
            const string delimiter = ";";
            return string.Join(delimiter,
                artDescription.Replace(delimiter, ""),
                thingDescription.Replace(delimiter, ""),
                steamAccountID.Replace(delimiter, ""),
                language.Replace(delimiter, ""));
        }

        private static GeneratedImage ProcessResponse(Stream response, string contentType, Description description)
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
                        var processedResponse = TranslateResponse(responseFromServer);
                        return GeneratedImage.InProgress(processedResponse);
                    }
                case "image/png":
                    using (var ms = new MemoryStream())
                    {
                        response.CopyTo(ms);
                        var array = ms.ToArray();
                        Texture2D tex = new Texture2D(2, 2, TextureFormat.Alpha8, true);
                        tex.LoadImage(array);
                        tex.Apply();
                        if (tex.NullOrBad())
                        {
                            return GeneratedImage.Error();
                        }
                        return GeneratedImage.Done(tex, description.ArtDescription);
                    }
                default:
                    return GeneratedImage.Error();
            }
        }

        private static string TranslateResponse(string responseFromServer)
        {
            const string queued = "Queued: ";
            const int approximateGenerationTimeSeconds = 30;

            if (responseFromServer.Contains(queued))
            {
                string queuePos = responseFromServer.Substring(responseFromServer.IndexOf(queued) + queued.Length);
                int waitTimeSeconds = (int.Parse(queuePos) + 1) * approximateGenerationTimeSeconds;
                return "AiArtInProgress".Translate()
                       + Environment.NewLine + Environment.NewLine +
                       "AiArtTimeReaming".Translate() + TimeSpan.FromSeconds(waitTimeSeconds).ToString();
            }

            if (responseFromServer.Contains("Try later"))
            {
                return "AiArtLimit".Translate();
            }

            return responseFromServer;
        }

        private static string SteamAccountID()
        {
            try
            {
                return SteamUser.GetSteamID().GetAccountID().m_AccountID.ToString();
            }
            catch (InvalidOperationException)
            {
                return "unknown";
            }
        }
    }
}