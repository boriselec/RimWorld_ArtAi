using System;
using System.Collections.Generic;
using ArtAi.data;
using JetBrains.Annotations;
using UnityEngine;
using Verse;
using GenJob = ArtAi.AsyncGenerator.GenJob;

namespace ArtAi
{
    public abstract class ImageService
    {
        // user requested refresh of image by thing
        private static readonly HashSet<string> ForcedRefresh = new HashSet<string>();

        // store in progress generation requests to reduce load
        private static readonly Dictionary<Description, CachedImage> InProgress
            = new Dictionary<Description, CachedImage>();

        // Get existing/inprogress image from disk/cache
        public static GeneratedImage Get(Description description)
        {
            string thingId = description.ThingId;
            GeneratedImage generatedImage = CachedImageRepo.GetExactImage(description);

            bool forcedRefresh = ForcedRefresh.Contains(thingId);
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
            switch (generatedImage.Status)
            {
                case GenerationStatus.Done:
                case GenerationStatus.Outdated:
                    return generatedImage;
                case GenerationStatus.InProgress:
                case GenerationStatus.NeedGenerate:
                    return GetInProgress(description)
                           ?? GenerateAndRefreshCaches(description);
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
            GenJob job = AsyncGenerator.GetOrStart(description);
            switch (job.State)
            {
                case AsyncGenerator.JobState.Done:
                    // bytes already saved to disk by the worker
                    AsyncGenerator.Remove(description);
                    ClearCache(description);
                    // existing path: loads file and builds texture on the main thread
                    return Get(description);
                case AsyncGenerator.JobState.Error:
                    AsyncGenerator.Remove(description);
                    if (job.ErrorMessage != null)
                    {
                        Log.Error(job.ErrorMessage);
                    }
                    return GeneratedImage.Error();
                default:
                    // New / Enqueued -> still in progress
                    var lastImage = CachedImageRepo.GetLastGeneratedImage(description);
                    var inProgressImage = GeneratedImage.InProgress(
                        lastImage?.Texture,
                        QueuedMessage(job.QueuePosition >= 0 ? job.QueuePosition : (int?)null));
                    InProgress[description] = new CachedImage(inProgressImage);
                    return inProgressImage;
            }
        }

        private static string QueuedMessage(int? queuePosition)
        {
            string result = "AiArtInProgress".Translate();
            if (queuePosition != null)
            {
                result = result
                    + Environment.NewLine
                    + Environment.NewLine
                    + "AiArtQueuePosition".Translate()
                    + queuePosition;
            }
            return result;
        }

        private static void ClearCache(Description description)
        {
            ForcedRefresh.Remove(description.ThingId);
            InProgress.Remove(description);
            CachedImageRepo.ClearCache(description);
        }

        public static GeneratedImage ForceRefresh(Description description)
        {
            ForcedRefresh.Add(description.ThingId);
            return GetOrGenerate(description);
        }
    }
}
