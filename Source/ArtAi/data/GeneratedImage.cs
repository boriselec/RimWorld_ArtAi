using System;
using UnityEngine;
using Verse;

namespace ArtAi.data
{
    public class GeneratedImage
    {
        public readonly GenerationStatus Status;
        public readonly Texture2D Texture;
        public readonly string Description;

        private GeneratedImage(GenerationStatus status, Texture2D texture, string description)
        {
            Texture = texture;
            Status = status;
            Description = description;
        }

        public GeneratedImage WithTexture(Texture2D texture)
        {
            return new GeneratedImage(Status, texture, Description);
        }

        public static GeneratedImage InProgress(String description)
        {
            return new GeneratedImage(GenerationStatus.InProgress, null, description);
        }

        public static GeneratedImage Done(Texture2D texture, String description)
        {
            return new GeneratedImage(GenerationStatus.Done, texture, description);
        }

        public static GeneratedImage Outdated(Texture2D texture, string description)
        {
            return new GeneratedImage(GenerationStatus.Outdated, texture, description);
        }

        public static GeneratedImage Error()
        {
            return InProgress("AiArtError".Translate());
        }

        public static GeneratedImage NeedGenerate()
        {
            return new GeneratedImage(GenerationStatus.NeedGenerate, null, "AiArtGizmoTooltip".Translate());
        }
    }
}