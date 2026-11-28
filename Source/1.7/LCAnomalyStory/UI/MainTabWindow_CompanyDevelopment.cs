using System.Linq;
using LCAnomalyStory.Conditions;
using LCAnomalyStory.Components;
using LCAnomalyStory.Defs;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace LCAnomalyStory.UI
{
    [StaticConstructorOnStartup]
    public class MainTabWindow_CompanyDevelopment : MainTabWindow
    {
        private static readonly Texture2D TexWindowBg = ContentFinder<Texture2D>.Get("UI/Codex/Codex_WindowBg", false);
        private static readonly Texture2D TexHeaderBanner = ContentFinder<Texture2D>.Get("UI/Codex/Codex_HeaderBanner", false);
        private static readonly Texture2D TexPanelFrame = ContentFinder<Texture2D>.Get("UI/Codex/Codex_PanelFrame", false);
        private static readonly Texture2D TexSectionBar = ContentFinder<Texture2D>.Get("UI/Codex/Codex_SectionBar", false);
        private static readonly Texture2D TexTabNormal = ContentFinder<Texture2D>.Get("UI/Codex/Codex_TabNormal", false);
        private static readonly Texture2D TexTabActive = ContentFinder<Texture2D>.Get("UI/Codex/Codex_TabActive", false);
        private static readonly Texture2D TexHazardDivider = ContentFinder<Texture2D>.Get("UI/Codex/Codex_HazardDivider", false);

        private static readonly Color PanelBgFallback = new Color(0.06f, 0.07f, 0.09f, 0.94f);
        private static readonly Color GoldAccentColor = new Color(0.95f, 0.84f, 0.44f, 1f);
        private static readonly Color CrimsonAccentColor = new Color(0.90f, 0.22f, 0.22f, 1f);
        private static readonly Color BorderSubtleColor = new Color(0.35f, 0.32f, 0.24f, 0.72f);

        private Vector2 nodeScrollPosition;
        private Vector2 detailScrollPosition;
        private CompanyDevelopmentDef selectedNode;

        public override Vector2 InitialSize
        {
            get { return new Vector2(1020f, 720f); }
        }

        protected override float Margin => 0f;

        public MainTabWindow_CompanyDevelopment()
        {
            doWindowBackground = false;
            drawShadow = true;
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
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Color oldColor = GUI.color;

            try
            {
                if (TexWindowBg != null)
                {
                    GUI.DrawTexture(inRect, TexWindowBg, ScaleMode.StretchToFill);
                }
                else
                {
                    Widgets.DrawBoxSolidWithOutline(inRect, new Color(0.05f, 0.06f, 0.08f, 0.98f), GoldAccentColor);
                }

                Rect workRect = inRect.ContractedBy(14f);
                GameComponent_CompanyDevelopment component = StoryComponents.Development;
                if (component == null)
                {
                    DrawLobCorpPanel(workRect);
                    Text.Anchor = TextAnchor.MiddleCenter;
                    GUI.color = ColoredText.SubtleGrayColor;
                    Widgets.Label(workRect, "LCStory_NoGameComponent".Translate());
                    return;
                }

                Rect headerRect = new Rect(workRect.x, workRect.y, workRect.width, 46f);
                if (TexHeaderBanner != null)
                {
                    GUI.DrawTexture(headerRect, TexHeaderBanner, ScaleMode.StretchToFill);
                }
                else
                {
                    Widgets.DrawBoxSolidWithOutline(headerRect, new Color(0.08f, 0.10f, 0.13f, 0.96f), CrimsonAccentColor);
                }

                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = GoldAccentColor;
                Widgets.Label(new Rect(headerRect.x + 16f, headerRect.y + 4f, headerRect.width - 32f, 38f), "LCStory_WindowTitle".Translate(component.CurrentPhase));
                GUI.color = Color.white;
                Text.Font = GameFont.Small;

                if (TexHazardDivider != null)
                {
                    GUI.DrawTexture(new Rect(workRect.x, headerRect.yMax + 3f, workRect.width, 6f), TexHazardDivider, ScaleMode.StretchToFill);
                }

                Rect body = new Rect(workRect.x, headerRect.yMax + 12f, workRect.width, workRect.yMax - (headerRect.yMax + 12f));
                Rect left = new Rect(body.x, body.y, 312f, body.height);
                Rect right = new Rect(left.xMax + 12f, body.y, body.width - left.width - 12f, body.height);

                DrawLobCorpPanel(left);
                DrawNodeList(left.ContractedBy(10f), component);

                Rect directiveRect = new Rect(right.x, right.y, right.width, 212f);
                Rect detailRect = new Rect(right.x, directiveRect.yMax + 10f, right.width, right.height - directiveRect.height - 10f);
                DrawLobCorpPanel(directiveRect);
                DrawLobCorpPanel(detailRect);
                DrawDirectivePanel(directiveRect.ContractedBy(12f), component);
                DrawNodeDetails(detailRect.ContractedBy(12f), component);
            }
            finally
            {
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                GUI.color = oldColor;
            }
        }

        private static void DrawLobCorpPanel(Rect rect)
        {
            if (TexPanelFrame != null)
            {
                GUI.DrawTexture(rect, TexPanelFrame, ScaleMode.StretchToFill);
            }
            else
            {
                Widgets.DrawBoxSolidWithOutline(rect, PanelBgFallback, BorderSubtleColor);
            }
        }

        private static void DrawSectionBar(Rect rect, string label)
        {
            if (TexSectionBar != null)
            {
                GUI.DrawTexture(rect, TexSectionBar, ScaleMode.StretchToFill);
            }
            else
            {
                Widgets.DrawBoxSolid(rect, new Color(0.10f, 0.12f, 0.16f, 0.95f));
            }

            GUI.color = GoldAccentColor;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.LabelFit(new Rect(rect.x + 10f, rect.y, rect.width - 14f, rect.height), label);
            GUI.color = Color.white;
        }

        private static bool DrawLobCorpButton(Rect rect, string label, bool activeLook)
        {
            bool hovered = Mouse.IsOver(rect);
            Texture2D tex = (activeLook || hovered) ? TexTabActive : TexTabNormal;
            if (tex != null)
            {
                GUI.color = hovered && !activeLook ? new Color(1.10f, 1.06f, 1.0f) : Color.white;
                GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill);
            }
            else
            {
                Widgets.DrawBoxSolidWithOutline(
                    rect,
                    activeLook ? new Color(0.22f, 0.10f, 0.12f, 0.95f) : new Color(0.09f, 0.11f, 0.14f, 0.94f),
                    (activeLook || hovered) ? GoldAccentColor : BorderSubtleColor);
            }

            GUI.color = (activeLook || hovered) ? GoldAccentColor : Color.white;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.LabelFit(rect.ContractedBy(6f, 0f), label);
            GUI.color = Color.white;

            if (Widgets.ButtonInvisible(rect))
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
                return true;
            }
            return false;
        }

        private void DrawNodeList(Rect rect, GameComponent_CompanyDevelopment component)
        {
            CompanyDevelopmentDef[] nodes = DefDatabase<CompanyDevelopmentDef>.AllDefsListForReading
                .Where(def => !def.hidden)
                .OrderBy(def => def.phase)
                .ThenBy(def => def.displayOrder)
                .ThenBy(def => def.defName)
                .ToArray();
            Rect view = new Rect(0f, 0f, rect.width - 16f, nodes.Length * 52f + 8f);
            Widgets.BeginScrollView(rect, ref nodeScrollPosition, view);
            float y = 0f;
            foreach (CompanyDevelopmentDef node in nodes)
            {
                bool completed = component.IsCompleted(node);
                bool available = component.IsAvailable(node);
                bool isSelected = selectedNode == node;

                Rect row = new Rect(0f, y, view.width, 46f);
                Color accent = completed
                    ? new Color(0.35f, 0.88f, 0.56f)
                    : (available ? GoldAccentColor : new Color(0.55f, 0.58f, 0.64f));
                Color bg = isSelected
                    ? new Color(0.18f, 0.13f, 0.11f, 0.96f)
                    : (Mouse.IsOver(row) ? new Color(0.11f, 0.13f, 0.17f, 0.94f) : new Color(0.08f, 0.09f, 0.12f, 0.90f));
                Widgets.DrawBoxSolidWithOutline(row, bg, isSelected ? GoldAccentColor : new Color(accent.r * 0.55f, accent.g * 0.55f, accent.b * 0.55f, 0.8f));
                Widgets.DrawBoxSolid(new Rect(row.x + 2f, row.y + 4f, 4f, row.height - 8f), accent);

                string status = completed
                    ? "LCStory_Status_Completed".Translate()
                    : available
                        ? "LCStory_Status_Available".Translate()
                        : "LCStory_Status_Locked".Translate();

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = isSelected ? GoldAccentColor : Color.white;
                Widgets.LabelEllipses(new Rect(row.x + 12f, row.y + 3f, row.width - 18f, 22f), node.LabelCap);

                Text.Font = GameFont.Tiny;
                GUI.color = accent;
                Widgets.LabelFit(new Rect(row.x + 12f, row.y + 24f, row.width - 18f, 18f), status);
                GUI.color = Color.white;

                if (Widgets.ButtonInvisible(row))
                {
                    selectedNode = node;
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                }
                y += 52f;
            }
            Widgets.EndScrollView();
        }

        private void DrawNodeDetails(Rect rect, GameComponent_CompanyDevelopment component)
        {
            if (selectedNode == null)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = ColoredText.SubtleGrayColor;
                Widgets.Label(rect, "LCStory_SelectNode".Translate());
                return;
            }

            Rect view = new Rect(0f, 0f, rect.width - 16f, 800f);
            Widgets.BeginScrollView(rect, ref detailScrollPosition, view);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(view);

            DrawSectionBar(listing.GetRect(28f), selectedNode.LabelCap);
            listing.Gap(4f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = new Color(0.84f, 0.86f, 0.90f);
            listing.Label("LCStory_PhaseDepartmentAndKind".Translate(
                selectedNode.phase,
                selectedNode.department == null ? "-" : selectedNode.department.LabelCap,
                ("LCStory_NodeKind_" + selectedNode.kind).Translate()));
            GUI.color = Color.white;
            listing.GapLine(8f);
            listing.Label(selectedNode.description);
            listing.Gap();

            GUI.color = GoldAccentColor;
            listing.Label("LCStory_Prerequisites".Translate());
            GUI.color = Color.white;
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
            GUI.color = GoldAccentColor;
            listing.Label("LCStory_Conditions".Translate());
            GUI.color = Color.white;
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
            GUI.color = GoldAccentColor;
            listing.Label("LCStory_Rewards".Translate());
            GUI.color = Color.white;
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
                GUI.color = new Color(0.45f, 0.94f, 0.62f);
                listing.Label("LCStory_NodeAlreadyCompleted".Translate());
                GUI.color = Color.white;
            }
            else if (selectedNode.manualCompletion)
            {
                bool canComplete = component.CanComplete(selectedNode);
                if (canComplete && DrawLobCorpButton(listing.GetRect(32f), "LCStory_ApproveNode".Translate(), activeLook: true))
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
            GUI.color = GoldAccentColor;
            listing.Label(examination.LabelCap);
            GUI.color = Color.white;
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
                    if (DrawLobCorpButton(listing.GetRect(30f), "LCStory_StartExam".Translate(), activeLook: true))
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
            DrawSectionBar(new Rect(rect.x, rect.y, rect.width, 26f), "LCStory_DirectivePanel".Translate());
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            if (directive == null || runtime == null)
            {
                GUI.color = ColoredText.SubtleGrayColor;
                Widgets.Label(new Rect(rect.x, rect.y + 32f, rect.width, 50f), "LCStory_NoActiveDirective".Translate());
                GUI.color = Color.white;
                DrawDepartmentPriority(new Rect(rect.x, rect.yMax - 55f, rect.width, 52f), component);
                return;
            }

            GUI.color = GoldAccentColor;
            Widgets.Label(new Rect(rect.x, rect.y + 30f, rect.width, 24f), directive.LabelCap);
            GUI.color = Color.white;
            float y = rect.y + 56f;
            foreach (DirectiveObjective objective in directive.objectives)
            {
                bool done = objective.IsSatisfied(component, runtime);
                GUI.color = done ? new Color(0.45f, 0.94f, 0.62f) : Color.white;
                Widgets.Label(new Rect(rect.x, y, rect.width, 22f), (done ? "✓ " : "○ ") + objective.ProgressText(component, runtime));
                y += 22f;
            }
            GUI.color = Color.white;
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
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.84f, 0.86f, 0.90f);
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 20f), "LCStory_DepartmentPriority".Translate(
                component.PrioritizedDepartment == null ? "-" : component.PrioritizedDepartment.LabelCap));
            GUI.color = Color.white;
            if (candidates.Length == 0)
            {
                return;
            }
            float width = rect.width / candidates.Length;
            for (int i = 0; i < candidates.Length; i++)
            {
                CompanyDepartmentDef candidate = candidates[i];
                bool active = component.PrioritizedDepartment == candidate;
                if (DrawLobCorpButton(new Rect(rect.x + width * i, rect.y + 23f, width - 6f, 28f), candidate.LabelCap, activeLook: active))
                {
                    component.PrioritizeDepartment(candidate);
                }
            }
        }
    }
}
