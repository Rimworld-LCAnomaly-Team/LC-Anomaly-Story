using System.Reflection;
using HarmonyLib;
using LCAnomalyCore.Comp;
using LCAnomalyCore.Defs;
using LCAnomalyCore.Util;
using LCAnomalyStory.Compatibility;
using LCAnomalyStory.Components;
using Verse;

namespace LCAnomalyStory
{
    [StaticConstructorOnStartup]
    internal static class Bootstrap
    {
        internal const string HarmonyId = "DarthCY.LC.AnomalyStory";

        static Bootstrap()
        {
            new Harmony(HarmonyId).PatchAll(Assembly.GetExecutingAssembly());
            ManagedResearchUtility.ValidateBridgeConfiguration();
            CompAbnormality.OnAnyAbnormalityStudyCompleted += OnAbnormalityStudyCompleted;
            CompEgoExtractable.OnAnyEgoExtracted += OnEgoExtracted;
            Log.Message("[LC Anomaly Story] RimWorld 1.7 progression framework initialized.");
        }

        private static void OnAbnormalityStudyCompleted(Pawn studier, CompAbnormality abnormality, LC_StudyResult result)
        {
            if (result == LC_StudyResult.Good)
            {
                StoryComponents.Development?.RecordStatistic(GameComponent_CompanyDevelopment.StatisticGoodWorks, 1L);
            }
        }

        private static void OnEgoExtracted(CompEgoExtractable comp, EGO_TYPE egoType, ThingDef egoDef)
        {
            StoryComponents.Development?.RecordStatistic(GameComponent_CompanyDevelopment.StatisticEgoExtracted, 1L);
        }
    }

    public static class StoryComponents
    {
        public static GameComponent_CompanyDevelopment Development
        {
            get { return Current.Game == null ? null : Current.Game.GetComponent<GameComponent_CompanyDevelopment>(); }
        }
    }
}
