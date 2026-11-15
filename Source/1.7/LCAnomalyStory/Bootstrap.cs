using System.Reflection;
using HarmonyLib;
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
            Log.Message("[LC Anomaly Story] RimWorld 1.7 progression framework initialized.");
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
