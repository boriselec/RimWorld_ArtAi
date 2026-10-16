using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace ArtAi.util
{
    public static class ShellUtil
    {
        // same as the game's "open folder" buttons
        public static void OpenFolder(string path)
        {
            Application.OpenURL(Path.GetFullPath(path));
        }

        // open the file manager with the file selected
        public static void RevealFile(string path)
        {
            path = Path.GetFullPath(path);
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                    Process.Start("explorer.exe", "/select,\"" + path + "\"");
                    break;
                case RuntimePlatform.OSXPlayer:
                    Process.Start("open", "-R \"" + path + "\"");
                    break;
                default:
                    OpenFolder(Path.GetDirectoryName(path));
                    break;
            }
        }
    }
}
