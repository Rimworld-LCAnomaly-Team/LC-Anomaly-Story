using System.Collections.Generic;
using LCAnomalyStory.Components;
using Verse;

namespace LCAnomalyStory.Defs
{
    public abstract class DirectiveObjective
    {
        public string labelKey;

        public abstract bool IsSatisfied(GameComponent_CompanyDevelopment component, DirectiveRuntime runtime);

        public abstract string ProgressText(GameComponent_CompanyDevelopment component, DirectiveRuntime runtime);

        public virtual IEnumerable<string> ConfigErrors()
        {
            yield break;
        }

        protected string Label
        {
            get { return (labelKey.NullOrEmpty() ? "LCStory_DirectiveObjective" : labelKey).Translate(); }
        }
    }

    public class DirectiveObjective_Statistic : DirectiveObjective
    {
        public string statistic;
        public long target;

        public override bool IsSatisfied(GameComponent_CompanyDevelopment component, DirectiveRuntime runtime)
        {
            return Current(component, runtime) >= target;
        }

        public override string ProgressText(GameComponent_CompanyDevelopment component, DirectiveRuntime runtime)
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

        private long Current(GameComponent_CompanyDevelopment component, DirectiveRuntime runtime)
        {
            long baseline;
            runtime.statisticBaselines.TryGetValue(statistic, out baseline);
            return System.Math.Max(0L, component.GetStatistic(statistic) - baseline);
        }
    }

    public class CompanyDirectiveDef : Def
    {
        public int minimumPhase = 1;
        public CompanyDepartmentDef department;
        public int durationTicks = 60000;
        public int cooldownTicks = 30000;
        public float selectionWeight = 1f;
        public int departmentExperience = 1;
        public List<DirectiveObjective> objectives = new List<DirectiveObjective>();

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }
            if (minimumPhase < 0)
            {
                yield return "minimumPhase must not be negative";
            }
            if (durationTicks < 60)
            {
                yield return "durationTicks must be at least 60";
            }
            if (cooldownTicks < 0)
            {
                yield return "cooldownTicks must not be negative";
            }
            if (selectionWeight <= 0f)
            {
                yield return "selectionWeight must be positive";
            }
            if (objectives == null || objectives.Count == 0)
            {
                yield return "at least one objective is required";
                yield break;
            }
            foreach (DirectiveObjective objective in objectives)
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
    }
}
