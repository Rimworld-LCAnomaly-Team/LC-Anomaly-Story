using System.Collections.Generic;
using System.Linq;
using LCAnomalyStory.Components;
using LCAnomalyStory.Conditions;
using LCAnomalyStory.Rewards;
using RimWorld;
using Verse;

namespace LCAnomalyStory.Defs
{
    public enum DevelopmentNodeKind
    {
        Milestone,
        DepartmentTask,
        StageGate
    }

    public class CompanyDevelopmentDef : Def
    {
        public int phase;
        public int displayOrder;
        public DevelopmentNodeKind kind;
        public CompanyDepartmentDef department;
        public bool hidden;
        public bool manualCompletion;
        public List<CompanyDevelopmentDef> prerequisites = new List<CompanyDevelopmentDef>();
        public List<DevelopmentCondition> conditions = new List<DevelopmentCondition>();
        public List<DevelopmentReward> rewards = new List<DevelopmentReward>();
        public string completionLetterLabel;
        public string completionLetterText;
        public LetterDef completionLetterDef;

        public bool PrerequisitesCompleted(GameComponent_CompanyDevelopment component)
        {
            return prerequisites == null || prerequisites.All(component.IsCompleted);
        }

        public bool ConditionsSatisfied(GameComponent_CompanyDevelopment component)
        {
            return conditions == null || conditions.All(condition => condition != null && condition.IsSatisfied(component, this));
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (phase < 0)
            {
                yield return "phase must not be negative";
            }

            if (prerequisites != null && prerequisites.Contains(this))
            {
                yield return "a development node cannot require itself";
            }

            if (conditions != null)
            {
                foreach (DevelopmentCondition condition in conditions)
                {
                    if (condition == null)
                    {
                        yield return "contains a null condition";
                        continue;
                    }

                    foreach (string error in condition.ConfigErrors())
                    {
                        yield return "condition: " + error;
                    }
                }
            }

            if (rewards != null)
            {
                foreach (DevelopmentReward reward in rewards)
                {
                    if (reward == null)
                    {
                        yield return "contains a null reward";
                        continue;
                    }

                    foreach (string error in reward.ConfigErrors())
                    {
                        yield return "reward: " + error;
                    }
                }
            }
        }
    }
}
