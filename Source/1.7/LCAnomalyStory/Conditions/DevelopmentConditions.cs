using System.Collections.Generic;
using System.Linq;
using LCAnomalyStory.Components;
using LCAnomalyStory.Defs;
using Verse;
using CoreComponents = LCAnomalyCore.Util.Components;

namespace LCAnomalyStory.Conditions
{
    public abstract class DevelopmentCondition
    {
        public string labelKey;

        public virtual IEnumerable<string> ConfigErrors()
        {
            yield break;
        }

        public abstract bool IsSatisfied(GameComponent_CompanyDevelopment component, CompanyDevelopmentDef node);

        public abstract string ProgressText(GameComponent_CompanyDevelopment component, CompanyDevelopmentDef node);

        protected string Label(string fallbackKey)
        {
            return (labelKey.NullOrEmpty() ? fallbackKey : labelKey).Translate();
        }
    }

    public class DevelopmentCondition_StatisticAtLeast : DevelopmentCondition
    {
        public string statistic;
        public long target;
        public bool sinceNodeActivation = true;

        public override bool IsSatisfied(GameComponent_CompanyDevelopment component, CompanyDevelopmentDef node)
        {
            return Current(component, node) >= target;
        }

        public override string ProgressText(GameComponent_CompanyDevelopment component, CompanyDevelopmentDef node)
        {
            return Label("LCStory_Condition_Statistic") + ": " + Current(component, node) + " / " + target;
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

        private long Current(GameComponent_CompanyDevelopment component, CompanyDevelopmentDef node)
        {
            long value = component.GetStatistic(statistic);
            if (sinceNodeActivation)
            {
                value -= component.GetActivationBaseline(node, statistic);
            }
            return System.Math.Max(0L, value);
        }
    }

    public class DevelopmentCondition_ThingCountAtLeast : DevelopmentCondition
    {
        public ThingDef thingDef;
        public int count = 1;

        public override bool IsSatisfied(GameComponent_CompanyDevelopment component, CompanyDevelopmentDef node)
        {
            return CurrentCount() >= count;
        }

        public override string ProgressText(GameComponent_CompanyDevelopment component, CompanyDevelopmentDef node)
        {
            return Label("LCStory_Condition_ThingCount") + ": " + CurrentCount() + " / " + count;
        }

        public override IEnumerable<string> ConfigErrors()
        {
            if (thingDef == null)
            {
                yield return "thingDef is required";
            }
            if (count < 1)
            {
                yield return "count must be at least 1";
            }
        }

        private int CurrentCount()
        {
            if (thingDef == null || Find.Maps == null)
            {
                return 0;
            }
            return Find.Maps.Sum(map => map.listerThings.ThingsOfDef(thingDef).Count(thing => !thing.Destroyed));
        }
    }

    public class DevelopmentCondition_CoreAnomalyCountAtLeast : DevelopmentCondition
    {
        public int count = 1;

        public override bool IsSatisfied(GameComponent_CompanyDevelopment component, CompanyDevelopmentDef node)
        {
            return CurrentCount() >= count;
        }

        public override string ProgressText(GameComponent_CompanyDevelopment component, CompanyDevelopmentDef node)
        {
            return Label("LCStory_Condition_AnomalyCount") + ": " + CurrentCount() + " / " + count;
        }

        public override IEnumerable<string> ConfigErrors()
        {
            if (count < 1)
            {
                yield return "count must be at least 1";
            }
        }

        private static int CurrentCount()
        {
            return CoreComponents.LC == null ? 0 : CoreComponents.LC.AnomalyStatusSavedDict.Keys.Count(def => def != null);
        }
    }

    public class DevelopmentCondition_AnyCoreStudyLevelAtLeast : DevelopmentCondition
    {
        public int level = 1;

        public override bool IsSatisfied(GameComponent_CompanyDevelopment component, CompanyDevelopmentDef node)
        {
            return CurrentLevel() >= level;
        }

        public override string ProgressText(GameComponent_CompanyDevelopment component, CompanyDevelopmentDef node)
        {
            return Label("LCStory_Condition_StudyLevel") + ": " + CurrentLevel() + " / " + level;
        }

        public override IEnumerable<string> ConfigErrors()
        {
            if (level < 1)
            {
                yield return "level must be at least 1";
            }
        }

        private static int CurrentLevel()
        {
            return CoreComponents.LC == null || CoreComponents.LC.AnomalyStatusSavedDict.Count == 0
                ? 0
                : CoreComponents.LC.AnomalyStatusSavedDict.Values.Max(saved => saved.StudyProgress);
        }
    }

    public class DevelopmentCondition_ExaminationPassed : DevelopmentCondition
    {
        public CompanyExaminationDef examination;

        public override bool IsSatisfied(GameComponent_CompanyDevelopment component, CompanyDevelopmentDef node)
        {
            return examination != null && component.IsExaminationPassed(examination);
        }

        public override string ProgressText(GameComponent_CompanyDevelopment component, CompanyDevelopmentDef node)
        {
            string state = examination == null
                ? "LCStory_ExamState_Unavailable".Translate()
                : component.GetExaminationStateLabel(examination);
            return Label("LCStory_Condition_Examination") + ": " + state;
        }

        public override IEnumerable<string> ConfigErrors()
        {
            if (examination == null)
            {
                yield return "examination is required";
            }
        }
    }

    public class DevelopmentCondition_DepartmentPrioritized : DevelopmentCondition
    {
        public CompanyDepartmentDef department;

        public override bool IsSatisfied(GameComponent_CompanyDevelopment component, CompanyDevelopmentDef node)
        {
            return department != null && component.IsDepartmentPrioritized(department);
        }

        public override string ProgressText(GameComponent_CompanyDevelopment component, CompanyDevelopmentDef node)
        {
            return Label("LCStory_Condition_DepartmentPriority") + ": " + (department == null ? "-" : department.LabelCap.ToString());
        }

        public override IEnumerable<string> ConfigErrors()
        {
            if (department == null)
            {
                yield return "department is required";
            }
        }
    }

    public class DevelopmentCondition_AnyNodeCompleted : DevelopmentCondition
    {
        public List<CompanyDevelopmentDef> nodes = new List<CompanyDevelopmentDef>();

        public override bool IsSatisfied(GameComponent_CompanyDevelopment component, CompanyDevelopmentDef node)
        {
            return nodes != null && nodes.Any(component.IsCompleted);
        }

        public override string ProgressText(GameComponent_CompanyDevelopment component, CompanyDevelopmentDef node)
        {
            int complete = nodes == null ? 0 : nodes.Count(component.IsCompleted);
            return Label("LCStory_Condition_AnyNode") + ": " + complete + " / 1";
        }

        public override IEnumerable<string> ConfigErrors()
        {
            if (nodes == null || nodes.Count == 0)
            {
                yield return "at least one node is required";
            }
        }
    }
}
