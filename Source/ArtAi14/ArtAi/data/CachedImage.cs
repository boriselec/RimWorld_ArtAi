using System;

namespace ArtAi.data
{
    public class CachedImage
    {
        private const int UpToDateSeconds = 3;

        public readonly GeneratedImage Image;
        public readonly DateTime ValidUntil;

        public CachedImage(GeneratedImage image)
        {
            Image = image;
            ValidUntil = DateTime.Now.AddSeconds(UpToDateSeconds);
        }
    }
}