using System;
using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// A real evolved form (SpeciesDef.forms: [0] Ascended at Lv 20, [1] Exalted at Lv 35): its own name, its own
    /// body (a full part list, usually the base body with parts grown, swapped or added) and, for the Exalted form,
    /// optionally a second element and a skill it learns. A kind with no forms keeps its body and only grows and glows
    /// ("perfect form"). One Dex entry per kind either way.
    /// </summary>
    [Serializable]
    public class EvoForm
    {
        public string name;
        [TextArea(1, 3)] public string blurb;
        public BodyPart[] parts = new BodyPart[0];
        /// <summary>Empty = the kind's own pivots.</summary>
        public GroupPivot[] pivots = new GroupPivot[0];
        /// <summary>Exalted only: the element it gains (Normal = none).</summary>
        public Element element2 = Element.Normal;
        /// <summary>Exalted only: a skill it learns (takes the fourth skill slot).</summary>
        public string skill;
        /// <summary>Its own colours (else the kind's, warmed by the usual evolution glow). Shimmer and regional coats still win.</summary>
        public bool ownPalette;
        public Palette palette;
    }

    public partial class SpeciesDef
    {
        /// <summary>The real form at a stage (1 = Ascended, 2 = Exalted), or null.</summary>
        public EvoForm FormAt(int stage)
        {
            if (stage < 1 || forms == null || forms.Length < stage) return null;
            var f = forms[stage - 1];
            return f != null && !string.IsNullOrEmpty(f.name) ? f : null;
        }

        /// <summary>Has real evolved forms (else it is a "perfect form" kind: it only grows and glows).</summary>
        public bool RealEvolution => FormAt(1) != null;

        public BodyPart[] PartsAt(int stage)
        {
            for (int s = stage; s >= 1; s--) { var f = FormAt(s); if (f != null && f.parts != null && f.parts.Length > 0) return f.parts; }
            return parts;
        }

        public GroupPivot[] PivotsAt(int stage)
        {
            for (int s = stage; s >= 1; s--) { var f = FormAt(s); if (f != null && f.parts != null && f.parts.Length > 0) return f.pivots != null && f.pivots.Length > 0 ? f.pivots : pivots; }
            return pivots;
        }

        /// <summary>The name at a stage: the form's own name, else "Ascended X" / "Exalted X".</summary>
        public string NameAt(int stage)
        {
            var f = FormAt(stage);
            if (f != null) return f.name;
            return stage > 0 ? CreatureData.StageName(stage) + " " + displayName : displayName;
        }
    }
}
