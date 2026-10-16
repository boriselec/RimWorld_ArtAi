using System;
using System.Collections.Generic;
using ArtAi.data;
using ArtAi.Server;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Steam;

namespace ArtAi.Avatar
{
    [StaticConstructorOnStartup]
    public static class AvatarDrawer
    {
        private static CompArt GetCompArt(Thing thing) => thing?.TryGetComp<CompArt>();

        private static readonly Texture2D IconIdle
            = ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/Idle");

        private static readonly Texture2D IconOutdated
            = ContentFinder<Texture2D>.Get("UI/Overlays/Arrow");

        // dead-pawn grayscale+ribbon textures, keyed by source texture instance id
        private static readonly Dictionary<int, Texture2D> GrayRibbonCache
            = new Dictionary<int, Texture2D>();

        private const float GizmoSize = 75f;

        // as Designator_Dropdown's extra options icon
        private const float MenuMarkSize = 16f;

        // GizmoGridDrawer.GizmoSpacing is private since 1.6
        private static readonly Vector2 GizmoSpacing
            = AccessTools.Field(typeof(GizmoGridDrawer), "GizmoSpacing")
                  ?.GetValue(null) is Vector2 spacing
                ? spacing
                : new Vector2(5f, 14f);

        // draw art/avatar to the left of the gizmo grid, shifting the grid right
        public static void DrawBeforeGizmoGrid(Thing thing, ref float startX)
        {
            if (thing is MinifiedThing minifiedThing) thing = minifiedThing.InnerThing;

            if (NeedDrawAvatar(thing))
            {
                Pawn pawn = GetAvatarPawn(thing);
                Draw(GetDescriptionAvatar(pawn), pawn, ref startX);
            }
            if (NeedDrawArt(thing))
            {
                Draw(GetDescriptionArt(thing), thing, ref startX);
            }
        }

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
            if (thing is Building_CorpseCasket grave
                && grave.HasCorpse
                && NeedDraw(grave.Corpse?.InnerPawn))
                return true;
            if (thing is Building_Enterable enterable
                && enterable.innerContainer.Any
                && NeedDraw(enterable.SelectedPawn))
                return true;

            //new features enter here

            return false;
        }

        private static Pawn GetAvatarPawn(Thing thing) => thing switch
        {
            Pawn pawn => pawn,
            Corpse corpse => corpse.InnerPawn,
            Building_CorpseCasket casket => casket.Corpse?.InnerPawn,
            Building_Enterable enterable => enterable.SelectedPawn,
            _ => throw new ArgumentOutOfRangeException()
        };

