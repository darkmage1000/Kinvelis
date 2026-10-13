namespace Wildbond
{
    /// <summary>
    /// What the game is called wherever a player can read it: the app's name under its icon, the main-menu title and
    /// menu text. The user is trying "Kinvelis" (2026-10-10; it was "Wildbond"). Change it here, re-run
    /// Tools/compose_loading.py (its TITLE line) for the loading pictures, and build.
    /// Deliberately NOT renamed, because nothing a player sees depends on them and changing them would orphan saves or
    /// break tooling: the code namespace and editor menu ("Wildbond"), shader names, the Android package id
    /// (com.wildbond.game - a new id would install as a different app with no save), save file names and the company name.
    /// </summary>
    public static class GameInfo
    {
        public const string Title = "Kinvelis";
        /// <summary>Offers the DEV MODE switch in Settings (Core/Dev.cs). Set false before a store build: with it off the switch is not shown and nothing in Dev does anything.</summary>
        public const bool DevBuild = true;
    }
}
