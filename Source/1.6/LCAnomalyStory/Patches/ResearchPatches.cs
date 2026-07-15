using HarmonyLib;
using LCAnomalyStory.Compatibility;
using RimWorld;
using Verse;

namespace LCAnomalyStory.Patches
{
    [HarmonyPatch(typeof(ResearchProjectDef), "get_IsHidden")]
    internal static class Patch_ResearchProjectDef_IsHidden
    {
        private static void Postfix(ResearchProjectDef __instance, ref bool __result)
        {
            if (ManagedResearchUtility.IsManaged(__instance))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(ResearchProjectDef), "get_CanStartNow")]
    internal static class Patch_ResearchProjectDef_CanStartNow
    {
        private static void Postfix(ResearchProjectDef __instance, ref bool __result)
        {
            if (ManagedResearchUtility.IsManaged(__instance))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.SetCurrentProject))]
    internal static class Patch_ResearchManager_SetCurrentProject
    {
        private static bool Prefix(ResearchProjectDef proj)
        {
            if (!ManagedResearchUtility.IsManaged(proj))
            {
                return true;
            }
            Messages.Message("LCStory_ResearchManagedExternally".Translate(proj.LabelCap), MessageTypeDefOf.RejectInput, false);
            return false;
        }
    }

    [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.AddProgress))]
    internal static class Patch_ResearchManager_AddProgress
    {
        private static bool Prefix(ResearchProjectDef proj)
        {
            return !ManagedResearchUtility.IsManaged(proj);
        }
    }
}

