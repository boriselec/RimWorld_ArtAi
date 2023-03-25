using ArtAi.data;
using RimWorld;
using Verse;

namespace ArtAi
{
    public static class DescriptionCompArt
    {
        public static Description GetDescription(CompArt compArt)
        {
            var ArtDescription = compArt.GenerateImageDescription();
            var ThingDescription = compArt.parent.def.description;
            return new Description(ArtDescription, ThingDescription, LanguageDatabase.activeLanguage.folderName);
        }
    }
}
