using System.Threading;
using UnityEngine;
using Verse;

namespace ArtAi.Server
{
    // Which server the mod sends requests to: drawn in the mod settings and in the server window
    public static class ServerChoice
    {
        private const float RowHeight = 30f;
        private const float ButtonWidth = 110f;
        private const float LabelPct = 0.3f;

        // "Test" button; the result is shown only for the address it was started with
        private static volatile ServerCheck _testResult;
        private static volatile bool _testRunning;
        private static string _testedUrl;

        public static void Draw(Listing_Standard listing)
        {
            string url = ArtAiSettings.GetUrl();
            if (listing.RadioButton("AiArtServerPublic".Translate(), url == ArtAiSettings.PublicServerUrl))
            {
                ArtAiSettings.SetServerUrl(ArtAiSettings.PublicServerUrl);
            }
            if (listing.RadioButton("AiArtServerLocal".Translate(LocalServer.Url), LocalServer.IsSelected))
            {
                ArtAiSettings.SetServerUrl(LocalServer.Url);
            }

            // the address in use: filled by the choice above, or typed in (e.g. a server on another computer)
            Rect row = listing.GetRect(RowHeight);
            Widgets.Label(row.LeftPart(LabelPct), "AiArtServerUrl".Translate());
            Rect field = new Rect(row.x + row.width * LabelPct, row.y,
                row.width * (1f - LabelPct) - ButtonWidth - 10f, row.height);
            ArtAiSettings.ServerUrl = Widgets.TextField(field, ArtAiSettings.ServerUrl);
            if (Widgets.ButtonText(row.RightPartPixels(ButtonWidth), "AiArtServerTest".Translate(),
                    active: !_testRunning))
            {
                StartTest(ArtAiSettings.GetUrl());
            }
            listing.Label("AiArtServerUrlHint".Translate());

            ServerCheck result = _testResult;
            if (_testRunning)
            {
                listing.Label("AiArtTestRunning".Translate());
            }
            else if (result != null && _testedUrl == ArtAiSettings.GetUrl())
            {
                listing.Label(result.Text());
            }
        }

        private static void StartTest(string url)
        {
            _testedUrl = url;
            _testResult = null;
            _testRunning = true;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    _testResult = ServerCheck.Run(url);
                }
                finally
                {
                    _testRunning = false;
                }
            });
        }
    }
}
