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
            var language = LanguageDatabase.activeLanguage.folderName;
            return new Description(ArtDescription, ThingDescription, language, compArt.parent.ThingID);
        }
    }
}
