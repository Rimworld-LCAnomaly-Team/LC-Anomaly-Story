using System.Linq;
using LCAnomalyStory.Defs;
using LudeonTK;
using RimWorld;
using Verse;

namespace LCAnomalyStory.Debug
{
    internal static class DevelopmentDebugActions
    {
        [DebugAction("LC Anomaly Story", "输出发展诊断")]
        private static void LogDiagnostics()
        {
            Log.Message("[LC Anomaly Story] " + (StoryComponents.Development?.DiagnosticSummary() ?? "component unavailable"));
        }

        [DebugAction("LC Anomaly Story", "强制完成下一个可用节点")]
        private static void CompleteNextAvailableNode()
        {
            var component = StoryComponents.Development;
            CompanyDevelopmentDef node = DefDatabase<CompanyDevelopmentDef>.AllDefsListForReading
                .Where(component.IsAvailable)
                .OrderBy(def => def.phase)
                .ThenBy(def => def.displayOrder)
                .FirstOrDefault();
            if (node != null)
            {
                component.TryCompleteNode(node, true);
                Messages.Message("DEV: completed " + node.defName, MessageTypeDefOf.TaskCompletion, false);
            }
        }

        [DebugAction("LC Anomaly Story", "重置发展状态（不撤回研究解锁）")]
        private static void ResetProgression()
        {
            StoryComponents.Development?.ResetForDebug();
            Messages.Message("DEV: Story progression reset; finished research tokens were preserved", MessageTypeDefOf.CautionInput, false);
        }
    }
}
