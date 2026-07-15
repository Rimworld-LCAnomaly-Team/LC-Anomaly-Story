using System.Collections.Generic;
using UnityEngine;

namespace LCAnomalyStory.Examinations
{
    public abstract class ExaminationEffect
    {
        public virtual IEnumerable<string> ConfigErrors()
        {
            yield break;
        }

        public virtual bool CanStart(ExaminationContext context, out string rejectionReason)
        {
            rejectionReason = null;
            return true;
        }

        public virtual void OnStarted(ExaminationContext context)
        {
        }

        public virtual void OnLoaded(ExaminationContext context)
        {
        }

        public virtual void Tick(ExaminationContext context)
        {
        }

        public virtual void OnPassed(ExaminationContext context)
        {
        }

        public virtual void OnFailed(ExaminationContext context)
        {
        }

        public virtual void OnCancelled(ExaminationContext context)
        {
        }

        public virtual void ModifyStudySuccessRate(ExaminationContext context, ref float value)
        {
        }
    }

    public sealed class ExaminationEffect_WorkSuccessPenalty : ExaminationEffect
    {
        public float amount;

        public override IEnumerable<string> ConfigErrors()
        {
            if (amount <= 0f || amount > 1f)
            {
                yield return "amount must be greater than 0 and no greater than 1";
            }
        }

        public override void ModifyStudySuccessRate(ExaminationContext context, ref float value)
        {
            value = Mathf.Clamp01(value - amount);
        }
    }
}