        private static bool NeedDraw(Pawn pawn)
        {
            return pawn != null
                   && pawn.IsColonist
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
                description = new Description(
                    $"Title: {book.Title}.\n" +
                    $"Description: {book.FlavorUI}",
                    "Book cover. ", LanguageDatabase.activeLanguage.folderName,
                    book.ThingID);
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

        // top edge of the portrait above the gizmo grid: windows try not to cover it
        public static float PortraitTop()
        {
            float top = GizmoGridButtonDrawStart(0f).y;
            return ArtAiSettings.DoubleAvatarSize ? top - GizmoSize - GizmoSpacing.y : top;
        }

        //From game code (GizmoGridDrawer.DrawGizmoGrid)
        private static Rect GizmoGridButtonDrawStart(float startX)
        {
            float y = UI.screenHeight - 35 - GizmoSpacing.y - GizmoSize;
            if (SteamDeck.IsSteamDeck
                && SteamDeck.KeyboardShowing
                && Find.MainTabsRoot.OpenTab == MainButtonDefOf.Architect
                && ((MainTabWindow_Architect)MainButtonDefOf.Architect.TabWindow)
                    .QuickSearchWidgetFocused)
            {
                y -= 335f;
            }

            return new Rect(startX, y, GizmoSize, GizmoSize);
        }

        private static void Draw(Description description, Thing thing, ref float startX)
        {
            if (description.IsNull || thing == null) return;

            // keeps the server status of the image menu fresh (a background check every few minutes)
            ServerAvailability.Update();
            var image = ImageService.Get(description);

            Rect rect = GizmoGridButtonDrawStart(startX);
            if (ArtAiSettings.DoubleAvatarSize && image.Texture != null)
            {
                // stretch up to the top of the second gizmo row
                rect.yMin -= rect.height + GizmoSpacing.y;
                rect.width = rect.height;
            }
            startX += GizmoSpacing.x + rect.width;

            Rect outerRect = rect;
            GUI.DrawTexture(rect, Command.BGTexShrunk);
            rect = rect.ContractedBy(2);

            // click anywhere on the image -> menu: view, generate, prompt, server settings, status
            if (Widgets.ButtonInvisible(rect))
            {
                Find.WindowStack.Add(new FloatMenu(ImageMenu.Options(description)));
            }

            image = TransformImage(thing, image);
            Draw(rect, image);
            DrawMenuMark(rect);

            if (!string.IsNullOrEmpty(image.Description))
            {
                TooltipHandler.TipRegion(rect, image.Description);
            }

            // do not pass clicks to the map (it would deselect the thing)
            GenUI.AbsorbClicksInRect(outerRect);
        }

        private static void Draw(Rect rect, GeneratedImage image)
        {
            switch (image.Status)
            {
                case GenerationStatus.Done:
                    GUI.DrawTexture(rect, image.Texture);
                    break;

                case GenerationStatus.Outdated:
                    GUI.DrawTexture(rect, image.Texture);
                    DrawButton(rect, IconOutdated);
                    break;

                case GenerationStatus.InProgress:
                    if (image.Texture != null)
                    {
                        GUI.DrawTexture(rect, image.Texture);
                    }

                    DrawButton(rect, IconIdle);
                    break;

                case GenerationStatus.NeedGenerate:
                    DrawButton(rect, TexButton.OpenInspector);
                    break;

                case GenerationStatus.Error:
                    if (image.Texture != null)
                    {
                        GUI.DrawTexture(rect, image.Texture);
                    }

                    DrawButton(rect, IconOutdated);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // The game marks gizmos that open a menu by PlusOptions in the top-right corner
        // (same size and margin); flipped vertically, it marks the bottom-right one
        private static void DrawMenuMark(Rect rect)
        {
            Rect markRect = new Rect(
                rect.xMax - MenuMarkSize - 1f, rect.yMax - MenuMarkSize - 1f,
                MenuMarkSize, MenuMarkSize);
            GUI.DrawTextureWithTexCoords(markRect, Designator_Dropdown.PlusTex, new Rect(0f, 1f, 1f, -1f));
        }

        private static void DrawButton(Rect rect, Texture texture)
        {
            var width = 24f;
            var dw2 = (rect.width - 24f) / 2f;
            var dh2 = (rect.height - 24f) / 2f;
            GUI.DrawTexture(
                new Rect(rect.x + dw2, rect.y + dh2, width, width),
                texture);
        }

        private static GeneratedImage TransformImage(Thing thing, GeneratedImage image)
        {
            if (thing is Pawn pawn2 && pawn2.Dead && image.Texture != null)
            {
                int key = image.Texture.GetInstanceID();
                if (!GrayRibbonCache.TryGetValue(key, out var modifiedTexture))
                {
                    modifiedTexture = MakeGrayWithRibbon(image.Texture);
                    GrayRibbonCache[key] = modifiedTexture;
                }

                return image.WithTexture(modifiedTexture);
            }
            else
            {
                return image;
            }
        }

        private static Texture2D MakeGrayWithRibbon(Texture2D originalTexture)
        {
            if (originalTexture == null) return null;
            // Create a new texture for the grayscale version
            Texture2D grayscaleTexture = new Texture2D(
                originalTexture.width,
                originalTexture.height);
            Color[] pixels = originalTexture.GetPixels();

            // Convert to grayscale
            for (int i = 0; i < pixels.Length; i++)
            {
                Color pixel = pixels[i];
                float grayValue
                    = pixel.r * 0.299f
                      + pixel.g * 0.587f
                      + pixel.b * 0.114f;
                pixels[i] = new Color(
                    grayValue, grayValue, grayValue,
                    // Preserve alpha
                    pixel.a);
            }

            // Add a black ribbon
            int width = originalTexture.width;
            int height = originalTexture.height;
            // Define ribbon thickness as a percentage of the image diagonal
            float ribbonThickness = 0.05f;
            float diagonal = Mathf.Sqrt(width * width + height * height);
            int ribbonWidth = (int)(ribbonThickness * diagonal);

            // % of the smaller dimension
            int minDimension = Mathf.Min(width, height);
            // Offset for shifting the ribbon
            int offset = (int)(0.5f * minDimension);

            // Draw a diagonal ribbon closer to the bottom-right
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Use a single offset to shift the diagonal
                    if (Mathf.Abs((x - y) - offset) < ribbonWidth)
                    {
                        int index = y * width + x;
                        pixels[index] =
                            new Color(
                                0, 0, 0,
                                // Preserve the original alpha
                                pixels[index].a);
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
