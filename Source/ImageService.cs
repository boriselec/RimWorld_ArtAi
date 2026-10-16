using System;
using ArtAi.data;
using ArtAi.Server;
using UnityEngine;
using Verse;
using GenJob = ArtAi.AsyncGenerator.GenJob;
using JobState = ArtAi.AsyncGenerator.JobState;

namespace ArtAi
{
    public static class ImageService
    {
        // Image to show now: generating > exact > outdated > none. Cheap enough to call every frame.
        public static GeneratedImage Get(Description description)
        {
            GenJob job = AsyncGenerator.Find(description);
            if (job != null)
            {
                switch (job.State)
                {
                    case JobState.Done:
                        // bytes already saved to disk by the worker, read them below
                        AsyncGenerator.Remove(description);
                        CachedImageRepo.ClearCache(description);
                        break;
                    case JobState.Error:
                        return GeneratedImage.Error(CurrentTexture(description), job.ErrorMessage);
                    default:
                        return GeneratedImage.InProgress(CurrentTexture(description), InProgressMessage(job));
                }
            }

            return CachedImageRepo.GetExactImage(description)
                   ?? CachedImageRepo.GetLastGeneratedImage(description)
                   ?? GeneratedImage.NeedGenerate();
        }

        // Like Get, but starts generation when the thing has no image yet
        public static GeneratedImage GetOrGenerate(Description description)
        {
            GeneratedImage image = Get(description);
            if (image.Status != GenerationStatus.NeedGenerate)
            {
                return image;
            }

            Generate(description);
            return Get(description);
        }

        public static bool IsGenerating(Description description)
        {
            GenJob job = AsyncGenerator.Find(description);
            return job != null && (job.State == JobState.New || job.State == JobState.Enqueued);
        }

        // Start generation of a new image. Does nothing while one is already generating.
        // prompt: null = the one the current image was made with (it may be edited by the player),
        // or, if there is none, built from the description; seed: null = the server picks it
        public static void Generate(Description description, string prompt = null, int? seed = null)
        {
            if (IsGenerating(description)) return;
            prompt = prompt
                     ?? ImageRepo.GetSavedPrompt(description)
                     ?? Generator.BuildPrompt(description);
            AsyncGenerator.Start(description, prompt, seed);
        }

        // Repeat the failed generation with the same prompt and seed
        public static void Retry(Description description)
        {
            GenJob job = AsyncGenerator.Find(description);
            if (job == null || job.State != JobState.Error) return;
            AsyncGenerator.Start(description, job.Prompt, job.Seed);
        }

        // One line for the image menu, e.g. "Status: 3 in queue, server is not available"
        public static string StatusText(Description description)
        {
            string status;
            GenJob job = AsyncGenerator.Find(description);
            if (job != null && job.State == JobState.Error)
            {
                status = "AiArtStatusError".Translate() + ": " + job.ErrorMessage;
            }
            else if (job != null && job.State != JobState.Done)
            {
                status = job.QueuePosition > 0
                    ? "AiArtStatusQueue".Translate(job.QueuePosition).ToString()
                    : "AiArtStatusGenerating".Translate().ToString();
                if (job.ServerStatus != null)
                {
                    status += ", " + job.ServerStatus.Text();
                }
            }
            else
            {
                switch (Get(description).Status)
                {
                    case GenerationStatus.Done:
                        status = "AiArtStatusDone".Translate();
                        break;
                    case GenerationStatus.Outdated:
                        status = "AiArtStatusOutdated".Translate();
                        break;
                    default:
                        status = "AiArtStatusNotGenerated".Translate();
                        break;
                }
            }

            status += ", " + ServerAvailability.Text();
            return "AiArtStatus".Translate(status);
        }

        // previous image of the thing, shown while the new one is generating
        private static Texture2D CurrentTexture(Description description)
        {
            return (CachedImageRepo.GetExactImage(description)
                    ?? CachedImageRepo.GetLastGeneratedImage(description))
                ?.Texture;
        }

        private static string InProgressMessage(GenJob job)
        {
            string result = "AiArtInProgress".Translate();
            if (job.QueuePosition >= 0)
            {
                result = result
                         + Environment.NewLine
                         + Environment.NewLine
                         + "AiArtQueuePosition".Translate()
                         + job.QueuePosition;
            }
            if (job.ServerStatus != null)
            {
                result = result
                         + Environment.NewLine
                         + Environment.NewLine
                         + job.ServerStatus.Text().CapitalizeFirst();
            }
            return result;
        }
    }
}
