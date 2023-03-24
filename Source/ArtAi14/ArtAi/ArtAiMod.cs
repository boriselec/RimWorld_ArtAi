using HarmonyLib;
using UnityEngine;
using Verse;

namespace ArtAi
{
    public class ArtAiSettings : ModSettings
    {
        public static string ServerUrl = "https://boriselec.com/rimworld-art/generate";

        public override void ExposeData()
        {
            Scribe_Values.Look(ref ServerUrl, "serverUrl", "https://boriselec.com/rimworld-art/generate");
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
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.Label("Generation server url");
            ArtAiSettings.ServerUrl = listingStandard.TextEntry(ArtAiSettings.ServerUrl);
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "ArtAi";
        }
    }
}