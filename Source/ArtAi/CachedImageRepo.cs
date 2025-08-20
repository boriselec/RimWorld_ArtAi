using System.Collections.Generic;
using ArtAi.data;
using UnityEngine;

namespace ArtAi
{
    public abstract class CachedImageRepo
    {
        // cache of done images by exact description
        private static readonly Dictionary<Description, Texture2D> GetExactImageCache
            = new Dictionary<Description, Texture2D>();

        // cache of done images by thing (description may be outdated)
        private static readonly Dictionary<string, Texture2D> GetLastGeneratedImageCache
            = new Dictionary<string, Texture2D>();

        public static GeneratedImage GetExactImage(Description description)
        {
            Texture2D exactImage;
            if (GetExactImageCache.TryGetValue(description, out var value))
            {
                exactImage = value;
            }
            else
            {
                exactImage = ImageRepo.GetExactImage(description);
                if (exactImage != null)
                {
                    GetExactImageCache[description] = exactImage;
                }
            }

            return exactImage == null
                ? null
                : GeneratedImage.Done(exactImage, description.ArtDescription);
        }

        public static GeneratedImage GetLastGeneratedImage(Description description)
        {
            string thingId = description.ThingId;
            Texture2D lastGeneratedImage;
            if (GetLastGeneratedImageCache.TryGetValue(thingId, out var value))
            {
                lastGeneratedImage = value;
            }
            else
            {
                lastGeneratedImage = ImageRepo.GetLastGeneratedImage(thingId);
                if (lastGeneratedImage != null)
                {
                    GetLastGeneratedImageCache[thingId] = lastGeneratedImage;
                }
            }

            return lastGeneratedImage == null
                ? null
                : GeneratedImage.Outdated(lastGeneratedImage, description.ArtDescription);
        }

        public static void ClearCache(Description description)
        {
            GetLastGeneratedImageCache.Remove(description.ThingId);
        }
    }
}