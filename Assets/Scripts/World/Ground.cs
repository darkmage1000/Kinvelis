namespace Wildbond
{
    /// <summary>
    /// Gameplay ground height: the cave floor inside the reserved cave zone, the analytic terrain everywhere else.
    /// Use this (not WorldGen.Height) for anything that walks, lands or aims.
    /// </summary>
    public static class Ground
    {
        public static float Height(float x, float z) => Caves.InZone(x, z) ? Caves.FloorY : WorldGen.Height(x, z);
    }
}
