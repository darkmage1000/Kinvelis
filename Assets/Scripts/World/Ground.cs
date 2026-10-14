using UnityEngine;

namespace Wildbond
{
    /// <summary>
    /// Gameplay ground height: the cave floor inside the reserved cave zone, the analytic terrain everywhere else -
    /// or the top of a house floor (Housing.FloorAt) where one stands higher than the terrain. `HeightNear` also knows
    /// about upper storeys and stairs, for things that walk.
    /// Use this (not WorldGen.Height) for anything that walks, lands or aims.
    /// </summary>
    public static class Ground
    {
        public static float Height(float x, float z) => Caves.InZone(x, z) ? Caves.FloorY : Mathf.Max(WorldGen.Height(x, z), Housing.FloorAt(x, z));

        /// <summary>
        /// The ground under feet at height y: as Height, plus the house's upper floors and stairs that are within a step
        /// of those feet (Housing.SurfaceAt) - so someone downstairs walks under the floor above and someone upstairs stands on it.
        /// </summary>
        public static float HeightNear(float x, float z, float y) => Caves.InZone(x, z) ? Caves.FloorY : Mathf.Max(WorldGen.Height(x, z), Housing.SurfaceAt(x, z, y));
    }
}
