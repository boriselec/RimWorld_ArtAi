using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ArtAi.Avatar
{
    [HarmonyPatch(typeof(LoadedLanguage), nameof(LoadedLanguage.InjectIntoData_BeforeImpliedDefs))]
    public static class UntranslatedDefs
    {
        // Untranslated def labels (by def name)
        public static Dictionary<string, string> Labels = new Dictionary<string, string>();

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once ArrangeTypeMemberModifiers
        [HarmonyPrefix]
        static void Prefix()
        {
            Labels = DefDatabase<GeneDef>.AllDefs
                .ToDictionary(x => x.defName, x => x.label);
        }
    }
}
