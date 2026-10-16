using ArtAi.data;
using UnityEngine;
using Verse;

namespace ArtAi.Avatar
{
    // The enlarged image with "Regenerate" and "Prompt" buttons
    public class Dialog_ShowImage : Window
    {
        private const float DefaultImageSide = 500f;
        private const float ButHeight = 24f;
        private const float ButTextMargin = 8f;
        private const float ButSpacing = 6f;
        // buttons below the image
        private const float ButtonsRow = ButHeight + 10f;

        public readonly Description Description;

        // shown until the thing has an image of its own (it always has, when opened from the menu)
        private readonly Texture2D openedImage;

        public override Vector2 InitialSize => WindowSize(DefaultImageSide);

        public Dialog_ShowImage(Texture2D image, Description description)
        {
            closeOnCancel = true;
            closeOnAccept = false;
            doCloseButton = false;
            doCloseX = true;
            resizeable = true;
            draggable = true;

            openedImage = image;
            Description = description;
        }

        // window size for a square image of the given side
        public static Vector2 WindowSize(float imageSide)
        {
            return new Vector2(imageSide + 2 * StandardMargin, imageSide + ButtonsRow + 2 * StandardMargin);
        }

        // side of the square image that fits in the window size
        public static float ImageSide(Vector2 windowSize)
        {
            return Mathf.Min(windowSize.x - 2 * StandardMargin, windowSize.y - ButtonsRow - 2 * StandardMargin);
        }

        public override void PostOpen()
        {
            base.PostOpen();
            ImageWindows.Arrange();
        }

        public override void DoWindowContents(Rect inRect)
        {
            // the current image: a regenerated one replaces the old one when it is ready
            Texture2D image = ImageService.Get(Description).Texture ?? openedImage;
            var imageRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height - ButtonsRow);
            GUI.DrawTexture(imageRect, image, ScaleMode.ScaleToFit);

            float x = inRect.x;
            float y = inRect.yMax - ButHeight;
            // hidden while generating: the new image is on its way
            if (!ImageService.IsGenerating(Description)
                && SubtleButton(ref x, y, "AiArtMenuRegenerate".Translate()))
            {
                // a new seed, otherwise the server would return the same picture
                ImageService.Generate(Description, seed: ImageMenu.NewSeed());
            }
            if (SubtleButton(ref x, y, "AiArtButtonPrompt".Translate()))
            {
                ImageWindows.OpenPrompt(Description);
            }
        }

        // Small subtle button (as the game's main tab buttons), so it doesn't distract from the image.
        // Sized by its text: ButtonTextSubtle always draws the label with the Small font.
        private static bool SubtleButton(ref float x, float y, string label)
        {
            Text.Font = GameFont.Small;
            var rect = new Rect(x, y, Text.CalcSize(label).x + 2 * ButTextMargin, ButHeight);
            x = rect.xMax + ButSpacing;
            return Widgets.ButtonTextSubtle(rect, label, textLeftMargin: ButTextMargin);
        }
    }
}
