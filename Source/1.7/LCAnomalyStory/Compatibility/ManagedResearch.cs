using RimWorld;
using System.Collections.Generic;
using System.Linq;
using LCAnomalyStory.Defs;
using LCAnomalyStory.Rewards;
using Verse;

namespace LCAnomalyStory.Compatibility
{
    public class ManagedResearchExtension : DefModExtension
    {
    }

    public static class ManagedResearchUtility
    {
        public static bool IsManaged(ResearchProjectDef project)
        {
            return project != null && project.GetModExtension<ManagedResearchExtension>() != null;
        }

        public static void StopManagedCurrentProject()
        {
            if (Find.ResearchManager == null)
            {
                return;
            }
            ResearchProjectDef current = Find.ResearchManager.GetProject();
            if (IsManaged(current))
            {
                Find.ResearchManager.StopProject(current);
            }
        }

        public static void ValidateBridgeConfiguration()
        {
            List<ResearchProjectDef> managed = DefDatabase<ResearchProjectDef>.AllDefsListForReading.Where(IsManaged).ToList();
            HashSet<ResearchProjectDef> rewarded = new HashSet<ResearchProjectDef>(
                DefDatabase<CompanyDevelopmentDef>.AllDefsListForReading
                    .Where(node => node.rewards != null)
                    .SelectMany(node => node.rewards)
                    .OfType<DevelopmentReward_FinishResearch>()
                    .Where(reward => reward.project != null)
                    .Select(reward => reward.project));

            foreach (ResearchProjectDef project in managed)
            {
                if (!rewarded.Contains(project))
                {
                    Log.Error("[LC Anomaly Story] Managed research has no development reward bridge: " + project.defName);
                }
            }
            foreach (ResearchProjectDef project in rewarded)
            {
                if (!IsManaged(project))
                {
                    Log.Warning("[LC Anomaly Story] Development reward finishes an unmarked research project: " + project.defName);
                }
            }
            Log.Message("[LC Anomaly Story] Validated " + managed.Count + " managed Core research bridges.");
        }
    }
}
