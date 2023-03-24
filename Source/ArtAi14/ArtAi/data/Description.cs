using RimWorld;

namespace ArtAi.data
{
    public struct Description
    {
        public string ArtDescription { get; }
        public string ThingDescription { get; }

        public bool IsNull => ArtDescription == null && ThingDescription == null;

        public Description(CompArt compArt)
        {
            ArtDescription = compArt.GenerateImageDescription();
            ThingDescription = compArt.parent.def.description;
        }

        public Description(string artDescription, string thingDescription)
        {
            ArtDescription = artDescription;
            ThingDescription = thingDescription;
        }

        public int GetHash()
        {
            return (ArtDescription + ";" + ThingDescription).GetDeterministicHashCode();
        }
    }
}