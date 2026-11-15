using HarmonyLib;
using LCAnomalyCore.Comp;
using LCAnomalyStory.Components;
using System.Reflection;
using Verse;

namespace LCAnomalyStory.Patches
{
    [HarmonyPatch(typeof(CompAbnormality), nameof(CompAbnormality.Notify_Studied))]
    internal static class Patch_CompAbnormality_NotifyStudied
    {
        private static void Postfix(bool interrupted)
        {
            if (!interrupted)
            {
                StoryComponents.Development?.RecordStatistic(GameComponent_CompanyDevelopment.StatisticValidWorks, 1L);
            }
        }
    }

    [HarmonyPatch(typeof(CompPeBoxProduce), nameof(CompPeBoxProduce.CheckSpawnPeBox))]
    internal static class Patch_CompPeBoxProduce_CheckSpawnPeBox
    {
        private static void Postfix(CompPeBoxProduce __instance, Pawn studier, int amount)
        {
            if (studier != null && amount > 0 && __instance.Props.peBoxDef != null)
            {
                StoryComponents.Development?.RecordStatistic(GameComponent_CompanyDevelopment.StatisticPeBoxProduced, amount);
            }
        }
    }

    [HarmonyPatch]
    internal static class Patch_CompAbnormality_StudySuccessRateCalculate
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(CompAbnormality), "StudySuccessRateCalculate");
        }

        private static void Postfix(ref float __result)
        {
            StoryComponents.Development?.ModifyActiveExaminationStudySuccessRate(ref __result);
        }
    }
}
