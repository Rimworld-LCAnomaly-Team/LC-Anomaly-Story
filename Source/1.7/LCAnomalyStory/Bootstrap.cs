using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using LCAnomalyCore.Buildings;
using LCAnomalyCore.Comp;
using LCAnomalyCore.Defs;
using LCAnomalyCore.GameComponent;
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
            MapComponent_LCQliphothMeltdown.OnMeltdownWaveTriggered += OnMeltdownWaveTriggered;
            Building_AbnormalityContainmentTerminal.OnAnyMeltdownResolved += OnMeltdownResolved;
            Building_AbnormalityContainmentTerminal.OnAnyMeltdownBreached += OnMeltdownBreached;
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

        private static void OnMeltdownWaveTriggered(Map map, int meltdownLevel, List<Building_AbnormalityContainmentTerminal> triggered)
        {
            StoryComponents.Development?.RecordStatistic(GameComponent_CompanyDevelopment.StatisticMeltdownWaves, 1L);
        }

        private static void OnMeltdownResolved(Building_AbnormalityContainmentTerminal terminal)
        {
            StoryComponents.Development?.RecordStatistic(GameComponent_CompanyDevelopment.StatisticMeltdownsResolved, 1L);
        }

        private static void OnMeltdownBreached(Building_AbnormalityContainmentTerminal terminal, int lostPe)
        {
            StoryComponents.Development?.RecordStatistic(GameComponent_CompanyDevelopment.StatisticMeltdownsBreached, 1L);
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
