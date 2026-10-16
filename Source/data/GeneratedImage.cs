using UnityEngine;
using Verse;

namespace ArtAi.data
{
    public class GeneratedImage
    {
        public readonly GenerationStatus Status;
        public readonly Texture2D Texture;
        public readonly string Description;

        private GeneratedImage(
            GenerationStatus status,
            Texture2D texture,
            string description)
        {
            Texture = texture;
            Status = status;
            Description = description;
        }

        public GeneratedImage WithTexture(Texture2D texture)
        {
            return new GeneratedImage(Status, texture, Description);
        }

        public static GeneratedImage InProgress(string description)
        {
            return InProgress(null, description);
        }

        public static GeneratedImage InProgress(Texture2D texture, string description)
        {
            return new GeneratedImage(
                GenerationStatus.InProgress,
                texture,
                description);
        }

        public static GeneratedImage Done(Texture2D texture, string description)
        {
            return new GeneratedImage(GenerationStatus.Done, texture, description);
        }

        public static GeneratedImage Outdated(Texture2D texture, string description)
        {
            return new GeneratedImage(GenerationStatus.Outdated, texture, description);
        }

        // texture: previous image of the thing, if any
        public static GeneratedImage Error(Texture2D texture, string message)
        {
            return new GeneratedImage(
                GenerationStatus.Error,
                texture,
                "AiArtError".Translate() + ": " + message
                + "\n\n" + "AiArtRetry".Translate());
        }

        public static GeneratedImage NeedGenerate()
        {
            return new GeneratedImage(
                GenerationStatus.NeedGenerate,
                null,
                "AiArtGizmoTooltip".Translate());
        }
    }
}
