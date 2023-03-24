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
                string expectedFileName = GetImageFileName(description);
                VirtualFile virtualFile = virtualDirectory.GetFile(expectedFileName);
                if (virtualFile.Exists)
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
        
        public static void SaveImage(byte[] data, Description description)
        {
            try
            {
                if (!Directory.Exists(RepoPath))
                {
                    Directory.CreateDirectory(RepoPath);
                }

                var filePath = Path.Combine(RepoPath, GetImageFileName(description));
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

        private static string GetImageFileName(Description description)
        {
            var hash = ((uint)description.GetHashCode()).ToString();
            int trimTo = 250 - RepoPath.Length - hash.Length;
            string sanitizedFileName = trimTo > 0 
                ? Path.GetInvalidFileNameChars()
                .Aggregate(description.ArtDescription + description.ThingDescription,
                    (f, c) => f.Replace(c, '_'))
                .Replace(" ", "_")
                : String.Empty;
            return sanitizedFileName.Substring(0, Math.Min(sanitizedFileName.Length, trimTo)) + hash + ".png";
        }
    }
}