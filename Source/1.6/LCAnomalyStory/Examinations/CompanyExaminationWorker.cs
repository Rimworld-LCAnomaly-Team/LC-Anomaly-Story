using System.Collections.Generic;
using System.Linq;
using LCAnomalyStory.Defs;

namespace LCAnomalyStory.Examinations
{
    public enum ExaminationOutcome
    {
        None,
        Passed,
        Failed
    }

    public class CompanyExaminationWorker
    {
        public CompanyExaminationDef Def { get; private set; }

        internal void Initialize(CompanyExaminationDef definition)
        {
            Def = definition;
        }

        public virtual IEnumerable<string> ConfigErrors()
        {
            yield break;
        }

        public virtual bool CanStart(ExaminationContext context, out string rejectionReason)
        {
            rejectionReason = null;
            if (Def.effects == null)
            {
                return true;
            }
            foreach (ExaminationEffect effect in Def.effects.Where(effect => effect != null))
            {
                if (!effect.CanStart(context, out rejectionReason))
                {
                    return false;
                }
            }
            return true;
        }

        public virtual void OnStarted(ExaminationContext context)
        {
            ForEachEffect(effect => effect.OnStarted(context));
        }

        public virtual void OnLoaded(ExaminationContext context)
        {
            ForEachEffect(effect => effect.OnLoaded(context));
        }

        public virtual void Tick(ExaminationContext context)
        {
            ForEachEffect(effect => effect.Tick(context));
        }

        public virtual ExaminationOutcome Evaluate(ExaminationContext context)
        {
            if (Def.objectives != null
                && Def.objectives.Count > 0
                && Def.objectives.All(objective => objective != null && objective.IsSatisfied(context.Component, context.Runtime)))
            {
                return ExaminationOutcome.Passed;
            }
            if (context.Runtime.deadlineTick >= 0 && context.CurrentTick >= context.Runtime.deadlineTick)
            {
                return ExaminationOutcome.Failed;
            }
            return ExaminationOutcome.None;
        }

        public virtual void OnPassed(ExaminationContext context)
        {
            ForEachEffect(effect => effect.OnPassed(context));
        }

        public virtual void OnFailed(ExaminationContext context)
        {
            ForEachEffect(effect => effect.OnFailed(context));
        }

        public virtual void OnCancelled(ExaminationContext context)
        {
            ForEachEffect(effect => effect.OnCancelled(context));
        }

        public virtual IEnumerable<string> ProgressLines(ExaminationContext context)
        {
            if (Def.objectives == null)
            {
                yield break;
            }
            foreach (ExaminationObjective objective in Def.objectives.Where(objective => objective != null))
            {
                yield return objective.ProgressText(context.Component, context.Runtime);
            }
        }

        public virtual void ModifyStudySuccessRate(ExaminationContext context, ref float value)
        {
            if (Def.effects == null)
            {
                return;
            }
            foreach (ExaminationEffect effect in Def.effects.Where(effect => effect != null))
            {
                effect.ModifyStudySuccessRate(context, ref value);
            }
        }

        private void ForEachEffect(System.Action<ExaminationEffect> action)
        {
            if (Def.effects == null)
            {
                return;
            }
            foreach (ExaminationEffect effect in Def.effects.Where(effect => effect != null))
            {
                action(effect);
            }
        }
    }
}
