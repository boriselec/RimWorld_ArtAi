using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace ArtAi.Avatar
{
    [HarmonyPatch(typeof (Thing), "GetGizmos")]
    public class ThingGizmoPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref IEnumerable<Gizmo> __result, Thing __instance)
        {
            if (AvatarDrawer.NeedDrawArt(__instance))
            {
                __result = __result.Prepend(new ArtGizmo(__instance));
            }
            if (AvatarDrawer.NeedDrawAvatar(__instance))
            {
                __result = __result.Prepend(new AvatarGizmo(__instance));
            }
        }
    }
}