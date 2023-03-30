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

        public static Texture2D GetImage(Description description)
        {
            try
            {
                VirtualDirectory virtualDirectory = AbstractFilesystem.GetDirectory(RepoPath);
                VirtualFile virtualFile = GetLastModifiedFile(virtualDirectory, description);
                
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

        private static VirtualFile GetLastModifiedFile(VirtualDirectory imageDir, Description description)
        {
            var thingDir = imageDir.GetDirectory(description.ThingId);
            return (thingDir.Exists
                    ? thingDir.GetFiles("*png", SearchOption.TopDirectoryOnly)
                    : Enumerable.Empty<VirtualFile>())
                // legacy path
                .Concat(Enumerable.Repeat(imageDir.GetFile(GetImageFileName(description, RepoPath)), 1))
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