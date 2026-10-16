using ArtAi.Server;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace ArtAi
{
    public class ArtAiSettings : ModSettings
    {
        public const string PublicServerUrl = "https://boriselec.com/rimworld-art";

        public static string ServerUrl = PublicServerUrl;

        public static bool ShowGizmo = true;

        public static bool DoubleAvatarSize = true;

        // The defaults must be constants: Scribe does not write a value equal to its default
        public override void ExposeData()
        {
            Scribe_Values.Look(ref ServerUrl, "serverUrl", PublicServerUrl);
            Scribe_Values.Look(ref ShowGizmo, "showGizmo", true);
            Scribe_Values.Look(ref DoubleAvatarSize, "doubleAvatarSize", true);
            base.ExposeData();
        }

        public static string GetUrl()
        {
            return ServerUrl.TrimEnd('/');
        }

        // change the server from outside the mod settings window, and save it
        public static void SetServerUrl(string url)
        {
            ServerUrl = url;
            ArtAiMod.Instance.WriteSettings();
        }
    }

    public class ArtAiMod : Mod
    {
        public static ArtAiMod Instance { get; private set; }

        // folder of the mod, contains Server/ with the local server files
        public static string RootDir => Instance.Content.RootDir;

        static ArtAiMod()
        {
            var harmony = new Harmony("artaiavatar.patch");
            harmony.PatchAll();
        }

        public ArtAiMod(ModContentPack content) : base(content)
        {
            Instance = this;
            GetSettings<ArtAiSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            try
            {
                listingStandard.CheckboxLabeled("AiArtSettingsShowGizmo".Translate(), ref ArtAiSettings.ShowGizmo);
                listingStandard.Gap();
                listingStandard.CheckboxLabeled("AiArtSettingsDoubleSize".Translate(), ref ArtAiSettings.DoubleAvatarSize);
                listingStandard.Gap();
                listingStandard.GapLine();
                listingStandard.Label("AiArtServerUse".Translate());
                ServerChoice.Draw(listingStandard);
                listingStandard.Gap();
                if (listingStandard.ButtonText("AiArtMenuServer".Translate()))
                {
                    Find.WindowStack.Add(new Dialog_ServerSettings());
                }
            }
            finally
            {
                // keep the GUI group balanced if drawing fails
                listingStandard.End();
            }
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "ArtAi";
        }
    }
}
