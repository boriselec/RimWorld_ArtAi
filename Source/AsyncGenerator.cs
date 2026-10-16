using System;
using System.Collections.Generic;
using System.Threading;
using ArtAi.data;
using ArtAi.Server;
using ArtAi.util;

namespace ArtAi
{
    // Runs the whole generation lifecycle (enqueue -> poll -> download -> save to disk)
    // on a single background daemon thread, so the UI/draw thread never blocks on HTTP.
    //
    // Thread-safety rules:
    //  - the worker calls only network + plain file IO (Generator.*, ImageRepo.SaveImage);
    //    no Unity API and no Verse.Log here (errors are captured for the main thread).
    //  - the worker thread is a background daemon started exactly once.
    public static class AsyncGenerator
    {
        public enum JobState { New, Enqueued, Done, Error }

        public class GenJob
        {
            public readonly Description Description;
            public readonly string Prompt;
            // null: the server picks the seed
            public readonly int? Seed;
            public volatile JobState State = JobState.New;
            public volatile string ErrorMessage;
            // queue position, or -1 when unknown
            public volatile int QueuePosition = -1;
            // why it takes long (e.g. model download progress), if the server says
            public volatile ServerStatus ServerStatus;
            public string RqUid;

            public GenJob(Description description, string prompt, int? seed)
            {
                Description = description;
                Prompt = prompt;
                Seed = seed;
            }
        }

        private const int PollIntervalMs = 5_000;

        private static readonly object Lock = new object();
        private static readonly Dictionary<Description, GenJob> Jobs
            = new Dictionary<Description, GenJob>();
        private static bool _started;

        // Main thread: the job for the description, or null
        public static GenJob Find(Description description)
        {
            lock (Lock)
            {
                return Jobs.TryGetValue(description, out var job) ? job : null;
            }
        }

        // Main thread: start a new job, replacing a finished one. Never blocks.
        public static void Start(Description description, string prompt, int? seed)
        {
            lock (Lock)
            {
                Jobs[description] = new GenJob(description, prompt, seed);
                EnsureWorker();
            }
        }

        // Main thread: drop a job once its Done result has been consumed.
        public static void Remove(Description description)
        {
            lock (Lock)
            {
                Jobs.Remove(description);
            }
        }

        // call under Lock
        private static void EnsureWorker()
        {
            if (_started) return;
            _started = true;
            var thread = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = "ArtAi.AsyncGenerator"
            };
            thread.Start();
        }

        private static void WorkerLoop()
        {
            while (true)
            {
                try
                {
                    List<GenJob> snapshot;
                    lock (Lock)
                    {
                        snapshot = new List<GenJob>(Jobs.Values);
                    }

                    foreach (var job in snapshot)
                    {
                        Advance(job);
                    }
                }
                catch (Exception e)
                {
                    BgLog.Message("AsyncGenerator loop error: " + e);
                }

                Thread.Sleep(PollIntervalMs);
            }
        }

        private static void Advance(GenJob job)
        {
            try
            {
                switch (job.State)
                {
                    case JobState.New:
                        job.RqUid = Generator.Enqueue(
                            job.Prompt, job.Description.Language, job.Seed);
                        job.State = JobState.Enqueued;
                        break;
                    case JobState.Enqueued:
                        var (filename, queuePosition, serverStatus) = Generator.PollHistory(job.RqUid);
                        if (filename != null)
                        {
                            var bytes = Generator.DownloadImage(filename);
                            ImageRepo.SaveImage(bytes, job.Description, job.Prompt);
                            job.State = JobState.Done;
                        }
                        else
                        {
                            job.QueuePosition = queuePosition ?? -1;
                            job.ServerStatus = serverStatus;
                        }
                        break;
                    default:
                        return;
                }
                ServerAvailability.Report(true);
            }
            catch (Exception e)
            {
                BgLog.Message("Generation failed: " + e);
                // an HTTP error still means the server answered
                ServerAvailability.Report(!(e is ServerUnavailableException));
                job.ErrorMessage = e.Message;
                job.State = JobState.Error;
            }
        }
    }
}
