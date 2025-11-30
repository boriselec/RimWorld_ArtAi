using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ArtAi.data;
using ArtAi.util;
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
                if (Queued.TryGetValue(description, out var rqUid))
                {
                    return Get(rqUid, description);
                }
                else
                {
                    return Enqueue(description);
                }
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                Queued.Clear();
                return GeneratedImage.Error();
            }
        }

        private static GeneratedImage Enqueue(Description description)
        {
            string prompt = (description.ThingDescription
                             + " " + description.ArtDescription)
                .Replace('\n', ' ')
                .Replace("  ", " ");
            Log.Message("AiArt. prompt: " + prompt);

            string postData = @"{
                ""prompt"": """ + prompt + @""",
                ""language"": """ + description.Language + @"""
            }";
            var rs = HttpUtil.DoPost("/prompt", postData);

            string rqUid = GetJsonField(rs, "prompt_id");
            string queuePosition = GetJsonField(rs, "artAiQueuePosition");

            if (string.IsNullOrWhiteSpace(rqUid))
            {
                Log.Error("No prompt_id");
                return GeneratedImage.Error();
            }
            Queued[description] = rqUid;
            return GeneratedImage.InProgress(QueuedMessage(queuePosition));
        }

        private static GeneratedImage Get(string rqUid, Description description)
        {
            var rs = HttpUtil.DoGetText("/history/" + rqUid);
            var filename = GetJsonField(rs, "filename");
            var queuePosition = GetJsonField(rs, "artAiQueuePosition");
            return filename == null
                ? GeneratedImage.InProgress(QueuedMessage(queuePosition))
                : Load(filename, description);
        }

        private static GeneratedImage Load(string filename, Description description)
        {
            var rs = HttpUtil.DoGetImage("/view?filename=" + filename);

            Texture2D tex = new Texture2D(2, 2, TextureFormat.Alpha8, true);
            tex.LoadImage(rs);
            tex.Apply();
            if (tex.NullOrBad() || tex.height * tex.width <= 64)
            {
                return GeneratedImage.Error();
            }

            return GeneratedImage.Done(tex, description.ArtDescription);
        }

        // This method uses regex for simple JSON field extraction instead of a full
        // JSON parser because the project targets .NET Framework 4.7.2,
        // where built-in JSON libraries like System.Text.Json are not available,
        // and external dependencies (e.g., Newtonsoft.Json) are avoided
        // to keep the mod lightweight.
        private static string GetJsonField(string json, string fieldName)
        {
            // The regex pattern matches JSON fields in the format "fieldName": value.
            // Examples of matches:
            // - "prompt_id": "abc123"
            // - "artAiQueuePosition": 5
            // - "filename": null
            // - "status": true
            string pattern = $@"""{fieldName}""\s*:\s*(?:""([^""]*)""|([^,\}}\]\s]*))";
            Match match = Regex.Match(json, pattern);
            if (match.Success)
            {
                string value = match.Groups[1].Success
                    ? match.Groups[1].Value
                    : match.Groups[2].Value;
                return string.IsNullOrEmpty(value) ? null : value;
            }
            return null;
        }

        private static string QueuedMessage(string queuePosition)
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
