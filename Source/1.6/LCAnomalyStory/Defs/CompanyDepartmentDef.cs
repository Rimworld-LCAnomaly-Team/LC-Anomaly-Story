using System.Collections.Generic;
using Verse;

namespace LCAnomalyStory.Defs
{
    public class CompanyDepartmentDef : Def
    {
        public int displayOrder;
        public int minimumPhase;
        public ThingDef coreThingDef;

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
        }
    }
}
