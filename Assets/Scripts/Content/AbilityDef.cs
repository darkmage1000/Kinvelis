using UnityEngine;

namespace Wildbond
{
    [CreateAssetMenu(menuName = "Wildbond/Ability", fileName = "Ability_")]
    public class AbilityDef : ScriptableObject
    {
        public string id;
        public string displayName;
        public Element element;
        public AbilityKind kind;
        [Tooltip("Signature twist on top of the kind (see SkillEffects).")]
        public AbilityEffect effect;
        public float power = 15f;
        [Tooltip("Max distance to start the ability.")]
        public float range = 2.5f;
        public float cooldown = 3f;
        public StatusType status;
        [Range(0, 1)] public float statusChance;
        public float projectileSpeed = 16f;
        [Tooltip("Burst radius, or dash distance for Dash abilities.")]
        public float radius = 3f;
        public float windup = 0.35f;
        public Color color = Color.white;
    }
}
