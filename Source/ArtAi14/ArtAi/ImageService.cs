using System;
using System.Collections.Generic;
using ArtAi.data;
using JetBrains.Annotations;
using UnityEngine;

namespace ArtAi
{
    public abstract class ImageService
    {
        // user requested refresh of image by thing
        private static readonly Dictionary<string, bool> ForcedRefresh
            = new Dictionary<string, bool>();

        // store in progress generation requests to reduce load
        private static readonly Dictionary<Description, CachedImage> InProgress
            = new Dictionary<Description, CachedImage>();

        // Get existing/inprogress image from disk/cache
        public static GeneratedImage Get(Description description)
        {
            string thingId = description.ThingId;

            GeneratedImage result;
            // Player requested refresh of image. Get image from disk/cache only if description matches exactly.
            if (ForcedRefresh.ContainsKey(thingId) && ForcedRefresh[thingId])
            {
                result = CachedImageRepo.GetExactImage(description);
                if (result != null)
                {
                    ForcedRefresh.Remove(thingId);
                }
            }
            else
            {
                result = CachedImageRepo.GetLastGeneratedImage(description);
            }

            if (result == null && InProgress.ContainsKey(description))
            {
                result = InProgress[description].Image;
            }

            return result ?? GeneratedImage.NeedGenerate();
        }

        // Get existing image or generate if none
        public static GeneratedImage GetOrGenerate(Description description)
        {
            GeneratedImage cached = Get(description);
            if (cached.Status == GenerationStatus.Done)
            {
                return cached;
            }

            return GetInProgress(description) ?? GenerateAndRefreshCaches(description);
        }

        [CanBeNull]
        private static GeneratedImage GetInProgress(Description description)
        {
            if (InProgress.ContainsKey(description))
            {
                CachedImage inProgressImage = InProgress[description];
                if (DateTime.Now < inProgressImage.ValidUntil)
                {
                    return inProgressImage.Image;
                }
            }

            return null;
        }

        private static GeneratedImage GenerateAndRefreshCaches(Description description)
        {
            GeneratedImage generatedImage = Generator.Generate(description);
            switch (generatedImage.Status)
            {
                case GenerationStatus.Done:
                    ImageRepo.SaveImage(generatedImage.Texture.EncodeToPNG(), description);
                    ClearCache(description);
                    return generatedImage;
                case GenerationStatus.InProgress:
                    InProgress[description] = new CachedImage(generatedImage);
                    return generatedImage;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void ClearCache(Description description)
        {
            ForcedRefresh.Remove(description.ThingId);
            InProgress.Remove(description);
            CachedImageRepo.ClearCache(description);
        }

        public static GeneratedImage ForceRefresh(Description description)
        {
            ForcedRefresh[description.ThingId] = true;
            return GetOrGenerate(description);
        }
    }
}