using System.Linq;
using LCAnomalyStory.Conditions;
using LCAnomalyStory.Components;
using LCAnomalyStory.Defs;
using RimWorld;
using UnityEngine;
using Verse;

namespace LCAnomalyStory.UI
{
    public class MainTabWindow_CompanyDevelopment : MainTabWindow
    {
        private Vector2 nodeScrollPosition;
        private Vector2 detailScrollPosition;
        private CompanyDevelopmentDef selectedNode;

        public override Vector2 InitialSize
        {
            get { return new Vector2(1000f, 720f); }
        }

        public override void PreOpen()
        {
            base.PreOpen();
            GameComponent_CompanyDevelopment component = StoryComponents.Development;
            component?.EvaluateAvailableNodes();
            if (selectedNode == null)
            {
                selectedNode = DefDatabase<CompanyDevelopmentDef>.AllDefsListForReading
                    .Where(def => !def.hidden)
                    .OrderBy(def => def.phase)
                    .ThenBy(def => def.displayOrder)
                    .FirstOrDefault();
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            GameComponent_CompanyDevelopment component = StoryComponents.Development;
            if (component == null)
            {
                Widgets.Label(inRect, "LCStory_NoGameComponent".Translate());
                return;
            }

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 34f), "LCStory_WindowTitle".Translate(component.CurrentPhase));
            Text.Font = GameFont.Small;

            Rect body = new Rect(inRect.x, inRect.y + 42f, inRect.width, inRect.height - 42f);
            Rect left = new Rect(body.x, body.y, 300f, body.height);
            Rect right = new Rect(left.xMax + 12f, body.y, body.width - left.width - 12f, body.height);
            Widgets.DrawMenuSection(left);
            DrawNodeList(left.ContractedBy(8f), component);
            Rect directiveRect = new Rect(right.x, right.y, right.width, 205f);
            Rect detailRect = new Rect(right.x, directiveRect.yMax + 10f, right.width, right.height - directiveRect.height - 10f);
            Widgets.DrawMenuSection(directiveRect);
            Widgets.DrawMenuSection(detailRect);
            DrawDirectivePanel(directiveRect.ContractedBy(10f), component);
            DrawNodeDetails(detailRect.ContractedBy(12f), component);
        }

        private void DrawNodeList(Rect rect, GameComponent_CompanyDevelopment component)
        {
            CompanyDevelopmentDef[] nodes = DefDatabase<CompanyDevelopmentDef>.AllDefsListForReading
                .Where(def => !def.hidden)
                .OrderBy(def => def.phase)
                .ThenBy(def => def.displayOrder)
                .ThenBy(def => def.defName)
                .ToArray();
            Rect view = new Rect(0f, 0f, rect.width - 16f, nodes.Length * 48f + 8f);
            Widgets.BeginScrollView(rect, ref nodeScrollPosition, view);
            float y = 0f;
            foreach (CompanyDevelopmentDef node in nodes)
            {
                Rect row = new Rect(0f, y, view.width, 42f);
                string status = component.IsCompleted(node)
                    ? "LCStory_Status_Completed".Translate()
                    : component.IsAvailable(node)
                        ? "LCStory_Status_Available".Translate()
                        : "LCStory_Status_Locked".Translate();
                if (Widgets.ButtonText(row, node.LabelCap + "\n" + status, drawBackground: selectedNode != node))
                {
                    selectedNode = node;
                }
                y += 48f;
            }
            Widgets.EndScrollView();
        }

        private void DrawNodeDetails(Rect rect, GameComponent_CompanyDevelopment component)
        {
            if (selectedNode == null)
            {
                Widgets.Label(rect, "LCStory_SelectNode".Translate());
                return;
            }

            Rect view = new Rect(0f, 0f, rect.width - 16f, 800f);
            Widgets.BeginScrollView(rect, ref detailScrollPosition, view);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(view);

            Text.Font = GameFont.Medium;
            listing.Label(selectedNode.LabelCap);
            Text.Font = GameFont.Small;
            listing.Label("LCStory_PhaseDepartmentAndKind".Translate(
                selectedNode.phase,
                selectedNode.department == null ? "-" : selectedNode.department.LabelCap,
                ("LCStory_NodeKind_" + selectedNode.kind).Translate()));
            listing.GapLine();
            listing.Label(selectedNode.description);
            listing.Gap();

            listing.Label("LCStory_Prerequisites".Translate());
            if (selectedNode.prerequisites == null || selectedNode.prerequisites.Count == 0)
            {
                listing.Label("  " + "LCStory_None".Translate());
            }
            else
            {
                foreach (CompanyDevelopmentDef prerequisite in selectedNode.prerequisites)
                {
                    listing.Label("  " + (component.IsCompleted(prerequisite) ? "✓ " : "○ ") + prerequisite.LabelCap);
                }
            }

            listing.Gap();
            listing.Label("LCStory_Conditions".Translate());
            if (selectedNode.conditions == null || selectedNode.conditions.Count == 0)
            {
                listing.Label("  " + "LCStory_None".Translate());
            }
            else
            {
                foreach (Conditions.DevelopmentCondition condition in selectedNode.conditions)
                {
                    bool satisfied = condition.IsSatisfied(component, selectedNode);
                    listing.Label("  " + (satisfied ? "✓ " : "○ ") + condition.ProgressText(component, selectedNode));
                }
            }

            DevelopmentCondition_ExaminationPassed examinationCondition = selectedNode.conditions?
                .OfType<DevelopmentCondition_ExaminationPassed>()
                .FirstOrDefault();
            if (examinationCondition?.examination != null)
            {
                bool prerequisitesMet = selectedNode.PrerequisitesCompleted(component)
                    && selectedNode.conditions.Where(condition => condition != examinationCondition)
                        .All(condition => condition.IsSatisfied(component, selectedNode));
                DrawExaminationControls(listing, component, examinationCondition.examination, prerequisitesMet);
            }

            listing.Gap();
            listing.Label("LCStory_Rewards".Translate());
            if (selectedNode.rewards == null || selectedNode.rewards.Count == 0)
            {
                listing.Label("  " + "LCStory_None".Translate());
            }
            else
            {
                foreach (Rewards.DevelopmentReward reward in selectedNode.rewards)
                {
                    listing.Label("  • " + reward.Description);
                }
            }

            listing.GapLine();
            if (component.IsCompleted(selectedNode))
            {
                listing.Label("LCStory_NodeAlreadyCompleted".Translate());
            }
            else if (selectedNode.manualCompletion)
            {
                bool canComplete = component.CanComplete(selectedNode);
                if (canComplete && listing.ButtonText("LCStory_ApproveNode".Translate()))
                {
                    component.TryCompleteNode(selectedNode);
                }
                else if (!canComplete)
                {
                    listing.Label("LCStory_ApproveNodeLocked".Translate());
                }
            }
            else
            {
                listing.Label("LCStory_NodeCompletesAutomatically".Translate());
            }

            listing.End();
            Widgets.EndScrollView();
        }

