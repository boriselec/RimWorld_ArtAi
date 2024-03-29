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
        
        public static GeneratedImage InProgress(String description)
        {
            return new GeneratedImage(GenerationStatus.InProgress, null, description);
        }
        
        public static GeneratedImage Done(Texture2D texture, String description)
        {
            return new GeneratedImage(GenerationStatus.Done, texture, description);
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

    public enum GenerationStatus
    {
        InProgress,
        Done,
        NeedGenerate
    }
}