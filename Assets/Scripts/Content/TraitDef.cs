using UnityEngine;

namespace Wildbond
{
    [CreateAssetMenu(menuName = "Wildbond/Trait", fileName = "Trait_")]
    public class TraitDef : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(1, 2)] public string description;
        public bool positive = true;
        [Tooltip("Relative roll weight for wild spawns. 0 = never rolled randomly (special traits).")]
        public float weight = 1f;
        public float hpMul = 1f;
        public float atkMul = 1f;
        public float defMul = 1f;
        public float speedMul = 1f;
        public float workMul = 1f;
        public float hungerMul = 1f;
        public Element elementBoost = Element.Normal;
        public float elementBoostMul = 1f;
    }
}
