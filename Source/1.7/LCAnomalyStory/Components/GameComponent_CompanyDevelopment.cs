using System;
using System.Collections.Generic;
using System.Linq;
using LCAnomalyStory.Compatibility;
using LCAnomalyStory.Defs;
using LCAnomalyStory.Examinations;
using RimWorld;
using Verse;

namespace LCAnomalyStory.Components
{
    public enum ExaminationStatus
    {
        Inactive,
        Active,
        Cooldown,
        Passed
    }

    public sealed class ExaminationRuntime : IExposable
    {
        public ExaminationStatus status;
        public bool everPassed;
        public int startTick = -1;
        public int deadlineTick = -1;
        public int retryAvailableTick = -1;
        public int nextWorkerTick = -1;
        public int attempts;
        public string lastOutcomeReason;
        public Dictionary<string, long> statisticBaselines = new Dictionary<string, long>();
        public Dictionary<string, long> longState = new Dictionary<string, long>();
        public Dictionary<string, float> floatState = new Dictionary<string, float>();
        public Dictionary<string, string> stringState = new Dictionary<string, string>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref status, "status", ExaminationStatus.Inactive);
            Scribe_Values.Look(ref everPassed, "everPassed", false);
            Scribe_Values.Look(ref startTick, "startTick", -1);
            Scribe_Values.Look(ref deadlineTick, "deadlineTick", -1);
            Scribe_Values.Look(ref retryAvailableTick, "retryAvailableTick", -1);
            Scribe_Values.Look(ref nextWorkerTick, "nextWorkerTick", -1);
            Scribe_Values.Look(ref attempts, "attempts", 0);
            Scribe_Values.Look(ref lastOutcomeReason, "lastOutcomeReason");
            Scribe_Collections.Look(ref statisticBaselines, "statisticBaselines", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref longState, "longState", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref floatState, "floatState", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref stringState, "stringState", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (status == ExaminationStatus.Passed)
                {
                    everPassed = true;
                }
                EnsureCollections();
            }
        }

        public void ResetAttemptState()
        {
            longState.Clear();
            floatState.Clear();
            stringState.Clear();
            lastOutcomeReason = null;
        }

        public void EnsureCollections()
        {
            statisticBaselines = statisticBaselines ?? new Dictionary<string, long>();
            longState = longState ?? new Dictionary<string, long>();
            floatState = floatState ?? new Dictionary<string, float>();
            stringState = stringState ?? new Dictionary<string, string>();
        }
    }

    public sealed class DevelopmentNodeRuntime : IExposable
    {
        public int activationTick = -1;
        public Dictionary<string, long> statisticBaselines = new Dictionary<string, long>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref activationTick, "activationTick", -1);
            Scribe_Collections.Look(ref statisticBaselines, "statisticBaselines", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && statisticBaselines == null)
            {
                statisticBaselines = new Dictionary<string, long>();
            }
        }
    }

    public sealed class DirectiveRuntime : IExposable
    {
        public string defName;
        public int startTick;
        public int deadlineTick;
        public Dictionary<string, long> statisticBaselines = new Dictionary<string, long>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref defName, "defName");
            Scribe_Values.Look(ref startTick, "startTick", 0);
            Scribe_Values.Look(ref deadlineTick, "deadlineTick", 0);
            Scribe_Collections.Look(ref statisticBaselines, "statisticBaselines", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && statisticBaselines == null)
            {
                statisticBaselines = new Dictionary<string, long>();
            }
        }
    }

    public class GameComponent_CompanyDevelopment : GameComponent
    {
        public const int CurrentDataVersion = 3;
        public const int ExaminationFrameworkApiVersion = CompanyExaminationDef.FrameworkApiVersion;
        public const string StatisticValidWorks = "validWorks";
        public const string StatisticGoodWorks = "goodWorks";
        public const string StatisticPeBoxProduced = "peBoxProduced";
        public const string StatisticEgoExtracted = "egoExtracted";
        public const string StatisticExaminationsPassed = "examinationsPassed";
        public const string StatisticMeltdownWaves = "meltdownWaves";
        public const string StatisticMeltdownsResolved = "meltdownsResolved";
        public const string StatisticMeltdownsBreached = "meltdownsBreached";
        public const string StatisticAbnormalitiesExtracted = "abnormalitiesExtracted";

        private int dataVersion = CurrentDataVersion;
        private int nextEvaluationTick;
        private HashSet<string> completedNodes = new HashSet<string>();
        private HashSet<string> appliedRewards = new HashSet<string>();
        private HashSet<string> sentCompletionLetters = new HashSet<string>();
        private HashSet<string> openDepartments = new HashSet<string>();
        private Dictionary<string, long> statistics = new Dictionary<string, long>();
        private Dictionary<string, DevelopmentNodeRuntime> nodeRuntime = new Dictionary<string, DevelopmentNodeRuntime>();
        private Dictionary<string, ExaminationRuntime> examinations = new Dictionary<string, ExaminationRuntime>();
        private CompanyExaminationDef activeExaminationCache;
        private DirectiveRuntime activeDirective;
        private int nextDirectiveTick;
        private string prioritizedDepartment;
        private bool evaluating;

        public GameComponent_CompanyDevelopment(Game game)
        {
        }

        public int CurrentPhase
        {
            get
            {
                return DefDatabase<CompanyDevelopmentDef>.AllDefsListForReading
                    .Where(IsCompleted)
                    .Select(def => def.phase)
                    .DefaultIfEmpty(0)
                    .Max();
            }
        }

        public IEnumerable<string> OpenDepartments
        {
            get { return openDepartments; }
        }

        public CompanyExaminationDef ActiveExamination
        {
            get
            {
                ExaminationRuntime cachedRuntime;
                if (activeExaminationCache != null
                    && examinations.TryGetValue(activeExaminationCache.defName, out cachedRuntime)
                    && cachedRuntime != null
                    && cachedRuntime.status == ExaminationStatus.Active)
                {
                    return activeExaminationCache;
                }

                activeExaminationCache = DefDatabase<CompanyExaminationDef>.AllDefsListForReading
                    .FirstOrDefault(def =>
                    {
                        ExaminationRuntime runtime;
                        return examinations.TryGetValue(def.defName, out runtime)
                            && runtime != null
                            && runtime.status == ExaminationStatus.Active;
                    });
                return activeExaminationCache;
            }
        }

        public ExaminationRuntime ActiveExaminationRuntime
        {
            get
            {
                CompanyExaminationDef examination = ActiveExamination;
                return examination == null ? null : GetExaminationRuntime(examination);
            }
        }

        public CompanyDirectiveDef ActiveDirective
        {
            get
            {
                return activeDirective == null || activeDirective.defName.NullOrEmpty()
                    ? null
                    : DefDatabase<CompanyDirectiveDef>.GetNamedSilentFail(activeDirective.defName);
            }
        }

        public DirectiveRuntime ActiveDirectiveRuntime
        {
            get { return activeDirective; }
        }

        public CompanyDepartmentDef PrioritizedDepartment
        {
            get { return prioritizedDepartment.NullOrEmpty() ? null : DefDatabase<CompanyDepartmentDef>.GetNamedSilentFail(prioritizedDepartment); }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref dataVersion, "dataVersion", 0);
            Scribe_Values.Look(ref nextEvaluationTick, "nextEvaluationTick", 0);
            Scribe_Collections.Look(ref completedNodes, "completedNodes", LookMode.Value);
            Scribe_Collections.Look(ref appliedRewards, "appliedRewards", LookMode.Value);
            Scribe_Collections.Look(ref sentCompletionLetters, "sentCompletionLetters", LookMode.Value);
            Scribe_Collections.Look(ref openDepartments, "openDepartments", LookMode.Value);
            Scribe_Collections.Look(ref statistics, "statistics", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref nodeRuntime, "nodeRuntime", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look(ref examinations, "examinations", LookMode.Value, LookMode.Deep);
            Scribe_Deep.Look(ref activeDirective, "activeDirective");
            Scribe_Values.Look(ref nextDirectiveTick, "nextDirectiveTick", 0);
            Scribe_Values.Look(ref prioritizedDepartment, "prioritizedDepartment");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                EnsureCollections();
            }
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            InitializeRuntime(false);
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
            InitializeRuntime(true);
        }

        public override void GameComponentTick()
        {
            TickActiveExamination();
            if (Find.TickManager.TicksGame < nextEvaluationTick)
            {
                return;
            }
            nextEvaluationTick = Find.TickManager.TicksGame + 250;
            EvaluateAvailableNodes();
            EvaluateDirective();
        }

        public bool IsCompleted(CompanyDevelopmentDef node)
        {
            return node != null && completedNodes.Contains(node.defName);
        }

        public bool IsAvailable(CompanyDevelopmentDef node)
        {
            return node != null && !node.hidden && !IsCompleted(node) && node.PrerequisitesCompleted(this);
        }

        public bool CanComplete(CompanyDevelopmentDef node)
        {
            return IsAvailable(node) && node.ConditionsSatisfied(this);
        }

        public bool TryCompleteNode(CompanyDevelopmentDef node, bool force = false)
        {
            if (node == null || IsCompleted(node))
            {
                return false;
            }
            if (!force && !CanComplete(node))
            {
                return false;
            }

            completedNodes.Add(node.defName);
            ApplyRewards(node);
            SendCompletionLetter(node);
            ActivateAvailableNodes();
            return true;
        }

        public void RecordStatistic(string statistic, long amount)
        {
            if (statistic.NullOrEmpty() || amount <= 0)
            {
                return;
            }
            statistics[statistic] = GetStatistic(statistic) + amount;
            EvaluateAvailableNodes();
        }

        public long GetStatistic(string statistic)
        {
            long value;
            return !statistic.NullOrEmpty() && statistics.TryGetValue(statistic, out value) ? value : 0L;
        }

        public long GetActivationBaseline(CompanyDevelopmentDef node, string statistic)
        {
            DevelopmentNodeRuntime runtime = EnsureNodeRuntime(node);
            long value;
            return runtime.statisticBaselines.TryGetValue(statistic, out value) ? value : 0L;
        }

        public bool IsDepartmentOpen(CompanyDepartmentDef department)
        {
            return department != null && openDepartments.Contains(department.defName);
        }

        public bool OpenDepartment(CompanyDepartmentDef department)
        {
            if (department == null)
            {
                return false;
            }
            openDepartments.Add(department.defName);
            return true;
        }

        public bool IsDepartmentPrioritized(CompanyDepartmentDef department)
        {
            return department != null && prioritizedDepartment == department.defName;
        }

        public bool CanPrioritizeDepartment(CompanyDepartmentDef department)
        {
            return department != null
                && !IsDepartmentOpen(department)
                && department.minimumPhase <= CurrentPhase + 1;
        }

        public bool PrioritizeDepartment(CompanyDepartmentDef department)
        {
            if (!CanPrioritizeDepartment(department))
            {
                return false;
            }
            prioritizedDepartment = department.defName;
            EvaluateAvailableNodes();
            return true;
        }

        public bool IsExaminationPassed(CompanyExaminationDef examination)
        {
            if (examination == null)
            {
                return false;
            }
            ExaminationRuntime runtime = GetExaminationRuntime(examination);
            return runtime.everPassed || runtime.status == ExaminationStatus.Passed;
        }

        public bool IsExaminationNodePrerequisitesMet(CompanyExaminationDef examination)
        {
            if (examination == null)
            {
                return false;
            }
            foreach (CompanyDevelopmentDef node in DefDatabase<CompanyDevelopmentDef>.AllDefsListForReading)
            {
                if (node.conditions != null
                    && node.conditions.OfType<Conditions.DevelopmentCondition_ExaminationPassed>().Any(condition => condition.examination == examination)
                    && (IsCompleted(node) || node.PrerequisitesCompleted(this)))
                {
                    return true;
                }
            }
            return false;
        }

        public bool CanStartExamination(CompanyExaminationDef examination)
        {
            string unused;
            return CanStartExamination(examination, out unused);
        }

        public bool CanStartExamination(CompanyExaminationDef examination, out string rejectionReason)
        {
            return CanStartExaminationInternal(examination, allowRepeatable: false, out rejectionReason);
        }

        public bool CanStartRepeatableExamination(CompanyExaminationDef examination, out string rejectionReason)
        {
            return CanStartExaminationInternal(examination, allowRepeatable: true, out rejectionReason);
        }

        private bool CanStartExaminationInternal(CompanyExaminationDef examination, bool allowRepeatable, out string rejectionReason)
        {
            rejectionReason = null;
            if (examination == null)
            {
                rejectionReason = "LCStory_ExamStartUnavailable".Translate();
                return false;
            }
            if (ActiveExamination != null)
            {
                rejectionReason = "LCStory_ExamStartAnotherActive".Translate();
                return false;
            }
            ExaminationRuntime runtime = GetExaminationRuntime(examination);
            if (!allowRepeatable && (runtime.everPassed || runtime.status == ExaminationStatus.Passed))
            {
                rejectionReason = "LCStory_ExamStartAlreadyPassed".Translate();
                return false;
            }
            if (Find.TickManager == null)
            {
                rejectionReason = "LCStory_ExamStartUnavailable".Translate();
                return false;
            }
            if (Find.TickManager.TicksGame < runtime.retryAvailableTick)
            {
                rejectionReason = "LCStory_ExamStartCooldown".Translate();
                return false;
            }

            ExaminationContext context = GetExaminationContext(examination);
            try
            {
                return examination.Worker.CanStart(context, out rejectionReason);
            }
            catch (Exception exception)
            {
                rejectionReason = "LCStory_ExamStartWorkerError".Translate();
                Log.Error("[LC Anomaly Story] Examination worker CanStart failed for " + examination.defName + ": " + exception);
                return false;
            }
        }

        public bool StartExamination(CompanyExaminationDef examination)
        {
            return StartExaminationInternal(examination, allowRepeatable: false);
        }

        public bool StartRepeatableExamination(CompanyExaminationDef examination)
        {
            return StartExaminationInternal(examination, allowRepeatable: true);
        }

        private bool StartExaminationInternal(CompanyExaminationDef examination, bool allowRepeatable)
        {
            string rejectionReason;
            if (!CanStartExaminationInternal(examination, allowRepeatable, out rejectionReason))
            {
                return false;
            }
            ExaminationRuntime runtime = GetExaminationRuntime(examination);
            int now = Find.TickManager.TicksGame;
            runtime.status = ExaminationStatus.Active;
            activeExaminationCache = examination;
            runtime.startTick = now;
            runtime.deadlineTick = examination.HasDeadline ? now + examination.durationTicks : -1;
            runtime.retryAvailableTick = -1;
            runtime.nextWorkerTick = now;
            runtime.attempts++;
            runtime.statisticBaselines = new Dictionary<string, long>(statistics);
            runtime.ResetAttemptState();

            ExaminationContext context = GetExaminationContext(examination);
            try
            {
                examination.Worker.OnStarted(context);
            }
            catch (Exception exception)
            {
                Log.Error("[LC Anomaly Story] Examination worker OnStarted failed for " + examination.defName + ": " + exception);
                if (runtime.status == ExaminationStatus.Active)
                {
                    runtime.status = runtime.everPassed ? ExaminationStatus.Passed : ExaminationStatus.Inactive;
                    activeExaminationCache = null;
                    runtime.nextWorkerTick = -1;
                    runtime.lastOutcomeReason = "LCStory_ExamStartWorkerError".Translate();
                    TryNotifyCancelled(examination, context);
                    runtime.startTick = -1;
                    runtime.deadlineTick = -1;
                }
                return runtime.status == ExaminationStatus.Active;
            }

            if (runtime.status != ExaminationStatus.Active)
            {
                return true;
            }

            Find.LetterStack.ReceiveLetter(
                "LCStory_ExamStartedTitle".Translate(examination.LabelCap),
                examination.description,
                LetterDefOf.NeutralEvent);
            return true;
        }

        public bool PassExamination(CompanyExaminationDef examination, string reason = null)
        {
            if (!IsActiveExamination(examination))
            {
                return false;
            }

            ExaminationRuntime runtime = GetExaminationRuntime(examination);
            runtime.status = ExaminationStatus.Passed;
            runtime.everPassed = true;
            activeExaminationCache = null;
            runtime.retryAvailableTick = -1;
            runtime.nextWorkerTick = -1;
            runtime.lastOutcomeReason = reason;
            ExaminationContext context = GetExaminationContext(examination);
            TryInvokeLifecycle(examination, "OnPassed", () => examination.Worker.OnPassed(context));
            RecordStatistic("examinationsPassed", 1L);
            Find.LetterStack.ReceiveLetter(
                "LCStory_ExamPassedTitle".Translate(examination.LabelCap),
                WithOutcomeReason("LCStory_ExamPassedText".Translate(examination.LabelCap), reason),
                LetterDefOf.PositiveEvent);
            return true;
        }

        public bool FailExamination(CompanyExaminationDef examination, string reason = null)
        {
            if (!IsActiveExamination(examination))
            {
                return false;
            }

            ExaminationRuntime runtime = GetExaminationRuntime(examination);
            int now = Find.TickManager == null ? 0 : Find.TickManager.TicksGame;
            runtime.status = ExaminationStatus.Cooldown;
            activeExaminationCache = null;
            runtime.retryAvailableTick = now + examination.retryCooldownTicks;
            runtime.nextWorkerTick = -1;
            runtime.lastOutcomeReason = reason;
            ExaminationContext context = GetExaminationContext(examination);
            TryInvokeLifecycle(examination, "OnFailed", () => examination.Worker.OnFailed(context));
            Find.LetterStack.ReceiveLetter(
                "LCStory_ExamFailedTitle".Translate(examination.LabelCap),
                WithOutcomeReason("LCStory_ExamFailedText".Translate(examination.LabelCap), reason),
                LetterDefOf.NegativeEvent);
            return true;
        }

        public bool CancelExamination(CompanyExaminationDef examination, string reason = null)
        {
            if (!IsActiveExamination(examination))
            {
                return false;
            }

            ExaminationRuntime runtime = GetExaminationRuntime(examination);
            runtime.status = ExaminationStatus.Inactive;
            activeExaminationCache = null;
            runtime.startTick = -1;
            runtime.deadlineTick = -1;
            runtime.retryAvailableTick = -1;
            runtime.nextWorkerTick = -1;
            runtime.lastOutcomeReason = reason;
            ExaminationContext context = GetExaminationContext(examination);
            TryNotifyCancelled(examination, context);
            return true;
        }

        public ExaminationRuntime GetExaminationRuntime(CompanyExaminationDef examination)
        {
            if (examination == null)
            {
                throw new ArgumentNullException(nameof(examination));
            }
            ExaminationRuntime runtime;
            if (examinations.TryGetValue(examination.defName, out runtime) && runtime != null)
            {
                runtime.EnsureCollections();
                return runtime;
            }
            runtime = new ExaminationRuntime();
            examinations[examination.defName] = runtime;
            return runtime;
        }

        public ExaminationContext GetExaminationContext(CompanyExaminationDef examination)
        {
            return new ExaminationContext(this, examination, GetExaminationRuntime(examination));
        }

        public bool TryGetActiveExaminationContext(out ExaminationContext context)
        {
            CompanyExaminationDef examination = ActiveExamination;
            context = examination == null ? null : GetExaminationContext(examination);
            return context != null;
        }

        public void ModifyActiveExaminationStudySuccessRate(ref float value)
        {
            ExaminationContext context;
            if (!TryGetActiveExaminationContext(out context))
            {
                return;
            }
            try
            {
                context.Definition.Worker.ModifyStudySuccessRate(context, ref value);
            }
            catch (Exception exception)
            {
                Log.ErrorOnce(
                    "[LC Anomaly Story] Examination worker ModifyStudySuccessRate failed for "
                    + context.Definition.defName + ": " + exception,
                    (context.Definition.defName + ":ModifyStudySuccessRate").GetHashCode());
            }
        }

        public IEnumerable<string> GetExaminationProgressLines(CompanyExaminationDef examination)
        {
            if (examination == null)
            {
                return Enumerable.Empty<string>();
            }
            try
            {
                return examination.Worker.ProgressLines(GetExaminationContext(examination)).ToList();
            }
            catch (Exception exception)
            {
                Log.ErrorOnce(
                    "[LC Anomaly Story] Examination worker ProgressLines failed for "
                    + examination.defName + ": " + exception,
                    (examination.defName + ":ProgressLines").GetHashCode());
                return new[] { "LCStory_ExamProgressWorkerError".Translate().ToString() };
            }
        }

        public string GetExaminationStateLabel(CompanyExaminationDef examination)
        {
            ExaminationRuntime runtime = GetExaminationRuntime(examination);
            switch (runtime.status)
            {
                case ExaminationStatus.Active:
                    return "LCStory_ExamState_Active".Translate();
                case ExaminationStatus.Cooldown:
                    return Find.TickManager != null && Find.TickManager.TicksGame >= runtime.retryAvailableTick
                        ? "LCStory_ExamState_Ready".Translate()
                        : "LCStory_ExamState_Cooldown".Translate();
                case ExaminationStatus.Passed:
                    return "LCStory_ExamState_Passed".Translate();
                default:
                    return "LCStory_ExamState_Ready".Translate();
            }
        }

        public int ExaminationTicksRemaining(CompanyExaminationDef examination)
        {
            ExaminationRuntime runtime = GetExaminationRuntime(examination);
            return runtime.status == ExaminationStatus.Active && runtime.deadlineTick >= 0 && Find.TickManager != null
                ? Math.Max(0, runtime.deadlineTick - Find.TickManager.TicksGame)
                : -1;
        }

        public int ExaminationCooldownRemaining(CompanyExaminationDef examination)
        {
            ExaminationRuntime runtime = GetExaminationRuntime(examination);
            return runtime.status == ExaminationStatus.Cooldown && Find.TickManager != null
                ? Math.Max(0, runtime.retryAvailableTick - Find.TickManager.TicksGame)
                : 0;
        }

        public void EvaluateAvailableNodes()
        {
            if (evaluating || Current.Game == null)
            {
                return;
            }

            evaluating = true;
            try
            {
                ActivateAvailableNodes();
                bool completedAny;
                do
                {
                    completedAny = false;
                    foreach (CompanyDevelopmentDef node in OrderedNodes())
                    {
                        if (IsAvailable(node) && !node.manualCompletion && node.ConditionsSatisfied(this))
                        {
                            completedAny |= TryCompleteNode(node);
                        }
                    }
                }
                while (completedAny);

                ApplyMissingRewards();
            }
            finally
            {
                evaluating = false;
            }
        }

        public void ResetForDebug()
        {
            CompanyExaminationDef activeExamination = ActiveExamination;
            if (activeExamination != null)
            {
                CancelExamination(activeExamination, "debug reset");
            }
            completedNodes.Clear();
            appliedRewards.Clear();
            sentCompletionLetters.Clear();
            openDepartments.Clear();
            statistics.Clear();
            nodeRuntime.Clear();
            examinations.Clear();
            activeExaminationCache = null;
            activeDirective = null;
            nextDirectiveTick = 0;
            prioritizedDepartment = null;
            InitializeRuntime(false);
        }

        public string DiagnosticSummary()
        {
            return "phase=" + CurrentPhase
                + ", completed=" + completedNodes.Count
                + ", available=" + OrderedNodes().Count(IsAvailable)
                + ", works=" + GetStatistic(StatisticValidWorks)
                + ", peBox=" + GetStatistic(StatisticPeBoxProduced)
                + ", directive=" + (ActiveDirective == null ? "none" : ActiveDirective.defName);
        }

        private void InitializeRuntime(bool notifyLoadedExamination)
        {
            EnsureCollections();
            MigrateIfNeeded();
            activeExaminationCache = null;
            ManagedResearchUtility.StopManagedCurrentProject();
            ActivateAvailableNodes();
            ApplyMissingRewards();
            nextEvaluationTick = Find.TickManager == null ? 0 : Find.TickManager.TicksGame + 60;
            if (nextDirectiveTick <= 0 && Find.TickManager != null)
            {
                nextDirectiveTick = Find.TickManager.TicksGame + 2500;
            }
            if (notifyLoadedExamination)
            {
                NotifyActiveExaminationLoaded();
            }
        }

        private void EnsureCollections()
        {
            completedNodes = completedNodes ?? new HashSet<string>();
            appliedRewards = appliedRewards ?? new HashSet<string>();
            sentCompletionLetters = sentCompletionLetters ?? new HashSet<string>();
            openDepartments = openDepartments ?? new HashSet<string>();
            statistics = statistics ?? new Dictionary<string, long>();
            nodeRuntime = nodeRuntime ?? new Dictionary<string, DevelopmentNodeRuntime>();
            examinations = examinations ?? new Dictionary<string, ExaminationRuntime>();
        }

        private void MigrateIfNeeded()
        {
            if (dataVersion < 1)
            {
                dataVersion = 1;
            }
            if (dataVersion < 2)
            {
                examinations = examinations ?? new Dictionary<string, ExaminationRuntime>();
                if (nextDirectiveTick < 0)
                {
                    nextDirectiveTick = 0;
                }
                dataVersion = 2;
            }
            if (dataVersion < 3)
            {
                foreach (ExaminationRuntime runtime in examinations.Values.Where(runtime => runtime != null))
                {
                    runtime.EnsureCollections();
                }
                dataVersion = 3;
            }
            dataVersion = CurrentDataVersion;
        }

        private IEnumerable<CompanyDevelopmentDef> OrderedNodes()
        {
            return DefDatabase<CompanyDevelopmentDef>.AllDefsListForReading
                .OrderBy(def => def.phase)
                .ThenBy(def => def.displayOrder)
                .ThenBy(def => def.defName);
        }

        private void ActivateAvailableNodes()
        {
            foreach (CompanyDevelopmentDef node in OrderedNodes())
            {
                if (IsAvailable(node) && !nodeRuntime.ContainsKey(node.defName))
                {
                    EnsureNodeRuntime(node);
                }
            }
        }

        private DevelopmentNodeRuntime EnsureNodeRuntime(CompanyDevelopmentDef node)
        {
            DevelopmentNodeRuntime runtime;
            if (nodeRuntime.TryGetValue(node.defName, out runtime) && runtime != null)
            {
                return runtime;
            }

            runtime = new DevelopmentNodeRuntime
            {
                activationTick = Find.TickManager == null ? 0 : Find.TickManager.TicksGame,
                statisticBaselines = new Dictionary<string, long>(statistics)
            };
            nodeRuntime[node.defName] = runtime;
            return runtime;
        }

        private void ApplyRewards(CompanyDevelopmentDef node)
        {
            if (node.rewards == null)
            {
                return;
            }
            for (int i = 0; i < node.rewards.Count; i++)
            {
                string rewardId = node.defName + ":" + i;
                if (appliedRewards.Contains(rewardId))
                {
                    continue;
                }
                try
                {
                    if (node.rewards[i] != null && node.rewards[i].TryApply(this, node))
                    {
                        appliedRewards.Add(rewardId);
                    }
                    else
                    {
                        Log.Error("[LC Anomaly Story] Failed to apply reward " + rewardId + "; it will be retried.");
                    }
                }
                catch (Exception exception)
                {
                    Log.Error("[LC Anomaly Story] Reward " + rewardId + " threw an exception and will be retried: " + exception);
                }
            }
        }

        private void TickActiveExamination()
        {
            CompanyExaminationDef examination = ActiveExamination;
            if (examination == null || Find.TickManager == null)
            {
                return;
            }

            int now = Find.TickManager == null ? 0 : Find.TickManager.TicksGame;
            ExaminationRuntime runtime = GetExaminationRuntime(examination);
            if (runtime.nextWorkerTick > now)
            {
                return;
            }
            runtime.nextWorkerTick = now + Math.Max(1, examination.workerTickInterval);

            ExaminationContext context = GetExaminationContext(examination);
            try
            {
                examination.Worker.Tick(context);
                if (!context.IsActive)
                {
                    return;
                }
                ExaminationOutcome outcome = examination.Worker.Evaluate(context);
                if (outcome == ExaminationOutcome.Passed)
                {
                    PassExamination(examination);
                }
                else if (outcome == ExaminationOutcome.Failed)
                {
                    FailExamination(examination);
                }
            }
            catch (Exception exception)
            {
                Log.Error("[LC Anomaly Story] Examination worker tick failed for " + examination.defName + ": " + exception);
                FailExamination(examination, "LCStory_ExamWorkerFailure".Translate());
            }
        }

        private bool IsActiveExamination(CompanyExaminationDef examination)
        {
            return examination != null && GetExaminationRuntime(examination).status == ExaminationStatus.Active;
        }

        private void NotifyActiveExaminationLoaded()
        {
            CompanyExaminationDef examination = ActiveExamination;
            if (examination == null)
            {
                return;
            }
            ExaminationRuntime runtime = GetExaminationRuntime(examination);
            runtime.nextWorkerTick = Find.TickManager == null ? 0 : Find.TickManager.TicksGame;
            ExaminationContext context = GetExaminationContext(examination);
            TryInvokeLifecycle(examination, "OnLoaded", () => examination.Worker.OnLoaded(context));
        }

        private void TryNotifyCancelled(CompanyExaminationDef examination, ExaminationContext context)
        {
            TryInvokeLifecycle(examination, "OnCancelled", () => examination.Worker.OnCancelled(context));
        }

        private static void TryInvokeLifecycle(CompanyExaminationDef examination, string lifecycle, Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Log.Error("[LC Anomaly Story] Examination worker " + lifecycle + " failed for " + examination.defName + ": " + exception);
            }
        }

        private static TaggedString WithOutcomeReason(TaggedString baseText, string reason)
        {
            return reason.NullOrEmpty() ? baseText : baseText + "\n\n" + reason;
        }

        private void EvaluateDirective()
        {
            int now = Find.TickManager == null ? 0 : Find.TickManager.TicksGame;
            CompanyDirectiveDef directive = ActiveDirective;
            if (activeDirective != null && directive == null)
            {
                Log.Warning("[LC Anomaly Story] Active directive Def is missing: " + activeDirective.defName);
                activeDirective = null;
                nextDirectiveTick = now + 2500;
                return;
            }

            if (directive != null)
            {
                if (directive.objectives.All(objective => objective.IsSatisfied(this, activeDirective)))
                {
                    RecordStatistic("directivesCompleted", 1L);
                    if (directive.department != null && directive.departmentExperience > 0)
                    {
                        RecordStatistic("departmentExperience." + directive.department.defName, directive.departmentExperience);
                    }
                    Messages.Message("LCStory_DirectiveCompleted".Translate(directive.LabelCap), MessageTypeDefOf.TaskCompletion, false);
                    activeDirective = null;
                    nextDirectiveTick = now + directive.cooldownTicks;
                }
                else if (now >= activeDirective.deadlineTick)
                {
                    Messages.Message("LCStory_DirectiveFailed".Translate(directive.LabelCap), MessageTypeDefOf.NegativeEvent, false);
                    activeDirective = null;
                    nextDirectiveTick = now + directive.cooldownTicks;
                }
                return;
            }

            if (CurrentPhase < 1 || now < nextDirectiveTick)
            {
                return;
            }

            List<CompanyDirectiveDef> candidates = DefDatabase<CompanyDirectiveDef>.AllDefsListForReading
                .Where(def => def.minimumPhase <= CurrentPhase && (def.department == null || IsDepartmentOpen(def.department)))
                .ToList();
            if (candidates.Count == 0)
            {
                nextDirectiveTick = now + 2500;
                return;
            }

            CompanyDirectiveDef selected = candidates.RandomElementByWeight(def => def.selectionWeight);
            activeDirective = new DirectiveRuntime
            {
                defName = selected.defName,
                startTick = now,
                deadlineTick = now + selected.durationTicks,
                statisticBaselines = new Dictionary<string, long>(statistics)
            };
            Messages.Message("LCStory_DirectiveReceived".Translate(selected.LabelCap), MessageTypeDefOf.NeutralEvent, false);
        }

        private void ApplyMissingRewards()
        {
            foreach (CompanyDevelopmentDef node in OrderedNodes().Where(IsCompleted))
            {
                ApplyRewards(node);
            }
        }

        private void SendCompletionLetter(CompanyDevelopmentDef node)
        {
            if (sentCompletionLetters.Contains(node.defName))
            {
                return;
            }
            sentCompletionLetters.Add(node.defName);

            TaggedString title = node.completionLetterLabel.NullOrEmpty()
                ? "LCStory_NodeCompletedTitle".Translate(node.LabelCap)
                : node.completionLetterLabel.Translate();
            TaggedString text = node.completionLetterText.NullOrEmpty()
                ? "LCStory_NodeCompletedText".Translate(node.LabelCap)
                : node.completionLetterText.Translate();
            Find.LetterStack.ReceiveLetter(title, text, node.completionLetterDef ?? LetterDefOf.PositiveEvent);
        }
    }
}
