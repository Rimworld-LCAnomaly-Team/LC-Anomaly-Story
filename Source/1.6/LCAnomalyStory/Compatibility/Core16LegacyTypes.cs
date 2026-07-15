using System;
using Verse;

// LC Anomaly Core 1.6 still contains its RimWorld 1.5-era ITabDef XML and
// references the study tab by an unqualified type name. RimWorld 1.6 removed
// ITabDef and expects inspectorTabs entries to resolve directly to ITab types.
// These global aliases preserve Core's existing XML without modifying Core.

public class ITabDef : Def
{
    public Type tabWindowClass;
    public bool visibleByDefault;
}

public class ITab_AbnormalityStudyNotes : LCAnomalyCore.UI.ITab_AbnormalityStudyNotes
{
}
