using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Steam;

namespace ArtAi.Avatar
{
    [HarmonyPatch(typeof(GizmoGridDrawer))]
    [HarmonyPatch("DrawGizmoGrid")]
    public static class GizmoGridDrawer_DrawGizmoGrid_Patch
    {

        //From game code
        private static Rect GizmoGridButtonDrawStart(float startX)
        {
            float num2 = (float)(Verse.UI.screenHeight - 35) - GizmoGridDrawer.GizmoSpacing.y - 75f;
            if (SteamDeck.IsSteamDeck && SteamDeck.KeyboardShowing && Find.MainTabsRoot.OpenTab == MainButtonDefOf.Architect && ((MainTabWindow_Architect)MainButtonDefOf.Architect.TabWindow).QuickSearchWidgetFocused)
            {
                num2 -= 335f;
            }
            Vector2 vector = new Vector2(startX, num2);
            return new Rect(vector.x, vector.y, 75f, 75f);
        }

        [HarmonyPrefix]
        public static bool Prefix(ref float startX)
        {
            WorldObject selectedWorldObject = null;
            Thing selectedThing = null;
            if (WorldRendererUtility.WorldRenderedNow)
            {
                selectedWorldObject = Find.WorldSelector.SingleSelectedObject;
                if (selectedWorldObject == null || !AvatarDrawer.NeedDraw(selectedWorldObject)) return true;
            }
            else
            {
                selectedThing = Find.Selector.SingleSelectedThing;
                if (selectedThing == null || !AvatarDrawer.NeedDraw(selectedThing)) return true;
                if (selectedThing is MinifiedThing minifiedThing) selectedThing = minifiedThing.InnerThing;
            }

            var rect = GizmoGridButtonDrawStart(startX);
            if (ArtAiSettings.DoubleAvatarSize)
            {
                rect.yMin -= rect.height + GizmoGridDrawer.GizmoSpacing.x;
                rect.width = rect.height;
            }
            startX += GizmoGridDrawer.GizmoSpacing.x + rect.width;

            if (selectedWorldObject != null) AvatarDrawer.Draw(selectedWorldObject, rect);
            if (selectedThing != null) AvatarDrawer.Draw(selectedThing, rect);

            GenUI.AbsorbClicksInRect(rect);

            return true;
        }
    }
}
