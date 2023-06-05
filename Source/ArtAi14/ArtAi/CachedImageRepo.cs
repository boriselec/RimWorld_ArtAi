using System.Collections.Generic;
using ArtAi.data;
using UnityEngine;

namespace ArtAi
{
    public abstract class CachedImageRepo
    {
        // cache of done images by exact description
        private static readonly Dictionary<Description, GeneratedImage> GetExactImageCache
            = new Dictionary<Description, GeneratedImage>();

        // cache of done images by thing (description may be outdated)
        private static readonly Dictionary<string, GeneratedImage> GetLastGeneratedImageCache
            = new Dictionary<string, GeneratedImage>();

        public static GeneratedImage GetExactImage(Description description)
        {
            if (GetExactImageCache.ContainsKey(description))
            {
                return GetExactImageCache[description];
            }

            Texture2D exactImage = ImageRepo.GetExactImage(description);
            if (exactImage != null)
            {
                GeneratedImage result = GeneratedImage.Done(exactImage, description.ArtDescription);
                GetExactImageCache[description] = result;
                return result;
            }

            return null;
        }

        public static GeneratedImage GetLastGeneratedImage(Description description)
        {
            string thingId = description.ThingId;
            if (GetLastGeneratedImageCache.ContainsKey(thingId))
            {
                return GetLastGeneratedImageCache[thingId];
            }

            Texture2D lastGeneratedImage = ImageRepo.GetLastGeneratedImage(thingId);
            if (lastGeneratedImage != null)
            {
                GeneratedImage done = GeneratedImage.Done(lastGeneratedImage, description.ArtDescription);
                GetLastGeneratedImageCache[thingId] = done;
                return done;
            }

            return null;
        }

        public static void ClearCache(Description description)
        {
            GetExactImageCache.Remove(description);
            GetLastGeneratedImageCache.Remove(description.ThingId);
        }
    }
}