using ArtAi.data;
using RimWorld;
using Verse;

namespace ArtAi
{
    public static class DescriptionCompArt
    {
        public static Description GetDescription(CompArt compArt)
        {
            var imageDescription = compArt.GenerateImageDescription();
            var description = compArt.parent.def.description;
            var folderName = LanguageDatabase.activeLanguage.folderName;
            return new Description(imageDescription, description, folderName, compArt.parent.ThingID);
        }
    }
}
