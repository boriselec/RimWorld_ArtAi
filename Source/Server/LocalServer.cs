using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using ArtAi.util;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArtAi.Server
{
    // The local generation server: Server/artai.py of the mod, copied to a folder
    // next to the game's save data and run there with uv (see Server/README.md).
    public static class LocalServer
    {
        private static readonly string[] ServerFiles = { "artai.py", "run.bat", "run.sh", "README.md" };

        public static string Folder => Path.Combine(GenFilePaths.SaveDataFolderPath, "AiArtServer");

        public static string ConfigPath => Path.GetFullPath(Path.Combine(Folder, "artai_config.json"));

        private static LocalServerConfig _config;

        public static LocalServerConfig Config => _config ?? (_config = LocalServerConfig.Load(ConfigPath));

        public static string Url => "http://127.0.0.1:" + Config.port;

        // base URL of the OpenAI-compatible API of the text model, null when it is off
        // "localhost": some programs (e.g. EchoColony) accept it but not an IP address
        public static string OpenAiUrl => Config.openai_port > 0 ? "http://localhost:" + Config.openai_port + "/v1" : null;

        // the mod sends requests to the local server
        public static bool IsSelected => ArtAiSettings.GetUrl() == Url;

        // Last answer to /info, null until the first one; updated by RequestStatus()
        public static volatile ServerCheck LastCheck;
        private static int _statusRequestRunning;

        public static bool Responding => LastCheck?.Kind == ServerCheck.Result.LocalServer;

        // the server process started from the game, if any
        private static Process _process;

        // started from the game, but not answering yet (e.g. uv is installing libraries)
        public static bool IsStarting => !Responding && IsOwnProcessRunning();

        public static void ReloadConfig()
        {
            _config = LocalServerConfig.Load(ConfigPath);
        }

        public static void SaveConfig(LocalServerConfig config)
        {
            bool wasSelected = IsSelected;
            Directory.CreateDirectory(Folder);
            config.Save(ConfigPath);
            _config = config;
            if (wasSelected)
            {
                // the port may have changed
                ArtAiSettings.SetServerUrl(Url);
            }
        }

        // Copy the server files from the mod. A file is rewritten only when it differs,
        // so run.bat is never changed while cmd.exe is executing it.
        public static void Install()
        {
            string source = Path.Combine(ArtAiMod.RootDir, "Server");
            Directory.CreateDirectory(Folder);
            foreach (var name in ServerFiles)
            {
                string from = Path.Combine(source, name);
                string to = Path.Combine(Folder, name);
                if (!File.Exists(to) || !File.ReadAllBytes(from).SequenceEqual(File.ReadAllBytes(to)))
                {
                    File.Copy(from, to, true);
                }
            }
        }

        public static void Start()
        {
            Install();
            // the server exits by itself when the game process exits
            string args = " --parent-pid " + Process.GetCurrentProcess().Id;
            ProcessStartInfo startInfo = Application.platform == RuntimePlatform.WindowsPlayer
                // the console window shows the progress and stays open if the server fails
                ? new ProcessStartInfo("cmd.exe", "/c run.bat" + args) { UseShellExecute = true }
                : new ProcessStartInfo("/bin/sh", "run.sh" + args) { UseShellExecute = false };
            startInfo.WorkingDirectory = Folder;
            _process = Process.Start(startInfo);
            ServerAvailability.NotifyStarted();
        }

        // Start from a button or menu item: a failure is shown to the player
        public static void TryStart()
        {
            try
            {
                Start();
            }
            catch (Exception e)
            {
                Log.Warning("AiArt: cannot start the local server: " + e);
                Messages.Message("AiArtLocalStartFailed".Translate(e.Message), MessageTypeDefOf.RejectInput, false);
            }
        }

        // Ask the server to exit, and kill the process tree started from the game:
        // that also covers the time before the server answers (uv installing libraries).
        public static void Stop()
        {
            string url = Url;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    HttpUtil.DoPost(url + "/shutdown", "{}");
                }
                catch (Exception e)
                {
                    BgLog.Message("Local server shutdown request: " + e.Message);
                }
            });

            if (IsOwnProcessRunning())
            {
                if (Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    // cmd.exe -> uv -> python
                    Process.Start(new ProcessStartInfo("taskkill", "/T /F /PID " + _process.Id)
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                }
                else
                {
                    _process.Kill();
                }
            }
            _process = null;
        }

        // Ask /info in the background; the answer appears in LastCheck
        public static void RequestStatus()
        {
            if (Interlocked.Exchange(ref _statusRequestRunning, 1) == 1) return;

            string url = Url;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    LastCheck = ServerCheck.Run(url);
                }
                finally
                {
                    Interlocked.Exchange(ref _statusRequestRunning, 0);
                }
            });
        }

        public static bool IsOwnProcessRunning()
        {
            try
            {
                return _process != null && !_process.HasExited;
            }
            catch (Exception)
            {
                // the process object is not attached to a running process
                return false;
            }
        }
    }
}