        private static void DrawExaminationControls(Listing_Standard listing, GameComponent_CompanyDevelopment component, CompanyExaminationDef examination, bool prerequisitesMet)
        {
            ExaminationRuntime runtime = component.GetExaminationRuntime(examination);
            listing.Gap();
            listing.Label(examination.LabelCap);
            listing.Label(examination.description);
            if (runtime.status == ExaminationStatus.Active)
            {
                int ticksRemaining = component.ExaminationTicksRemaining(examination);
                if (ticksRemaining >= 0)
                {
                    listing.Label("LCStory_ExamTimeRemaining".Translate(FormatTicks(ticksRemaining)));
                }
                foreach (string progressLine in component.GetExaminationProgressLines(examination))
                {
                    listing.Label("  - " + progressLine);
                }
            }
            else if (runtime.status == ExaminationStatus.Cooldown && component.ExaminationCooldownRemaining(examination) > 0)
            {
                listing.Label("LCStory_ExamCooldownRemaining".Translate(FormatTicks(component.ExaminationCooldownRemaining(examination))));
            }
            else if (!prerequisitesMet)
            {
                listing.Label("LCStory_ExamPrerequisitesMissing".Translate());
            }
            else if (runtime.status != ExaminationStatus.Passed)
            {
                string rejectionReason;
                if (component.CanStartExamination(examination, out rejectionReason))
                {
                    if (listing.ButtonText("LCStory_StartExam".Translate()))
                    {
                        component.StartExamination(examination);
                    }
                }
                else if (!rejectionReason.NullOrEmpty())
                {
                    listing.Label(rejectionReason);
                }
            }
        }

        private static string FormatTicks(int ticks)
        {
            return ((float)ticks / 2500f).ToString("0.0") + " " + "LCStory_GameHours".Translate();
        }

        private static void DrawDirectivePanel(Rect rect, GameComponent_CompanyDevelopment component)
        {
            CompanyDirectiveDef directive = component.ActiveDirective;
            DirectiveRuntime runtime = component.ActiveDirectiveRuntime;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 28f), "LCStory_DirectivePanel".Translate());
            Text.Font = GameFont.Small;
            if (directive == null || runtime == null)
            {
                Widgets.Label(new Rect(rect.x, rect.y + 32f, rect.width, 50f), "LCStory_NoActiveDirective".Translate());
                DrawDepartmentPriority(new Rect(rect.x, rect.yMax - 55f, rect.width, 52f), component);
                return;
            }
            Widgets.Label(new Rect(rect.x, rect.y + 30f, rect.width, 24f), directive.LabelCap);
            float y = rect.y + 56f;
            foreach (DirectiveObjective objective in directive.objectives)
            {
                Widgets.Label(new Rect(rect.x, y, rect.width, 22f), (objective.IsSatisfied(component, runtime) ? "✓ " : "○ ") + objective.ProgressText(component, runtime));
                y += 22f;
            }
            int remaining = Find.TickManager == null ? 0 : System.Math.Max(0, runtime.deadlineTick - Find.TickManager.TicksGame);
            Widgets.Label(new Rect(rect.x, y, rect.width, 22f), "LCStory_ExamTimeRemaining".Translate(FormatTicks(remaining)));
            DrawDepartmentPriority(new Rect(rect.x, rect.yMax - 55f, rect.width, 52f), component);
        }

        private static void DrawDepartmentPriority(Rect rect, GameComponent_CompanyDevelopment component)
        {
            CompanyDepartmentDef[] candidates = DefDatabase<CompanyDepartmentDef>.AllDefsListForReading
                .Where(component.CanPrioritizeDepartment)
                .OrderBy(def => def.displayOrder)
                .Take(3)
                .ToArray();
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 22f), "LCStory_DepartmentPriority".Translate(
                component.PrioritizedDepartment == null ? "-" : component.PrioritizedDepartment.LabelCap));
            if (candidates.Length == 0)
            {
                return;
            }
            float width = rect.width / candidates.Length;
            for (int i = 0; i < candidates.Length; i++)
            {
                CompanyDepartmentDef candidate = candidates[i];
                if (Widgets.ButtonText(new Rect(rect.x + width * i, rect.y + 25f, width - 4f, 27f), candidate.LabelCap))
                {
                    component.PrioritizeDepartment(candidate);
                }
            }
        }
    }
}
