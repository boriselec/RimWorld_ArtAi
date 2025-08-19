using System;
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
        private static CompArt GetCompArt(Thing thing) => thing?.TryGetComp<CompArt>();

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

        private static void Draw(Description description, Rect rect, ref float startX)
        {
            if (description.IsNull) return;

            var image = ImageService.Get(description);

            if (ArtAiSettings.DoubleAvatarSize && image.Status == GenerationStatus.Done)
            {
                rect.yMin -= rect.height + GizmoGridDrawer.GizmoSpacing.x;
                rect.width = rect.height;
            }
            startX += GizmoGridDrawer.GizmoSpacing.x + rect.width;

            GUI.DrawTexture(rect, Command.BGTexShrunk);
            rect = rect.ContractedBy(2);

            if (Widgets.ButtonInvisible(rect))
            {
                switch (image.Status)
                {
                    // click on done image -> show dialog window
                    case GenerationStatus.Done:
                        var dialogWindow = new Dialog_ShowImage(
                            image.Texture,
                            // do not show refresh button if current image matches description
                            showRefreshButton: () => ImageRepo.GetExactImage(description) == null,
                            refreshCallback: () => image = ImageService.ForceRefresh(description));
                        Find.WindowStack.Add(dialogWindow);
                        break;
                    // click on empty image -> start generation
                    case GenerationStatus.NeedGenerate:
                        image = ImageService.GetOrGenerate(description);
                        break;
                }
            }

            // in progress image can be outdated
            if (image.Status == GenerationStatus.InProgress)
            {
                image = ImageService.GetOrGenerate(description);
            }

            Draw(rect, image);

            if (!string.IsNullOrEmpty(image.Description))
            {
                TooltipHandler.TipRegion(rect, image.Description);
            }
        }

        private static void Draw(Rect rect, GeneratedImage image)
        {
            switch (image.Status)
            {
                case GenerationStatus.Done:
                    GUI.DrawTexture(rect, image.Texture);
                    break;

                case GenerationStatus.InProgress:
                    DrawButton(rect, Icon_Idle);
                    break;

                case GenerationStatus.NeedGenerate:
                    DrawButton(rect, TexButton.OpenInspector);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void DrawButton(Rect rect, Texture texture)
        {
            var width = 24f;
            var dw2 = (rect.width - 24f) / 2f;
            var dh2 = (rect.height - 24f) / 2f;
            GUI.DrawTexture(new Rect(rect.x + dw2, rect.y + dh2, width, width), texture);
        }

        public static void Draw(Thing thing, Rect rect, ref float startX)
        {
            Draw(GetDescription(thing), rect, ref startX);
        }

        public static void Draw(WorldObject worldObject, Rect rect, ref float startX)
        {
            Draw(GetDescription(worldObject), rect, ref startX);
        }
    }
}