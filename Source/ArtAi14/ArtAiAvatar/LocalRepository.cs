using System.Collections.Generic;
using System.IO;
using ArtAi.data;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArtAi
{
    public class LocalRepository
    {
        private readonly Dictionary<Description, GeneratedImage> _images
            = new Dictionary<Description, GeneratedImage>();

        public static LocalRepository Instance = new LocalRepository();

        private string CacheFolder = Path.Combine(Path.GetDirectoryName(GenFilePaths.ConfigFolderPath), "CacheAvatar");

        public GeneratedImage GetImageBase(Description description)
        {
            GeneratedImage image;
            if (!_images.ContainsKey(description) || (image = _images[description]).NeedUpdate())
            {
                image = Generator.Generate(description);
                _images[description] = image;
            }
            return image;
        }

        public GeneratedImage GetImage(Description description)
        {
            GeneratedImage image;
            if (!_images.ContainsKey(description) || (image = _images[description]).NeedUpdate())
            {
                var hash = (uint)description.GetHashCode();
                var fileName = Path.Combine(CacheFolder, hash.ToString() + ".png");
                if (!Directory.Exists(CacheFolder)) Directory.CreateDirectory(CacheFolder);
                if (File.Exists(fileName))
                {
                    image = LoadImage(fileName);
                }
                else
                {
                    image = Generator.Generate(description);
                    SaveFile(fileName, image);
                }
                _images[description] = image;
            }
            return image;
        }

        private GeneratedImage LoadImage(string fileName)
        {
            var data = File.ReadAllBytes(fileName);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(data);
            texture.Apply();

            return GeneratedImage.Done(texture);
        }

        private void SaveFile(string fileName, GeneratedImage image)
        {
            if (image.Status != GenerationStatus.Done || image.Texture == null) return;

            var encodedImage = image.Texture.EncodeToPNG();

            File.WriteAllBytes(fileName, encodedImage);
        }
    }
}
