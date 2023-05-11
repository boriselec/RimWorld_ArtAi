using System;
using UnityEngine;
using Verse;

namespace ArtAi.Avatar
{
    public class Dialog_ShowImage : Window
    {
        private Texture2D Image;
        private Action RefreshCallback;

        public override Vector2 InitialSize
        {
            get { return LastInitialSize; }
        }

        private static float _menuOffset = 50f;

        static Vector2 LastInitialSize = new Vector2(200f, 200f + _menuOffset);
        static Vector2 LastInitialPos = new Vector2(-1f, -1f);

        public Dialog_ShowImage(Texture2D image, Action refreshCallback)
        {
            closeOnCancel = true;
            closeOnAccept = false;
            doCloseButton = false;
            doCloseX = true;
            resizeable = true;
            draggable = true;

            Image = image;
            RefreshCallback = refreshCallback;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            if (LastInitialPos.y == -1f && LastInitialPos.x == -1f)
            {
                windowRect.x = (Screen.width - windowRect.width) / 2f;
                windowRect.y = (Screen.height - windowRect.height) / 2f;
                LastInitialPos = windowRect.position;
            }
            else
                windowRect.Set(LastInitialPos.x, LastInitialPos.y, windowRect.width, windowRect.height + _menuOffset);

            Vector2 center = new Vector2(windowRect.x + windowRect.width / 2f, windowRect.y + windowRect.height / 2f);
            Vector2 size = new Vector2(Image.width, Image.height);
            if (size.x > Screen.width - 200)
            {
                var newx = Screen.width - 200;
                size.y = size.y * newx / size.x;
                size.x = newx;
            }
            if (size.y > Screen.height - 100)
            {
                var newy = Screen.height - 100;
                size.x = size.x * newy / size.y;
                size.y = newy;
            }
            windowRect.Set(center.x - size.x / 2f, center.y - size.y / 2f, size.x, size.y + _menuOffset);
        }

        public override void PostClose()
        {
            LastInitialSize = windowRect.size;
            LastInitialPos = windowRect.position;
        }

        public override void DoWindowContents(Rect inRect)
        {
            var recDiv = Image.width / Image.height - inRect.width / inRect.height;
            var rect = (recDiv > 0)
                ? new Rect(inRect.x, inRect.y, inRect.width, inRect.height / (1f + (inRect.height / inRect.width - Image.height / Image.width)))
                : new Rect(inRect.x, inRect.y, inRect.width / (1f - recDiv), inRect.height);

            rect.x += (inRect.width - rect.width) / 2f;
            rect.y += (inRect.height - rect.height) / 2f;

            rect.y -= _menuOffset / 2f;

            GUI.DrawTexture(rect, Image);

            if (Widgets.ButtonText(new Rect(rect.x, rect.y + rect.height + 20f, 115f, 25f), "refresh"))
            {
                RefreshCallback.Invoke();
                Close();
            }
        }

    }
}
