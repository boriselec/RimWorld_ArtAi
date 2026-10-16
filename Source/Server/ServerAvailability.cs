using System;
using System.Threading;
using Verse;

namespace ArtAi.Server
{
    // Whether the server the mod uses answers, for the status line of the image menu.
    // Cached: checked in the background at most every 5 minutes, every 5 seconds while
    // a local server started from the game is coming up. Generation requests update it too.
    public static class ServerAvailability
    {
        private enum State { Unknown, Available, Unavailable }

        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan StartingCheckInterval = TimeSpan.FromSeconds(5);

        private static volatile State _state = State.Unknown;
        // set by Start; cleared when the server answers
        private static volatile bool _starting;
        private static int _checkRunning;

        // main thread only
        private static DateTime _lastCheck = DateTime.MinValue;
        private static string _checkedUrl;

        public static bool IsUnavailable => _state == State.Unavailable;

        // main thread: start a background check when the cached result is too old
        public static void Update()
        {
            string url = ArtAiSettings.GetUrl();
            if (url != _checkedUrl)
            {
                // another server was chosen: the cached result is about the old one
                _checkedUrl = url;
                _state = State.Unknown;
                _lastCheck = DateTime.MinValue;
            }

            bool starting = _starting && LocalServer.IsOwnProcessRunning();
            TimeSpan interval = starting ? StartingCheckInterval : CheckInterval;
            if (DateTime.Now - _lastCheck < interval) return;
            if (Interlocked.Exchange(ref _checkRunning, 1) == 1) return;

            _lastCheck = DateTime.Now;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    bool available = ServerCheck.Run(url).Kind != ServerCheck.Result.NoAnswer;
                    // skip the result if another server was chosen meanwhile
                    if (url == _checkedUrl)
                    {
                        Report(available);
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref _checkRunning, 0);
                }
            });
        }

        // any thread: the result of a request to the server
        public static void Report(bool available)
        {
            _state = available ? State.Available : State.Unavailable;
            if (available)
            {
                _starting = false;
            }
        }

        // main thread: a local server was started, check often until it answers
        public static void NotifyStarted()
        {
            _starting = true;
            _lastCheck = DateTime.MinValue;
        }

        // main thread
        public static string Text()
        {
            switch (_state)
            {
                case State.Available:
                    return "AiArtStatusAvailable".Translate();
                case State.Unavailable:
                    return "AiArtStatusUnavailable".Translate();
                default:
                    return "AiArtStatusChecking".Translate();
            }
        }
    }
}
