using System;
using System.Collections.Generic;
using LCAnomalyStory.Components;
using LCAnomalyStory.Examinations;
using Verse;

namespace LCAnomalyStory.Defs
{
    public abstract class ExaminationObjective
    {
        public string labelKey;

        public abstract bool IsSatisfied(GameComponent_CompanyDevelopment component, ExaminationRuntime runtime);

        public abstract string ProgressText(GameComponent_CompanyDevelopment component, ExaminationRuntime runtime);

        public virtual IEnumerable<string> ConfigErrors()
        {
            yield break;
        }

        protected string Label
        {
            get { return (labelKey.NullOrEmpty() ? "LCStory_ExamObjective" : labelKey).Translate(); }
        }
    }

    public class ExaminationObjective_Statistic : ExaminationObjective
    {
        public string statistic;
        public long target;

        public override bool IsSatisfied(GameComponent_CompanyDevelopment component, ExaminationRuntime runtime)
        {
            return Current(component, runtime) >= target;
        }

        public override string ProgressText(GameComponent_CompanyDevelopment component, ExaminationRuntime runtime)
        {
            return Label + ": " + Current(component, runtime) + " / " + target;
        }

        public override IEnumerable<string> ConfigErrors()
        {
            if (statistic.NullOrEmpty())
            {
                yield return "statistic is required";
            }
            if (target < 1)
            {
                yield return "target must be at least 1";
            }
        }

        private long Current(GameComponent_CompanyDevelopment component, ExaminationRuntime runtime)
        {
            long baseline;
            runtime.statisticBaselines.TryGetValue(statistic, out baseline);
            return Math.Max(0L, component.GetStatistic(statistic) - baseline);
        }
    }

    public class CompanyExaminationDef : Def
    {
        public const int FrameworkApiVersion = 1;

        public int durationTicks = 60000;
        public int retryCooldownTicks = 30000;
        public int workerTickInterval = 60;
        public Type workerClass = typeof(CompanyExaminationWorker);
        public List<ExaminationObjective> objectives = new List<ExaminationObjective>();
        public List<ExaminationEffect> effects = new List<ExaminationEffect>();

        private CompanyExaminationWorker worker;

        public CompanyExaminationWorker Worker
        {
            get
            {
                if (worker == null)
                {
                    Type type = workerClass ?? typeof(CompanyExaminationWorker);
                    worker = (CompanyExaminationWorker)Activator.CreateInstance(type);
                    worker.Initialize(this);
                }
                return worker;
            }
        }

        public bool HasDeadline
        {
            get { return durationTicks >= 0; }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }
            if (durationTicks != -1 && durationTicks < 60)
            {
                yield return "durationTicks must be -1 (worker-controlled) or at least 60";
            }
            if (retryCooldownTicks < 0)
            {
                yield return "retryCooldownTicks must not be negative";
            }
            if (workerTickInterval < 1)
            {
                yield return "workerTickInterval must be at least 1";
            }
            if (workerClass == null)
            {
                yield return "workerClass is required";
            }
            else if (!typeof(CompanyExaminationWorker).IsAssignableFrom(workerClass))
            {
                yield return "workerClass must derive from CompanyExaminationWorker";
            }
            else if (workerClass.IsAbstract)
            {
                yield return "workerClass must not be abstract";
            }
            else if (workerClass.GetConstructor(Type.EmptyTypes) == null)
            {
                yield return "workerClass must have a public parameterless constructor";
            }

            if ((objectives == null || objectives.Count == 0) && workerClass == typeof(CompanyExaminationWorker))
            {
                yield return "the default worker requires at least one objective";
            }
            if (objectives != null)
            {
                foreach (ExaminationObjective objective in objectives)
                {
                    if (objective == null)
                    {
                        yield return "contains a null objective";
                        continue;
                    }
                    foreach (string error in objective.ConfigErrors())
                    {
                        yield return "objective: " + error;
                    }
                }
            }
            if (effects != null)
            {
                foreach (ExaminationEffect effect in effects)
                {
                    if (effect == null)
                    {
                        yield return "contains a null effect";
                        continue;
                    }
                    foreach (string error in effect.ConfigErrors())
                    {
                        yield return "effect: " + error;
                    }
                }
            }

            if (workerClass != null
                && typeof(CompanyExaminationWorker).IsAssignableFrom(workerClass)
                && !workerClass.IsAbstract
                && workerClass.GetConstructor(Type.EmptyTypes) != null)
            {
                CompanyExaminationWorker configuredWorker = null;
                string workerConstructionError = null;
                try
                {
                    configuredWorker = Worker;
                }
                catch (Exception exception)
                {
                    workerConstructionError = "workerClass could not be constructed: " + exception.GetType().Name + ": " + exception.Message;
                }
                if (workerConstructionError != null)
                {
                    yield return workerConstructionError;
                }
                if (configuredWorker != null)
                {
                    foreach (string error in configuredWorker.ConfigErrors())
                    {
                        yield return "worker: " + error;
                    }
                }
            }
        }
    }
}
