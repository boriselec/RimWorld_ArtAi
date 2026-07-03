using System;
using System.Collections.Generic;
using ArtAi.data;
using ArtAi.data.comfyui;
using ArtAi.util;
using ArtAi.util.json;

namespace ArtAi
{
    // Stateless network helpers. Safe to call from a background thread:
    // no Unity API and no Verse.Log here.
    public static class Generator
    {
        public static string Enqueue(Description description)
        {
            string prompt = (description.ThingDescription
                             + " " + description.ArtDescription)
                .Replace('\n', ' ')
                .Replace("  ", " ");

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

        public static (string filename, int? queuePosition) PollHistory(string rqUid)
        {
            var rs = HttpUtil.DoGetText(ArtAiSettings.GetUrl() + "/history/" + rqUid);
            var historyRs = rs.FromJson<Dictionary<string, HistoryRsItem>>();
            return historyRs.TryGetValue(rqUid, out var item)
                ? (item.Filename(), item.artAiQueuePosition)
                : (null, null);
        }

        public static byte[] DownloadImage(string filename)
        {
            var url = ArtAiSettings.GetUrl();
            return HttpUtil.DoGetImage(url + "/view?filename=" + filename);
        }
    }
}
