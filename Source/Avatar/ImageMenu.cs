using System;
using System.Collections.Generic;
using ArtAi.data;
using ArtAi.Server;
using ArtAi.util;
using Verse;

namespace ArtAi.Avatar
{
    // Menu opened by a click on an image
    public static class ImageMenu
    {
        private static readonly Random Random = new Random();

        // Unavailable items are hidden, not disabled: FloatMenu moves disabled items to the bottom
        public static List<FloatMenuOption> Options(Description description)
        {
            var options = new List<FloatMenuOption>();
            GeneratedImage image = ImageService.Get(description);

            // the main action first
            if (image.Texture != null)
            {
                options.Add(new FloatMenuOption("AiArtMenuView".Translate(),
                    () => ImageWindows.OpenImage(description, image.Texture)));
            }
            if (image.Status == GenerationStatus.NeedGenerate)
            {
                options.Add(new FloatMenuOption("AiArtMenuGenerate".Translate(),
                    () => ImageService.Generate(description)));
            }
            if (image.Status == GenerationStatus.Error)
            {
                options.Add(new FloatMenuOption("AiArtMenuRetry".Translate(),
                    () => ImageService.Retry(description)));
            }

            options.Add(new FloatMenuOption("AiArtMenuPrompt".Translate(),
                () => ImageWindows.OpenPrompt(description)));

            string imagePath = ImageRepo.GetImagePath(description);
            if (imagePath != null)
            {
                options.Add(new FloatMenuOption("AiArtMenuOpenImage".Translate(),
                    () => ShellUtil.RevealFile(imagePath)));
            }

            options.Add(new FloatMenuOption("AiArtMenuServer".Translate(),
                () => Find.WindowStack.Add(new Dialog_ServerSettings())));

            if (CanStartLocalServer())
            {
                options.Add(new FloatMenuOption("AiArtMenuStartServer".Translate(), LocalServer.TryStart));
            }

            // no action: shown disabled, as information, always the last item
            options.Add(new FloatMenuOption(ImageService.StatusText(description), null));

            return options;
        }

        // the mod uses a server on this computer, and it is known not to answer
        private static bool CanStartLocalServer()
        {
            string url = ArtAiSettings.GetUrl().ToLowerInvariant();
            return (url.Contains("localhost") || url.Contains("127.0.0.1"))
                   && ServerAvailability.IsUnavailable
                   && !LocalServer.IsOwnProcessRunning();
        }

        public static int NewSeed()
        {
            return Random.Next();
        }
    }
}
