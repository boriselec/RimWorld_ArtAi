using HarmonyLib;
using UnityEngine;
using Verse;

namespace ArtAi
{
    public class ArtAiSettings : ModSettings
    {
        public static string ServerUrl = "https://boriselec.com/rimworld-art/generate";
        public static bool ShowGizmo = true;
        public static bool DoubleAvatarSize = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref ServerUrl, "serverUrl", "https://boriselec.com/rimworld-art/generate");
            Scribe_Values.Look(ref ShowGizmo, "showGizmo", true);
            Scribe_Values.Look(ref DoubleAvatarSize, "doubleAvatarSize", true);
            base.ExposeData();
        }
    }

    public class ArtAiMod : Mod
    {
        static ArtAiMod()
        {
            var harmony = new Harmony("artaiavatar.patch");
            harmony.PatchAll();
        }

        public ArtAiMod(ModContentPack content) : base(content)
        {
            GetSettings<ArtAiSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.Label("Generation server url");
            ArtAiSettings.ServerUrl = listingStandard.TextEntry(ArtAiSettings.ServerUrl);
            listingStandard.Gap();
            listingStandard.CheckboxLabeled("Show gizmo", ref ArtAiSettings.ShowGizmo);
            listingStandard.Gap();
            listingStandard.CheckboxLabeled("Double avatar size", ref ArtAiSettings.DoubleAvatarSize);
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "ArtAi";
        }
    }
}