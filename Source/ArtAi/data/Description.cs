namespace ArtAi.data
{
    public struct Description
    {
        public string ArtDescription { get; }
        public string ThingDescription { get; }
        public string Language { get; }
        public string ThingId { get; }

        public bool IsNull => ArtDescription == null && ThingDescription == null;

        public Description(string artDescription, string thingDescription, string language, string thingId)
        {
            ArtDescription = artDescription;
            ThingDescription = thingDescription;
            Language = language;
            ThingId = thingId;
        }

        public int GetHash()
        {
            return (ArtDescription + ";" + ThingDescription).GetDeterministicHashCode();
        }
    }
}