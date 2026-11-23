using System.Collections.Generic;
using LCAnomalyCore.GameComponent;
using RimWorld;
using UnityEngine;
using Verse;

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

    /// <summary>考核期间为全设施殖民者施加临时压制 Hediff（如培训部核心抑制）。</summary>
    public sealed class ExaminationEffect_ColonistHediff : ExaminationEffect
    {
        public HediffDef hediff;

        public override IEnumerable<string> ConfigErrors()
        {
            if (hediff == null)
            {
                yield return "hediff is required";
            }
        }

        public override void OnStarted(ExaminationContext context) => ApplyToAllColonists();
        public override void OnLoaded(ExaminationContext context) => ApplyToAllColonists();
        public override void Tick(ExaminationContext context) => ApplyToAllColonists();
        public override void OnPassed(ExaminationContext context) => RemoveFromAllColonists();
        public override void OnFailed(ExaminationContext context) => RemoveFromAllColonists();
        public override void OnCancelled(ExaminationContext context) => RemoveFromAllColonists();

        private void ApplyToAllColonists()
        {
            if (hediff == null || Find.Maps == null) return;
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
                {
                    if (pawn.health?.hediffSet?.GetFirstHediffOfDef(hediff) == null)
                    {
                        pawn.health?.AddHediff(hediff);
                    }
                }
            }
        }

        private void RemoveFromAllColonists()
        {
            if (hediff == null || Find.Maps == null) return;
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
                {
                    Hediff existing = pawn.health?.hediffSet?.GetFirstHediffOfDef(hediff);
                    if (existing != null)
                    {
                        pawn.health.RemoveHediff(existing);
                    }
                }
            }
        }
    }

    /// <summary>考核期间封锁所有部门核心房间再生治疗（安保部核心抑制）。</summary>
    public sealed class ExaminationEffect_HealLockout : ExaminationEffect
    {
        public override void OnStarted(ExaminationContext context) => SetLocked(true);
        public override void OnLoaded(ExaminationContext context) => SetLocked(true);
        public override void OnPassed(ExaminationContext context) => SetLocked(false);
        public override void OnFailed(ExaminationContext context) => SetLocked(false);
        public override void OnCancelled(ExaminationContext context) => SetLocked(false);

        private static void SetLocked(bool locked)
        {
            if (GameComponent_LC.Instance != null)
            {
                GameComponent_LC.Instance.SuppressionHealLocked = locked;
            }
        }
    }

    /// <summary>考核期间周期性施加轻度精神压力脉冲（控制/情报/惩戒/研发核心抑制）。</summary>
    public sealed class ExaminationEffect_MentalStressPulse : ExaminationEffect
    {
        public int intervalTicks = 2500;
        public float severity = 0.04f;

        public override void Tick(ExaminationContext context)
        {
            if (intervalTicks <= 0 || severity <= 0f || Find.Maps == null) return;
            string key = "LCStory.stressPulse.next." + context.Definition.defName;
            long next = context.GetLong(key, -1L);
            if (next < 0)
            {
                context.SetLong(key, context.CurrentTick + intervalTicks);
                return;
            }
            if (context.CurrentTick < next) return;
            context.SetLong(key, context.CurrentTick + intervalTicks);

            HediffDef mentalDef = LCAnomalyCore.Defs.HediffDefOf.LC_MentalDamage;
            if (mentalDef == null) return;

            foreach (Map map in Find.Maps)
            {
                if (!map.IsPlayerHome) continue;
                foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
                {
                    if (pawn.Dead || pawn.Downed) continue;
                    Hediff h = pawn.health?.hediffSet?.GetFirstHediffOfDef(mentalDef) ?? pawn.health?.AddHediff(mentalDef);
                    if (h != null)
                    {
                        h.Severity = Mathf.Min(0.85f, h.Severity + severity);
                    }
                }
            }
        }
    }
}
