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
            return pawn != null
                   &&pawn.IsColonist
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

        private static void Draw(Description description, Thing thing, Vector2 topLeft)
        {
            if (description.IsNull || thing == null) return;

            var image = ImageService.Get(description);
            image = TransformImage(thing, image);

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
                        Find.WindowStack.Add(new Dialog_ShowImage(image.Texture,
                            () => ImageService.ForceRefresh(description)));
                        break;
                    // click on empty image -> start generation
                    case GenerationStatus.NeedGenerate:
                    case GenerationStatus.InProgress:
                        image = ImageService.GetOrGenerate(description);
                        image = TransformImage(thing, image);
                        break;
                }
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
            Draw(GetDescriptionArt(thing), thing, topLeft);
        }

        public static void DrawAvatar(Pawn thing, Vector2 topLeft)
        {
            Draw(GetDescriptionAvatar(thing), thing, topLeft);
        }

        private static GeneratedImage TransformImage(Thing thing, GeneratedImage image)
        {
            if (thing is Pawn pawn2 && pawn2.Dead)
            {
                Texture2D modifiedTexture = MakeGrayWithRibbon(image.Texture);
                return image.WithTexture(modifiedTexture);
            }
            else
            {
                return image;
            }
        }
        
        private static Texture2D MakeGrayWithRibbon(Texture2D originalTexture)
        {
            // Create a new texture for the grayscale version
            Texture2D grayscaleTexture = new Texture2D(originalTexture.width, originalTexture.height);
            Color[] pixels = originalTexture.GetPixels();

            // Convert to grayscale
            for (int i = 0; i < pixels.Length; i++)
            {
                Color pixel = pixels[i];
                float grayValue = pixel.r * 0.299f + pixel.g * 0.587f + pixel.b * 0.114f;
                pixels[i] = new Color(grayValue, grayValue, grayValue, pixel.a); // Preserve alpha
            }

            // Add a black ribbon
            int width = originalTexture.width;
            int height = originalTexture.height;
            float ribbonThickness = 0.05f; // Define ribbon thickness as a percentage of the image diagonal
            int ribbonWidth = (int)(ribbonThickness * Mathf.Sqrt(width * width + height * height));

            // Offset for shifting the ribbon
            int offset = (int)(0.5f * Mathf.Min(width, height)); // % of the smaller dimension

            // Draw a diagonal ribbon closer to the bottom-right
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Use a single offset to shift the diagonal
                    if (Mathf.Abs((x - y) - offset) < ribbonWidth)
                    {
                        int index = y * width + x;
                        pixels[index] = new Color(0, 0, 0, pixels[index].a); // Preserve the original alpha
                    }
                }
            }

            // Apply modifications to the new texture
            grayscaleTexture.SetPixels(pixels);
            grayscaleTexture.Apply();

            return grayscaleTexture;
        }
    }
}