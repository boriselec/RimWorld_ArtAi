using ArtAi.data;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ArtAi.Avatar
{
    [StaticConstructorOnStartup]
    public static class AvatarDrawer
    {
        public static bool DoubleSize
        { 
            get { return _doubleSize && !_activIconNeedUpdate; }
            set { _doubleSize = value; }
        }

        private static bool _doubleSize = true;
        private static bool _activIconNeedUpdate;

        private static CompArt GetCompArt(Thing thing) => thing == null ? null : thing.TryGetComp<CompArt>();

        private static readonly Texture2D Icon_Idle = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/Idle");

        public static bool NeedDraw(Thing thing)
        {
            if (!ArtAiSettings.ShowGizmo) return false;

            CompArt compArt = GetCompArt(thing);
            if (compArt != null && compArt.Active) return true;
            if (thing is Pawn pawn && NeedDraw(pawn)) return true;

            //new features enter here

            return false;
        }

        public static bool NeedDraw(WorldObject worldObject)
        {

            //new features enter here

            return false;
        }

        private static bool NeedDraw(Pawn pawn)
        {
            return pawn.Spawned
                   && pawn.IsColonist
                   && pawn.HostFaction == null
                   && !pawn.IsPrisoner;
        }

        private static Description GetDescription(Thing thing)
        {
            Description description = default;

            CompArt compArt = GetCompArt(thing);
            if (compArt != null && compArt.Active)
            {
                description = DescriptionCompArt.GetDescription(compArt);
            }
            if (thing is Pawn pawn && NeedDraw(pawn))
            {
                description = DescriptionAvatar.GetByColonist(pawn);
            }

            //new features enter here

            return description;
        }

        private static Description GetDescription(WorldObject worldObject)
        {
            Description description = default;


            //new features enter here

            return description;
        }

        private static void Draw(Description description, Rect rect)
        {
            if (description.IsNull) return;

            var image = Generator.Generate(description, true);
            if (image.Status == GenerationStatus.InProgress)
            {
                image = Generator.Generate(description);
            }
            var click = false;
            if (Widgets.ButtonInvisible(rect))
            {
                if (image.Status == GenerationStatus.NeedGenerate)
                {
                    image = Generator.Generate(description);
                }
                else
                    click = true;
            }

            GUI.DrawTexture(rect, Command.BGTexShrunk); //BGTex);
            rect = rect.ContractedBy(2);

            if (image.Status != GenerationStatus.Done)
            {
                _activIconNeedUpdate = true;
                var texture = image.Status == GenerationStatus.NeedGenerate ? TexButton.OpenInspector : Icon_Idle;
                var width = 24f;
                var dw2 = (rect.width - 24f) / 2f;
                var dh2 = (rect.height - 24f) / 2f;
                GUI.DrawTexture(new Rect(rect.x + dw2, rect.y + dh2, width, width), texture);
            }
            else
            {
                _activIconNeedUpdate = false;
                GUI.DrawTexture(rect, image.Texture);

                if (click)
                {
                    DoubleSize = !DoubleSize;
                    if (DoubleSize)
                    {
                        void RefreshCallback()
                        {
                            Generator.setForcedRefresh(description);
                            Draw(description, rect);
                        }
                        Find.WindowStack.Add(new Dialog_ShowImage(image.Texture, RefreshCallback));
                    }
                }
            }

            if (!string.IsNullOrEmpty(image.Description))
            {
                TooltipHandler.TipRegion(rect, image.Description);
            }
        }

        public static void Draw(Thing thing, Rect rect) => Draw(GetDescription(thing), rect);

        public static void Draw(WorldObject worldObject, Rect rect) => Draw(GetDescription(worldObject), rect);

    }
}