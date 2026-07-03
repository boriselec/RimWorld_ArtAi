using System;
using UnityEngine;

namespace ArtAi.util
{
    // Logging from background worker threads. Verse.Log is not thread-safe, so
    // background code must use UnityEngine.Debug.*. Unity's Player.log has no
    // timestamps, so we prepend one ourselves.
    public static class BgLog
    {
        public static void Message(string text)
        {
            Debug.Log("AiArt. " + DateTime.Now.ToString("HH:mm:ss.fff") + " " + text);
        }
    }
}
