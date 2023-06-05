using System;
using System.IO;
using System.Linq;
using ArtAi.data;
using RimWorld.IO;
using UnityEngine;
using Verse;

namespace ArtAi
{
    public abstract class ImageRepo
    {
        private static readonly string RepoPath = Path.Combine(GenFilePaths.SaveDataFolderPath, "AiArt");

        // Get image for thing that matches description exactly
        public static Texture2D GetExactImage(Description description)
        {
            return GetImage(thingDir => GetExactFile(thingDir, description), description.ThingId);
        }

        // Get last generated image for thing 
        public static Texture2D GetLastGeneratedImage(string thingId)
        {
            return GetImage(GetLastModifiedFile, thingId);
        }

        private static Texture2D GetImage(Func<VirtualDirectory, VirtualFile> fileSupplier, string thingId)
        {
            try
            {
                var thingDir = AbstractFilesystem.GetDirectory(RepoPath)
                    .GetDirectory(thingId);
                if (!thingDir.Exists)
                {
                    return null;
                }
                VirtualFile virtualFile = fileSupplier.Invoke(thingDir);
                
                if (virtualFile != null)
                {
                    return ModContentLoader<Texture2D>.LoadItem(virtualFile).contentItem;
                }
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }
            return null;
        }

        private static VirtualFile GetExactFile(VirtualDirectory thingDir, Description description)
        {
            return thingDir.GetFile(GetImageFileName(description, thingDir.FullPath));
        }

        private static VirtualFile GetLastModifiedFile(VirtualDirectory thingDir)
        {
            return thingDir.GetFiles("*png", SearchOption.TopDirectoryOnly)
                .Where(f => f.Exists)
                .OrderByDescending(f => new FileInfo(f.FullPath).LastWriteTime)
                .FirstOrFallback();
        }

        public static void SaveImage(byte[] data, Description description)
        {
            try
            {
                var dirPath = Path.Combine(RepoPath, description.ThingId);
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                var imageFileName = GetImageFileName(description, dirPath);
                var filePath = Path.Combine(dirPath, imageFileName);
                if (!File.Exists(filePath))
                {
                    File.WriteAllBytes(filePath, data);
                }
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }
        }

        private static string GetImageFileName(Description description, string dirPath)
        {
            var hash = ((uint)description.GetHash()).ToString();
            int trimTo = 250 - dirPath.Length - hash.Length;
            string sanitizedFileName = trimTo > 0 
                ? Path.GetInvalidFileNameChars()
                .Aggregate(description.ArtDescription + description.ThingDescription,
                    (f, c) => f.Replace(c, '_'))
                .Replace(" ", "_")
                : String.Empty;

            if (sanitizedFileName.Length > trimTo)
                return sanitizedFileName.Substring(0, trimTo) + hash + ".png";
            else
                return sanitizedFileName + ".png";
        }
    }
}