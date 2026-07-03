using System;
using System.Collections.Generic;
using System.Threading;
using ArtAi.data;
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
            public volatile JobState State = JobState.New;
            public volatile string ErrorMessage;
            // queue position, or -1 when unknown
            public volatile int QueuePosition = -1;
            public string RqUid;

            public GenJob(Description description)
            {
                Description = description;
            }
        }

        private const int PollIntervalMs = 5_000;

        private static readonly object Lock = new object();
        private static readonly Dictionary<Description, GenJob> Jobs
            = new Dictionary<Description, GenJob>();
        private static bool _started;

        // Main thread: return the existing job or start a new one. Never blocks.
        public static GenJob GetOrStart(Description description)
        {
            lock (Lock)
            {
                if (!Jobs.TryGetValue(description, out var job))
                {
                    job = new GenJob(description);
                    Jobs[description] = job;
                }
                EnsureWorker();
                return job;
            }
        }

        // Main thread: drop a job once its Done/Error result has been consumed.
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
                        job.RqUid = Generator.Enqueue(job.Description);
                        job.State = JobState.Enqueued;
                        break;
                    case JobState.Enqueued:
                        var (filename, queuePosition) = Generator.PollHistory(job.RqUid);
                        if (filename != null)
                        {
                            var bytes = Generator.DownloadImage(filename);
                            ImageRepo.SaveImage(bytes, job.Description);
                            job.State = JobState.Done;
                        }
                        else
                        {
                            job.QueuePosition = queuePosition ?? -1;
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                job.ErrorMessage = e.ToString();
                job.State = JobState.Error;
            }
        }
    }
}
