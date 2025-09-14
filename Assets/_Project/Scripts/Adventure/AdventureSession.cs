using System;

namespace ShapeBlaster.Adventure
{
    /// <summary>
    /// Static helper to manage Adventure mode session flags and week id.
    /// </summary>
    public static class AdventureSession
    {
        private const string ModeKey = "Adventure.Mode";
        private const string WeekKey = "Adventure.WeekId";
        private const string LevelIndexKey = "Adventure.LevelIndex";

        public static bool IsAdventureMode
        {
            get => UnityEngine.PlayerPrefs.GetInt(ModeKey, 0) == 1;
            private set => UnityEngine.PlayerPrefs.SetInt(ModeKey, value ? 1 : 0);
        }

        public static int CurrentWeekId
        {
            get => UnityEngine.PlayerPrefs.GetInt(WeekKey, ComputeWeekId());
            private set => UnityEngine.PlayerPrefs.SetInt(WeekKey, value);
        }

        public static int CurrentLevelIndex
        {
            get => UnityEngine.PlayerPrefs.GetInt(LevelIndexKey, 0);
            set => UnityEngine.PlayerPrefs.SetInt(LevelIndexKey, value);
        }

        public static void StartAdventureAndLoadGame()
        {
            IsAdventureMode = true;
            CurrentWeekId = ComputeWeekId();
            // Always start from level 0 as requested
            CurrentLevelIndex = 0;
            UnityEngine.SceneManagement.SceneManager.LoadScene("CoreGame");
        }

        public static void ExitAdventure()
        {
            IsAdventureMode = false;
        }

        public static int ComputeWeekId()
        {
            // ISO-like week anchor: weeks since Unix epoch Sunday (simple and stable)
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var now = DateTime.UtcNow;
            var days = (now - epoch).TotalDays;
            return (int)(days / 7);
        }
    }
}
