using ArtAi.data;
using UnityEngine;
using Verse;

namespace ArtAi.Avatar
{
    // View and edit the text sent to the generation server (like the game's Dialog_EditIdeoDescription)
    public class Dialog_Prompt : Window
    {
        private static readonly Vector2 ButSize = new Vector2(150f, 38f);

        private const float HeaderHeight = 35f;

        private readonly Description description;

        private string prompt;

        public override Vector2 InitialSize => new Vector2(700f, 400f);

        public Dialog_Prompt(Description description)
        {
            this.description = description;
            // the prompt of the current image if it was saved, otherwise the one built from the game text
            prompt = ImageRepo.GetSavedPrompt(description) ?? Generator.BuildPrompt(description);
            doCloseX = true;
            draggable = true;
            resizeable = true;
        }

        public override void PostOpen()
        {
            base.PostOpen();
            ImageWindows.Arrange();
        }

        // Enter inserts a new line in the text area instead of closing the window
        public override void OnAcceptKeyPressed()
        {
            Event.current.Use();
        }

        public override void DoWindowContents(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, HeaderHeight), "AiArtPromptTitle".Translate());
            Text.Font = GameFont.Small;

            float top = rect.y + HeaderHeight + 10f;
            string hint = "AiArtPromptHint".Translate();
            float hintHeight = Text.CalcHeight(hint, rect.width);
            Widgets.Label(new Rect(rect.x, top, rect.width, hintHeight), hint);
            top += hintHeight + 10f;

            prompt = Widgets.TextArea(
                new Rect(rect.x, top, rect.width, rect.height - ButSize.y - 17f - top),
                prompt);

            if (Widgets.ButtonText(
                    new Rect(rect.x, rect.height - ButSize.y, ButSize.x, ButSize.y),
                    "AiArtPromptReset".Translate()))
            {
                prompt = Generator.BuildPrompt(description);
            }

            bool generating = ImageService.IsGenerating(description);
            string regenerate = "AiArtMenuRegenerate".Translate();
            // wide enough for the label in any language
            float width = Mathf.Max(ButSize.x, Text.CalcSize(regenerate).x + 20f);
            if (Widgets.ButtonText(
                    new Rect(rect.width - width, rect.height - ButSize.y, width, ButSize.y),
                    regenerate,
                    active: !generating))
            {
                // the window stays open: the image window shows the new image when it is ready
                ImageService.Generate(description, prompt, ImageMenu.NewSeed());
            }
        }
    }
}
