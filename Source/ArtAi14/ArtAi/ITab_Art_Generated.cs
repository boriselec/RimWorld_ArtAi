using RimWorld;
using UnityEngine;
using Verse;

namespace ArtAi
{
    public class ITab_Art_Generated : ITab
    {
        private static string cachedImageDescription;
        private static CompArt cachedImageSource;
        private static TaleReference cachedTaleRef;
        private static Texture2D cachedImageGenerated;
        private static readonly Vector2 WinSize = new Vector2(400f, 300f);

        private CompArt SelectedCompArt
        {
            get
            {
                Thing thing = Find.Selector.SingleSelectedThing;
                if (thing is MinifiedThing minifiedThing)
                    thing = minifiedThing.InnerThing;
                return thing == null ? null : thing.TryGetComp<CompArt>();
            }
        }

        public override bool IsVisible => SelectedCompArt != null && SelectedCompArt.Active;

        public ITab_Art_Generated()
        {
            size = WinSize;
            labelKey = "TabArtView";
        }

        protected override void FillTab()
        {
            Rect rect1;
            Rect rect2 = rect1 = new Rect(0.0f, 0.0f, WinSize.x, WinSize.y).ContractedBy(10f);
            Text.Font = GameFont.Medium;
            Widgets.Label(rect2, SelectedCompArt.Title.Truncate(rect2.width));
            if (cachedImageSource != SelectedCompArt ||
                cachedTaleRef != SelectedCompArt.TaleRef)
            {
                cachedImageDescription = SelectedCompArt.GenerateImageDescription();
                cachedImageSource = SelectedCompArt;
                cachedTaleRef = SelectedCompArt.TaleRef;
                cachedImageGenerated = null;
            }

            Rect rect3 = rect1;
            rect3.yMin += 35f;
            Texture2D texture2D = GetGeneratedTexture();
            if (texture2D != null)
            {
                GUI.DrawTexture(rect3, texture2D, ScaleMode.ScaleToFit);
            }
            else
            {
                Text.Font = GameFont.Small;
                Widgets.Label(rect3, "Loading...");
            }
        }

        private static Texture2D GetGeneratedTexture()
        {
            if (cachedImageGenerated != null)
            {
                return cachedImageGenerated;
            }
            
            Texture2D texture2D = ContentFinder<Texture2D>.Get("TODO", false);
            if (texture2D != null)
            {
                cachedImageGenerated = texture2D;
            }
            return texture2D;
        }
    }
}