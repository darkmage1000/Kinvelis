using UnityEngine;

namespace Wildbond
{
    [CreateAssetMenu(menuName = "Wildbond/Recipe", fileName = "Recipe_")]
    public class RecipeDef : ScriptableObject
    {
        public string id;
        public string outputItem;
        public int outputCount = 1;
        public Ingredient[] inputs = new Ingredient[0];
        public CraftStation station = CraftStation.Hand;
        [Tooltip("Work points needed at a station. Hand recipes are instant.")]
        public float work = 10f;
        public int unlockLevel = 1;
    }
}
