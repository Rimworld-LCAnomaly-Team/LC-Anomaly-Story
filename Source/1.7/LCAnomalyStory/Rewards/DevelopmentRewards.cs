using System.Collections.Generic;
using LCAnomalyStory.Components;
using LCAnomalyStory.Defs;
using RimWorld;
using Verse;

namespace LCAnomalyStory.Rewards
{
    public abstract class DevelopmentReward
    {
        public string labelKey;

        public virtual IEnumerable<string> ConfigErrors()
        {
            yield break;
        }

        public abstract bool TryApply(GameComponent_CompanyDevelopment component, CompanyDevelopmentDef node);

        public virtual string Description
        {
            get { return (labelKey.NullOrEmpty() ? "LCStory_Reward_Generic" : labelKey).Translate(); }
        }
    }

    public class DevelopmentReward_FinishResearch : DevelopmentReward
    {
        public ResearchProjectDef project;

        public override bool TryApply(GameComponent_CompanyDevelopment component, CompanyDevelopmentDef node)
        {
            if (project == null || Find.ResearchManager == null)
            {
                return false;
            }
            if (!project.IsFinished)
            {
                Find.ResearchManager.FinishProject(project, false, null, false);
            }
            return project.IsFinished;
        }

        public override string Description
        {
            get { return project == null ? base.Description : "LCStory_Reward_Research".Translate(project.LabelCap); }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            if (project == null)
            {
                yield return "project is required";
            }
        }
    }

    public class DevelopmentReward_OpenDepartment : DevelopmentReward
    {
        public CompanyDepartmentDef department;

        public override bool TryApply(GameComponent_CompanyDevelopment component, CompanyDevelopmentDef node)
        {
            return component.OpenDepartment(department);
        }

        public override string Description
        {
            get { return "LCStory_Reward_Department".Translate(department == null ? string.Empty : department.LabelCap); }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            if (department == null)
            {
                yield return "department is required";
            }
        }
    }
}
