using System;
using System.Collections.Generic;
using ArtAi.data;
using ArtAi.data.comfyui;
using ArtAi.Server;
using ArtAi.util;
using ArtAi.util.json;

namespace ArtAi
{
    // Stateless network helpers. Safe to call from a background thread:
    // no Unity API and no Verse.Log here.
    public static class Generator
    {
        // Text sent to the server when the player did not edit the prompt
        public static string BuildPrompt(Description description)
        {
            return (description.ThingDescription
                    + " " + description.ArtDescription)
                .Replace('\n', ' ')
                .Replace("  ", " ");
        }

        // seed: null = the server picks it (the same prompt gives the same image)
        public static string Enqueue(string prompt, string language, int? seed)
        {
            var request = new Dictionary<string, object>
            {
                { "prompt", prompt },
                { "language", language }
            };
            if (seed != null)
            {
                request["seed"] = seed.Value;
            }

            var rs = HttpUtil.DoPost(ArtAiSettings.GetUrl() + "/prompt", request.ToJson());

            var promptRs = rs.FromJson<PromptRs>();
            string rqUid = promptRs.prompt_id;

            if (string.IsNullOrWhiteSpace(rqUid))
            {
                throw new Exception("Unexpected /prompt response");
            }
            return rqUid;
        }

        // filename != null when the image is ready; serverStatus: why it takes long, if the server says
        public static (string filename, int? queuePosition, ServerStatus serverStatus) PollHistory(string rqUid)
        {
            var rs = HttpUtil.DoGetText(ArtAiSettings.GetUrl() + "/history/" + rqUid);
            var historyRs = rs.FromJson<Dictionary<string, HistoryRsItem>>();
            if (!historyRs.TryGetValue(rqUid, out var item))
            {
                return (null, null, null);
            }
            if (item.artAiStatus == "error")
            {
                throw new Exception(item.artAiMessage ?? "Generation failed");
            }
            ServerStatus serverStatus = item.artAiMessage == null
                ? null
                : new ServerStatus(item.artAiStatus, item.artAiArgs, item.artAiMessage);
            return (item.Filename(), item.artAiQueuePosition, serverStatus);
        }

        public static byte[] DownloadImage(string filename)
        {
            var url = ArtAiSettings.GetUrl();
            return HttpUtil.DoGetImage(url + "/view?filename=" + filename);
        }
    }
}
