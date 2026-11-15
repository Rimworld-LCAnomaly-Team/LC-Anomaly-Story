using LCAnomalyStory.Components;
using LCAnomalyStory.Defs;
using Verse;

namespace LCAnomalyStory.Examinations
{
    public sealed class ExaminationContext
    {
        internal ExaminationContext(
            GameComponent_CompanyDevelopment component,
            CompanyExaminationDef definition,
            ExaminationRuntime runtime)
        {
            Component = component;
            Definition = definition;
            Runtime = runtime;
        }

        public GameComponent_CompanyDevelopment Component { get; }

        public CompanyExaminationDef Definition { get; }

        public ExaminationRuntime Runtime { get; }

        public int CurrentTick
        {
            get { return Find.TickManager == null ? 0 : Find.TickManager.TicksGame; }
        }

        public bool IsActive
        {
            get { return Runtime.status == ExaminationStatus.Active; }
        }

        public int TicksRemaining
        {
            get { return Runtime.deadlineTick < 0 ? -1 : System.Math.Max(0, Runtime.deadlineTick - CurrentTick); }
        }

        public long StatisticDelta(string statistic)
        {
            long baseline;
            Runtime.statisticBaselines.TryGetValue(statistic, out baseline);
            return System.Math.Max(0L, Component.GetStatistic(statistic) - baseline);
        }

        public long GetLong(string key, long defaultValue = 0L)
        {
            long value;
            return key != null && Runtime.longState.TryGetValue(key, out value) ? value : defaultValue;
        }

        public void SetLong(string key, long value)
        {
            if (!key.NullOrEmpty())
            {
                Runtime.longState[key] = value;
            }
        }

        public long IncrementLong(string key, long amount = 1L)
        {
            long value = GetLong(key) + amount;
            SetLong(key, value);
            return value;
        }

        public float GetFloat(string key, float defaultValue = 0f)
        {
            float value;
            return key != null && Runtime.floatState.TryGetValue(key, out value) ? value : defaultValue;
        }

        public void SetFloat(string key, float value)
        {
            if (!key.NullOrEmpty())
            {
                Runtime.floatState[key] = value;
            }
        }

        public string GetString(string key, string defaultValue = null)
        {
            string value;
            return key != null && Runtime.stringState.TryGetValue(key, out value) ? value : defaultValue;
        }

        public void SetString(string key, string value)
        {
            if (key.NullOrEmpty())
            {
                return;
            }
            if (value == null)
            {
                Runtime.stringState.Remove(key);
            }
            else
            {
                Runtime.stringState[key] = value;
            }
        }

        public bool Pass(string reason = null)
        {
            return Component.PassExamination(Definition, reason);
        }

        public bool Fail(string reason = null)
        {
            return Component.FailExamination(Definition, reason);
        }

        public bool Cancel(string reason = null)
        {
            return Component.CancelExamination(Definition, reason);
        }
    }
}
