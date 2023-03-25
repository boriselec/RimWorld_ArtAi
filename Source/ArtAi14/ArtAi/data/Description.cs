using RimWorld;

namespace ArtAi.data
{
    public struct Description
    {
        public string ArtDescription { get; }
        public string ThingDescription { get; }
        public string Language { get; }

        public bool IsNull => ArtDescription == null && ThingDescription == null;

        public Description(CompArt compArt, string language)
        {
            ArtDescription = compArt.GenerateImageDescription();
            ThingDescription = compArt.parent.def.description;
            Language = language;
        }

        public Description(string artDescription, string thingDescription, string language)
        {
            ArtDescription = artDescription;
            ThingDescription = thingDescription;
            Language = language;
        }

        public int GetHash()
        {
            return (ArtDescription + ";" + ThingDescription).GetDeterministicHashCode();
        }
    }
}