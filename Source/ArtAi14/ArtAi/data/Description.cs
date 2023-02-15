using RimWorld;

namespace ArtAi.data
{
    public struct Description
    {
        public string ArtDescription { get; }
        public string ThingDescription { get; }

        public Description(CompArt compArt)
        {
            ArtDescription = compArt.GenerateImageDescription();
            ThingDescription = compArt.parent.def.description;
        }
    }
}