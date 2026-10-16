using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace ArtAi.Avatar
{
    // DrawGizmoGrid is also used by the architect tab,
    // so draw only when called for selected objects
    [HarmonyPatch(typeof(GizmoGridDrawer), nameof(GizmoGridDrawer.DrawGizmoGridFor))]
    public static class GizmoGridDrawer_DrawGizmoGridFor_Patch
    {
        public static Thing SelectedThing;

        [HarmonyPrefix]
        public static void Prefix(IEnumerable<object> selectedObjects)
        {
            SelectedThing = null;
            if (selectedObjects is IList<object> list
                && list.Count == 1
                && list[0] is Thing thing)
            {
                SelectedThing = thing;
            }
        }

        [HarmonyPostfix]
        public static void Postfix()
        {
            SelectedThing = null;
        }
    }

    [HarmonyPatch(typeof(GizmoGridDrawer), nameof(GizmoGridDrawer.DrawGizmoGrid))]
    public static class GizmoGridDrawer_DrawGizmoGrid_Patch
    {
        // ReSharper disable InconsistentNaming
        [HarmonyPrefix]
        public static void Prefix(ref float startX)
        {
            Thing thing = GizmoGridDrawer_DrawGizmoGridFor_Patch.SelectedThing;
            // game skips layout event too
            if (thing == null || Event.current.type == EventType.Layout) return;

            AvatarDrawer.DrawBeforeGizmoGrid(thing, ref startX);
        }
    }
}
