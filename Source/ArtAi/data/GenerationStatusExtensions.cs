namespace ArtAi.data
{
    public static class GenerationStatusExtensions
    {
        public static bool HasImage(this GenerationStatus status)
        {
            return status == GenerationStatus.Done || status == GenerationStatus.Outdated;
        }
    }
}