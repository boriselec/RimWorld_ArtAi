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
            GeneratedImage generatedImage = CachedImageRepo.GetExactImage(description);

            bool forcedRefresh = ForcedRefresh.ContainsKey(thingId) && ForcedRefresh[thingId];
            if (forcedRefresh && generatedImage != null)
            {
                ForcedRefresh.Remove(thingId);
            }

            if (generatedImage == null && !forcedRefresh)
            {
                generatedImage = CachedImageRepo.GetLastGeneratedImage(description);
            }

            if (generatedImage == null && InProgress.ContainsKey(description))
            {
                generatedImage = InProgress[description].Image;
            }
            return generatedImage ?? GeneratedImage.NeedGenerate();
        }

        // Get existing image or generate if none
        public static GeneratedImage GetOrGenerate(Description description)
        {
            GeneratedImage generatedImage = Get(description);
            return generatedImage.Status.HasImage()
                ? generatedImage
                : GetInProgress(description) ?? GenerateAndRefreshCaches(description);
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
                case GenerationStatus.Outdated:
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