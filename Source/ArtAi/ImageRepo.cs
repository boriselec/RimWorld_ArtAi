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
                    return LoadTexture(virtualFile);
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

        // copy of Verse.ModContentLoader[T].LoadTexture
        // but make it readable
        private static Texture2D LoadTexture(VirtualFile file)
        {
            if (!file.Exists)
                return null;
            byte[] data = file.ReadAllBytes();
            Texture2D texture2D1 = new Texture2D(2, 2, TextureFormat.Alpha8, true);
            texture2D1.LoadImage(data);
            if ((texture2D1.width < 4 || texture2D1.height < 4 || !Mathf.IsPowerOfTwo(texture2D1.width)
                    ? 0
                    : (Mathf.IsPowerOfTwo(texture2D1.height) ? 1 : 0)) == 0 && Prefs.TextureCompression)
            {
                int mipmapsForDxtSupport = StaticTextureAtlas.CalculateMaxMipmapsForDxtSupport(texture2D1);
                if (Prefs.LogVerbose)
                    Log.Warning(
                        $"Texture {file.Name} is being reloaded with reduced mipmap count (clamped to {mipmapsForDxtSupport}) due to non-power-of-two dimensions: ({texture2D1.width}x{texture2D1.height}). This will be slower to load, and will look worse when zoomed out. Consider using a power-of-two texture size instead.");
                if (!UnityData.ComputeShadersSupported)
                {
                    Texture2D texture2D2 = new Texture2D(texture2D1.width, texture2D1.height, TextureFormat.Alpha8,
                        mipmapsForDxtSupport, false);
                    UnityEngine.Object.DestroyImmediate(texture2D1);
                    texture2D1 = texture2D2;
                    texture2D1.LoadImage(data);
                }
            }

            if (Prefs.TextureCompression & (texture2D1.width % 4 == 0 && texture2D1.height % 4 == 0))
            {
                if (!UnityData.ComputeShadersSupported)
                {
                    texture2D1.Compress(true);
                    texture2D1.filterMode = FilterMode.Trilinear;
                    texture2D1.anisoLevel = 2;
                    texture2D1.Apply(true, false);
                }
                else
                {
                    texture2D1.filterMode = FilterMode.Trilinear;
                    texture2D1.anisoLevel = 2;
                    texture2D1.Apply(true, false);
                    texture2D1 = StaticTextureAtlas.FastCompressDXT(texture2D1, true);
                }
            }
            else
            {
                texture2D1.filterMode = FilterMode.Trilinear;
                texture2D1.anisoLevel = 2;
                texture2D1.Apply(true, false);
            }

            texture2D1.name = Path.GetFileNameWithoutExtension(file.Name);
            return texture2D1;
        }
    }
}