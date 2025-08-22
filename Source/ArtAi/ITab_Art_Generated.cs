using ArtAi.data;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArtAi
{
    public class ITab_Art_Generated : ITab
    {
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
            var rect1 = DrawHeader();

            Description description = DescriptionCompArt.GetDescription(SelectedCompArt);
            var image = ImageService.GetOrGenerate(description);
            Draw(rect1, image);
        }

        private Rect DrawHeader()
        {
            Rect rect1;
            Rect rect2 = rect1 = new Rect(0.0f, 0.0f, WinSize.x, WinSize.y).ContractedBy(10f);
            Text.Font = GameFont.Medium;
            Widgets.Label(rect2, SelectedCompArt.Title.Truncate(rect2.width));
            return rect1;
        }

        private static void Draw(Rect rect1, GeneratedImage image)
        {
            Rect rect3 = rect1;
            rect3.yMin += 35f;
            if (image.Status.HasImage())
            {
                GUI.DrawTexture(rect3, image.Texture, ScaleMode.ScaleToFit);
            }
            else
            {
                Text.Font = GameFont.Small;
                Widgets.Label(rect3, image.Description);
            }
        }
    }
}