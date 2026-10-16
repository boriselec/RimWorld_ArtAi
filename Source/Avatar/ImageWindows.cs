using ArtAi.data;
using UnityEngine;
using Verse;

namespace ArtAi.Avatar
{
    // The image window and the prompt window: opened together, placed side by side
    public static class ImageWindows
    {
        // gap to the screen edges and above the portrait
        private const float Gap = 10f;

        private const float MinImageSide = 150f;

        public static void OpenImage(Description description, Texture2D texture)
        {
            Find.WindowStack.Add(new Dialog_ShowImage(texture, description));
        }

        // The prompt is opened together with the image it belongs to
        public static void OpenPrompt(Description description)
        {
            Texture2D texture = ImageService.Get(description).Texture;
            var imageWindow = Find.WindowStack.WindowOfType<Dialog_ShowImage>();
            if (texture != null && (imageWindow == null || !imageWindow.Description.Equals(description)))
            {
                OpenImage(description, texture);
            }
            Find.WindowStack.Add(new Dialog_Prompt(description));
        }

        // Called when one of the windows opens. With both open: image on the left, prompt on the right,
        // centered horizontally, common bottom edge above the portrait if the screen allows;
        // the image window shrinks if they do not fit on the screen.
        public static void Arrange()
        {
            var imageWindow = Find.WindowStack.WindowOfType<Dialog_ShowImage>();
            var promptWindow = Find.WindowStack.WindowOfType<Dialog_Prompt>();
            if (imageWindow == null || promptWindow == null) return;

            float maxWidth = UI.screenWidth - 2 * Gap;
            float maxHeight = UI.screenHeight - 2 * Gap;
            Rect prompt = promptWindow.windowRect;
            prompt.height = Mathf.Min(prompt.height, maxHeight);

            float side = Dialog_ShowImage.ImageSide(imageWindow.windowRect.size);
            side = Mathf.Min(side, Dialog_ShowImage.ImageSide(new Vector2(maxWidth - prompt.width, maxHeight)));
            Rect image = new Rect(Vector2.zero, Dialog_ShowImage.WindowSize(Mathf.Max(side, MinImageSide)));

            float height = Mathf.Max(image.height, prompt.height);
            float left = Mathf.Max(Gap, (UI.screenWidth - image.width - prompt.width) / 2f);
            float bottom = AvatarDrawer.PortraitTop() - Gap;
            if (bottom - height < Gap)
            {
                // the portrait cannot stay visible: as low as needed to fit on the screen
                bottom = Mathf.Min(UI.screenHeight - Gap, Gap + height);
            }

            image.position = new Vector2(left, bottom - image.height);
            prompt.position = new Vector2(left + image.width, bottom - prompt.height);
            imageWindow.windowRect = image;
            promptWindow.windowRect = prompt;
        }
    }
}
