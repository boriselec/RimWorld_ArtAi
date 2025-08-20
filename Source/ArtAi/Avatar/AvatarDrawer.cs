using System;
using ArtAi.data;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArtAi.Avatar
{
    [StaticConstructorOnStartup]
    public static class AvatarDrawer
    {
        private static CompArt GetCompArt(Thing thing) => thing?.TryGetComp<CompArt>();

        private static readonly Texture2D Icon_Idle = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/Idle");

        public static bool NeedDrawArt(Thing thing)
        {
            if (!ArtAiSettings.ShowGizmo) return false;

            CompArt compArt = GetCompArt(thing);
            if (compArt != null && compArt.Active) return true;
            if (thing is Book) return true;

            //new features enter here

            return false;
        }

        public static bool NeedDrawAvatar(Thing thing)
        {
            if (!ArtAiSettings.ShowGizmo) return false;

            if (thing is Pawn pawn && NeedDraw(pawn)) return true;
            if (thing is Corpse corpse && NeedDraw(corpse.InnerPawn)) return true;
            if (thing is Building_CorpseCasket grave && grave.HasCorpse && NeedDraw(grave.Corpse?.InnerPawn)) return true;

            //new features enter here

            return false;
        }

        private static bool NeedDraw(Pawn pawn)
        {
            return pawn.IsColonist
                   && pawn.HostFaction == null
                   && !pawn.IsPrisoner;
        }

        private static Description GetDescriptionArt(Thing thing)
        {
            Description description = default;

            CompArt compArt = GetCompArt(thing);
            if (compArt != null && compArt.Active)
            {
                description = DescriptionCompArt.GetDescription(compArt);
            }
            if (thing is Book book)
            {
                description = new Description($"Title: {book.Title}.\nDescription: {book.FlavorUI}", "Book cover. ", LanguageDatabase.activeLanguage.folderName, book.ThingID);
            }

            //new features enter here

            return description;
        }

        private static Description GetDescriptionAvatar(Thing thing)
        {
            Description description = default;

            if (thing is Pawn pawn && NeedDraw(pawn))
            {
                description = DescriptionAvatar.GetByColonist(pawn);
            }
            if (thing is Corpse corpse)
            {
                description = GetDescriptionAvatar(corpse.InnerPawn);
            }
            if (thing is Building_CorpseCasket grave)
            {
                description = GetDescriptionAvatar(grave.Corpse?.InnerPawn);
            }

            //new features enter here

            return description;
        }

        private static void Draw(Description description, Vector2 topLeft)
        {
            if (description.IsNull) return;

            var image = ImageService.Get(description);

            Rect rect = new Rect(topLeft.x, topLeft.y, 75f, 75f);
            GUI.DrawTexture(rect, Command.BGTexShrunk);
            rect = rect.ContractedBy(2);

            if (Widgets.ButtonInvisible(rect))
            {
                switch (image.Status)
                {
                    // click on done image -> show dialog window
                    case GenerationStatus.Done:
                        Find.WindowStack.Add(new Dialog_ShowImage(image.Texture));
                        break;
                    case GenerationStatus.Outdated:
                        Find.WindowStack.Add(new Dialog_ShowImage(image.Texture, () => image = ImageService.ForceRefresh(description)));
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
                case GenerationStatus.Outdated:
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

        public static void DrawArt(Thing thing, Vector2 topLeft)
        {
            Draw(GetDescriptionArt(thing), topLeft);
        }

        public static void DrawAvatar(Thing thing, Vector2 topLeft)
        {
            Draw(GetDescriptionAvatar(thing), topLeft);
        }
    }
}