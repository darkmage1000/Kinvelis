using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Travel help from the kin that is out with you, by its elements (user, 2026-10-14: "as we have armor and gear,
    /// kin should also be able to help: fire kin keep you warm, cold kin keep you chilled, floral kin cure poison, so
    /// on"). A kin with two elements gives both. Each effect is read at its use site through the getters below, which
    /// are neutral while no kin is out (or it has fainted, or you are riding it - a mount carries you instead):
    ///   Ember  warm: cold exposure x0.25                Frost  cool: heat exposure x0.25
    ///   Flora  poison wears off within ~1.5 s, toxic water does half
    ///   Tide   burns are put out quickly, swimming costs half the stamina
    ///   Volt   stamina comes back 30% faster            Lumen  lights the dark (companion light), drowsiness shaken off
    ///   Gale   you glide down slower and never take fall damage
    ///   Stone  8% less damage, quicksand does not drag at you
    ///   Shade  wild kin notice you a third later        Normal stamina costs 15% less
    /// Shown on the Kin tab (`Describe`), the Dex, and in the HUD vitals line when it is doing something.
    /// </summary>
    public static class KinAid
    {
        static CreatureActor Out
        {
            get
            {
                var gm = GameManager.I;
                if (gm == null || gm.Party == null) return null;
                var c = gm.Party.Companion;
                if (c == null || !c.IsAlive || c.Data == null || c.Data.Fainted || c.Mounted) return null;
                return c;
            }
        }

        public static bool Has(Element e)
        {
            var c = Out;
            return c != null && (c.Data.Element1 == e || c.Data.Element2 == e);
        }

        /// <summary>The name of the kin giving this help (for the HUD line), or null.</summary>
        public static string Who(Element e) => Has(e) ? Out.Data.DisplayName : null;

        public static float ColdMul => Has(Element.Ember) ? 0.25f : 1f;
        public static float HeatMul => Has(Element.Frost) ? 0.25f : 1f;
        public static float ToxicWaterMul => Has(Element.Flora) ? 0.5f : 1f;
        public static float SwimStaminaMul => Has(Element.Tide) ? 0.5f : 1f;
        public static float StaminaRegenMul => Has(Element.Volt) ? 1.3f : 1f;
        public static float StaminaCostMul => Has(Element.Normal) ? 0.85f : 1f;
        public static float GlideFallMul => Has(Element.Gale) ? 0.6f : 1f;
        public static bool NoFallDamage => Has(Element.Gale);
        public static float DamageTakenMul => Has(Element.Stone) ? 0.92f : 1f;
        public static bool QuicksandProof => Has(Element.Stone);
        /// <summary>Wild kin see / hear you at this share of their usual distance.</summary>
        public static float NoticeMul => Has(Element.Shade) ? 0.65f : 1f;
        public static bool Light => Has(Element.Lumen);

        static float cleanse;
        /// <summary>Every frame from PlayerController: the status helps (Flora cleans poison, Tide puts out burns, Lumen wakes you).</summary>
        public static void Tick(PlayerController pl, float dt)
        {
            if (!pl.Status.Active) { cleanse = 0f; return; }
            Element by = pl.Status.type == StatusType.Poison ? Element.Flora : pl.Status.type == StatusType.Burn ? Element.Tide : pl.Status.type == StatusType.Drowsy ? Element.Lumen : Element.Normal;
            if (by == Element.Normal || !Has(by)) { cleanse = 0f; return; }
            cleanse += dt;
            if (cleanse < 1.5f) return;
            cleanse = 0f;
            var c = Out;
            string what = pl.Status.type == StatusType.Poison ? "draws the poison out" : pl.Status.type == StatusType.Burn ? "douses the flames" : "shakes you awake";
            pl.Status = default(StatusState);
            Fx.Sparkle(pl.Position + Vector3.up * 1.2f, ElementInfo.Color(by), 12, 0.6f);
            GameEvents.Say($"{c.Data.DisplayName} {what}.", ElementInfo.Color(by));
        }

        /// <summary>"Keeps you warm in the cold" lines for one kin (Kin tab, Dex).</summary>
        public static string Describe(Element e1, Element e2)
        {
            string a = Line(e1), b = e2 != e1 && e2 != Element.Normal ? Line(e2) : null;
            return b != null ? a + "; " + b : a;
        }

        public static string Line(Element e)
        {
            switch (e)
            {
                case Element.Ember: return "keeps you warm in the cold";
                case Element.Frost: return "keeps you cool in the heat";
                case Element.Flora: return "draws out poison; toxic water hurts half as much";
                case Element.Tide: return "puts out burns; swimming costs half the stamina";
                case Element.Volt: return "stamina comes back 30% faster";
                case Element.Lumen: return "lights the dark; shakes off drowsiness";
                case Element.Gale: return "slower glides, no fall damage";
                case Element.Stone: return "8% less damage; quicksand can't drag you";
                case Element.Shade: return "wild kin notice you a third later";
                default: return "stamina costs 15% less";
            }
        }
    }
}
