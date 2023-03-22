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
        private static readonly string RepoPath = Application.dataPath + "/AiArt/";

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

                var filePath = RepoPath + GetImageFileName(description);
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
            int trimTo = 250 - RepoPath.Length;
            string sanitizedFileName = Path.GetInvalidFileNameChars()
                .Aggregate(description.ArtDescription + description.ThingDescription,
                    (f, c) => f.Replace(c, '_'))
                .Replace(" ", "_");
            return sanitizedFileName.Substring(0, Math.Min(sanitizedFileName.Length, trimTo)) + ".png";
        }
    }
}