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
            labelKey = "View";
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

                cachedImageDescription = "TODO";
            }

            Rect rect3 = rect1;
            rect3.yMin += 35f;
            Text.Font = GameFont.Small;
            Widgets.Label(rect3, cachedImageDescription);
        }
    }
}