using System;
using System.Collections.Generic;
using ArtAi.data;
using ArtAi.data.comfyui;
using ArtAi.util;
using ArtAi.util.json;
using UnityEngine;
using Verse;

namespace ArtAi
{
    public static class Generator
    {
        private static readonly Dictionary<Description, string> Queued
            = new Dictionary<Description, string>();

        public static GeneratedImage GetOrEnqueue(Description description)
        {
            try
            {
                if (!Queued.TryGetValue(description, out var rqUid))
                {
                    rqUid = Enqueue(description);
                    Queued[description] = rqUid;
                }
                return Get(rqUid, description);
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                Queued.Clear();
                return GeneratedImage.Error();
            }
        }

        private static string Enqueue(Description description)
        {
            string prompt = (description.ThingDescription
                             + " " + description.ArtDescription)
                .Replace('\n', ' ')
                .Replace("  ", " ");
            Log.Message("AiArt. prompt: " + prompt);

            string postData = new Dictionary<string, object>
            {
                { "prompt", prompt },
                { "language", description.Language }
            }.ToJson();

            var rs = HttpUtil.DoPost(ArtAiSettings.GetUrl() + "/prompt", postData);

            var promptRs = rs.FromJson<PromptRs>();
            string rqUid = promptRs.prompt_id;

            if (string.IsNullOrWhiteSpace(rqUid))
            {
                throw new Exception("Unexpected /prompt response");
            }
            return rqUid;
        }

        private static GeneratedImage Get(string rqUid, Description description)
        {
            var rs = HttpUtil.DoGetText(ArtAiSettings.GetUrl() + "/history/" + rqUid);
            var historyRs = rs.FromJson<Dictionary<string, HistoryRsItem>>();
            var (filename, queuePosition) = historyRs.TryGetValue(rqUid, out var item)
                ? (item.Filename(), item.artAiQueuePosition)
                : (null, null);

            return filename == null
                ? GeneratedImage.InProgress(QueuedMessage(queuePosition))
                : Load(filename, description);
        }

        private static GeneratedImage Load(string filename, Description description)
        {
            var url = ArtAiSettings.GetUrl();
            var rs = HttpUtil.DoGetImage(url + "/view?filename=" + filename);

            Texture2D tex = new Texture2D(2, 2, TextureFormat.Alpha8, true);
            tex.LoadImage(rs);
            tex.Apply();
            if (tex.NullOrBad() || tex.height * tex.width <= 64)
            {
                throw new Exception("Broken texture");
            }

            return GeneratedImage.Done(tex, description.ArtDescription);
        }

        private static string QueuedMessage(int? queuePosition)
        {
            string result = "AiArtInProgress".Translate();
            if (queuePosition != null)
            {
                result = result
                    + Environment.NewLine
                    + Environment.NewLine
                    + "AiArtQueuePosition".Translate()
                    + queuePosition;
            }
            return result;
        }
    }
}
