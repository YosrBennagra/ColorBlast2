using System;
using System.Collections.Generic;
using UnityEngine;
using ShapeBlaster.Adventure;
using ShapeBlaster.Systems.Scoring;
using Gameplay;

namespace ShapeBlaster.Adventure
{
    /// <summary>
    /// Orchestrates weekly Adventure mode levels and objective tracking.
    /// Place this in the CoreGame scene.
    /// </summary>
    public class AdventureManager : MonoBehaviour
    {
        [Header("Config")]
        [Tooltip("Resource path to the AdventureLevelLibrary (Resources/Adventure/AdventureLevelLibrary)")]
        public string levelLibraryResourcePath = "Adventure/AdventureLevelLibrary";
        [Tooltip("How many levels per weekly adventure")]
        public int levelsPerWeek = 50;
        [Tooltip("Apply pre-placed tiles defined in levels (disable to keep boards empty at start).")]
        public bool usePrePlacedTiles = false;

        [Header("Runtime Generator Settings")]
        public bool generatorAllowScoreGoal = true;
        [Range(1,3)] public int generatorMinGoals = 1;
        [Range(1,3)] public int generatorMaxGoals = 2;
        public Vector2Int generatorClearCountRange = new Vector2Int(6, 20);
        public Vector2Int generatorScoreBaseRange = new Vector2Int(1500, 4500);
        [Tooltip("Chance (0..1) to add a small cluster of pre-placed tiles per ClearTheme goal")]
        [Range(0f,1f)] public float generatorPreplaceClusterChance = 0.3f;

        

        [Header("Debug")]
        public bool logDebug = false;

        public static AdventureManager Instance { get; private set; }

        private AdventureLevelLibrary library;
        private List<int> weeklyLevelIndices = new List<int>();
        private AdventureLevel currentLevel;
        private int progressCount;
        private bool appliedThisScene = false;
        private List<int> goalProgress = new List<int>(); // for ClearTheme goals
        private int latestScore = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (!AdventureSession.IsAdventureMode)
            {
                if (logDebug) Debug.Log("AdventureManager disabled (not in adventure mode)");
                enabled = false;
                return;
            }

            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;

            LoadLibrary();
            BuildWeeklyList();
            BindHooks();
            LoadCurrentLevel();

            // Try applying if we're already in CoreGame
            TryApplyPrePlacedTiles();
        }

        private void OnDestroy()
        {
            UnbindHooks();
            if (Instance == this) Instance = null;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void LoadLibrary()
        {
#if UNITY_EDITOR
            // 1) Prefer project asset in _Project folder
            var projectPath = "Assets/_Project/AdventureLevels/AdventureLevelLibrary.asset";
            var projectLib = UnityEditor.AssetDatabase.LoadAssetAtPath<AdventureLevelLibrary>(projectPath);
            if (projectLib != null && projectLib.allLevels != null && projectLib.allLevels.Count > 0)
            {
                library = projectLib;
                // Keep Resources mirror in sync for runtime (only when not in Play mode)
                if (!Application.isPlaying)
                    SaveLibraryAsset(library, overwrite: true);
                return;
            }
#endif
            // 2) Try Resources
            library = Resources.Load<AdventureLevelLibrary>(levelLibraryResourcePath);
            if (library != null && library.allLevels != null && library.allLevels.Count > 0)
            {
#if UNITY_EDITOR
                // Ensure a project copy exists for easy editing (only when not in Play mode)
                if (!Application.isPlaying)
                    SaveLibraryAsset(library, overwrite: true);
#endif
                return;
            }

            // 3) Generate a starter set
            library = GenerateRuntimeLibrary();
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                SaveLibraryAsset(library, overwrite: true);
                Debug.Log("[Adventure] Created AdventureLevelLibrary at Assets/_Project/AdventureLevels/AdventureLevelLibrary.asset");
            }
            else
            {
                Debug.Log("[Adventure] Using runtime-generated AdventureLevelLibrary (Play Mode — not saving to disk)");
            }
#else
            Debug.Log("[Adventure] Using runtime-generated AdventureLevelLibrary (not saved in player builds)");
#endif
        }

#if UNITY_EDITOR
        [ContextMenu("Adventure/Regenerate And Save Library (Overwrite)")]
        private void EditorRegenerateAndSaveLibrary()
        {
            var gen = GenerateRuntimeLibrary();
            SaveLibraryAsset(gen, overwrite: true);
            library = gen;
            BuildWeeklyList();
            LoadCurrentLevel();
            TryApplyPrePlacedTiles();
            Debug.Log("Adventure library regenerated and saved to Assets/Resources/Adventure/AdventureLevelLibrary.asset");
        }

        [ContextMenu("Adventure/Sync Resources Mirror")]
        private void EditorSyncResourcesMirror()
        {
            if (library == null)
            {
                LoadLibrary();
            }
            if (library != null)
            {
                SaveLibraryAsset(library, overwrite: true);
                Debug.Log("[Adventure] Synced Resources mirror from project asset");
            }
        }

        private void SaveLibraryAsset(AdventureLevelLibrary lib, bool overwrite = false)
        {
            if (lib == null) return;
            if (Application.isPlaying) return; // Avoid asset DB operations during play to prevent reload warnings
            // Primary location requested by user
            string projectPath = "Assets/_Project/AdventureLevels/AdventureLevelLibrary.asset";
            // Mirror in Resources for runtime auto-load
            string resourcesPath = "Assets/Resources/Adventure/AdventureLevelLibrary.asset";

            // Ensure dirs
            var projDir = System.IO.Path.GetDirectoryName(projectPath);
            if (!System.IO.Directory.Exists(projDir)) System.IO.Directory.CreateDirectory(projDir);
            var resDir = System.IO.Path.GetDirectoryName(resourcesPath);
            if (!System.IO.Directory.Exists(resDir)) System.IO.Directory.CreateDirectory(resDir);

            // Helper to upsert an asset at a path from a source lib
            void Upsert(string path)
            {
                var existing = UnityEditor.AssetDatabase.LoadAssetAtPath<AdventureLevelLibrary>(path);
                if (existing != null && !overwrite)
                {
                    existing.allLevels.Clear();
                    existing.allLevels.AddRange(lib.allLevels);
                    UnityEditor.EditorUtility.SetDirty(existing);
                    UnityEditor.AssetDatabase.SaveAssets();
                    return;
                }
                if (existing != null && overwrite)
                {
                    UnityEditor.AssetDatabase.DeleteAsset(path);
                }
                // Create new asset instance with copied data
                var clone = ScriptableObject.CreateInstance<AdventureLevelLibrary>();
                clone.allLevels = new List<AdventureLevel>(lib.allLevels);
                UnityEditor.AssetDatabase.CreateAsset(clone, path);
            }

            Upsert(projectPath);
            Upsert(resourcesPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            // Load library from project path for editing/tweaking
            library = UnityEditor.AssetDatabase.LoadAssetAtPath<AdventureLevelLibrary>(projectPath);
        }
#else
        private void SaveLibraryAsset(AdventureLevelLibrary lib, bool overwrite = false) { /* no-op in player builds */ }
#endif

        private AdventureLevelLibrary GenerateRuntimeLibrary()
        {
            var lib = ScriptableObject.CreateInstance<AdventureLevelLibrary>();
            var mgr = ShapeSpriteManager.Instance;
            var themes = (mgr != null) ? mgr.GetThemeNames() : new List<string> { "Water", "Volcano" };
            int count = Mathf.Max(50, levelsPerWeek);
            int seed = AdventureSession.CurrentWeekId;
            var rng = new System.Random(seed * 7919 + 17);
            for (int i = 0; i < count; i++)
            {
                var lvl = new AdventureLevel();
                lvl.id = $"L{i + 1:00}";
                int goalsCount = Mathf.Clamp(rng.Next(generatorMinGoals, generatorMaxGoals + 1), 1, 3);
                lvl.mode = (rng.NextDouble() < 0.75) ? GoalMode.All : GoalMode.Any;
                var chosenThemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int g = 0; g < goalsCount; g++)
                {
                    bool makeScore = generatorAllowScoreGoal && (rng.NextDouble() < 0.4 || themes.Count == 0);
                    if (makeScore && lvl.goals.Exists(x => x.type == ObjectiveType.Score))
                    {
                        makeScore = false; // avoid multiple score goals
                    }
                    var goal = new AdventureGoal();
                    if (makeScore)
                    {
                        goal.type = ObjectiveType.Score;
                        goal.targetScore = rng.Next(generatorScoreBaseRange.x, generatorScoreBaseRange.y) + i * 10;
                    }
                    else
                    {
                        goal.type = ObjectiveType.ClearTheme;
                        string name = (themes.Count > 0) ? themes[rng.Next(themes.Count)] : "Water";
                        int attempts = 0;
                        while (chosenThemes.Contains(name) && attempts < 6 && themes.Count > 1)
                        {
                            name = themes[rng.Next(themes.Count)];
                            attempts++;
                        }
                        chosenThemes.Add(name);
                        goal.themeName = name;
                        goal.targetCount = rng.Next(generatorClearCountRange.x, generatorClearCountRange.y);

                        if (rng.NextDouble() < generatorPreplaceClusterChance)
                        {
                            int cx = rng.Next(0, 7);
                            int cy = rng.Next(0, 7);
                            var pts = new Vector2Int[] {
                                new Vector2Int(cx, cy), new Vector2Int(Mathf.Min(7, cx+1), cy), new Vector2Int(cx, Mathf.Min(7, cy+1))
                            };
                            foreach (var p in pts)
                                lvl.prePlacedTiles.Add(new AdventureLevel.PrePlacedTile { position = p, themeName = name });
                        }
                    }
                    lvl.goals.Add(goal);
                }
                // Description assembled from goals
                lvl.description = DescribeGoals(lvl);
                lib.allLevels.Add(lvl);
            }
            return lib;
        }

        private void BuildWeeklyList()
        {
            weeklyLevelIndices.Clear();
            if (library == null || library.allLevels.Count == 0) return;
            int count = library.allLevels.Count;
            int take = Mathf.Min(levelsPerWeek, count);
            // Simple: take the first `levelsPerWeek` levels in order
            for (int i = 0; i < take; i++) weeklyLevelIndices.Add(i);
        }

        private void BindHooks()
        {
            // Tile detailed clear (added in LineClearSystem)
            LineClearSystem.OnTilesClearedDetailed += HandleTilesClearedDetailed;
            // Score change (added in ScoreManager)
            ScoreManager.OnScoreChanged += HandleScoreChanged;
        }

        private void UnbindHooks()
        {
            LineClearSystem.OnTilesClearedDetailed -= HandleTilesClearedDetailed;
            ScoreManager.OnScoreChanged -= HandleScoreChanged;
        }

        private void LoadCurrentLevel()
        {
            progressCount = 0;
            latestScore = 0;
            currentLevel = null;
            if (library == null || weeklyLevelIndices.Count == 0) return;
            int idx = AdventureSession.CurrentLevelIndex;
            if (idx < 0 || idx >= weeklyLevelIndices.Count)
            {
                idx = 0;
                AdventureSession.CurrentLevelIndex = 0;
            }
            currentLevel = library.allLevels[weeklyLevelIndices[idx]];
            NormalizeGoals(currentLevel);
            InitGoalProgress(currentLevel);
            // Always log the level id for clarity
            Debug.Log($"[Adventure] Entered Level ID: {currentLevel?.id ?? "<none>"} ({idx + 1}/{weeklyLevelIndices.Count})");
            if (logDebug)
            {
                Debug.Log($"[Adventure] Goals: {DescribeObjective(currentLevel)}");
            }
        }

        

        private void InitGoalProgress(AdventureLevel lvl)
        {
            goalProgress.Clear();
            if (lvl == null) return;
            int count = (lvl.goals != null && lvl.goals.Count > 0) ? lvl.goals.Count : 1;
            for (int i = 0; i < count; i++) goalProgress.Add(0);
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            appliedThisScene = false;
            if (!AdventureSession.IsAdventureMode) return;
            if (scene.name == "CoreGame")
            {
                // Wait one frame to let scene bootstrap
                StartCoroutine(ApplyAfterFrame());
            }
        }

        private System.Collections.IEnumerator ApplyAfterFrame()
        {
            yield return null;
            TryApplyPrePlacedTiles();
        }

        private void TryApplyPrePlacedTiles()
        {
            if (appliedThisScene) return;
            if (!usePrePlacedTiles) { appliedThisScene = true; return; }
            if (currentLevel == null || currentLevel.prePlacedTiles == null || currentLevel.prePlacedTiles.Count == 0) { appliedThisScene = true; return; }
            var gm = FindFirstObjectByType<GridManager>();
            if (gm == null) return;
            Debug.Log($"[Adventure] Applying pre-placed tiles for Level ID: {currentLevel.id}");
            ApplyPrePlacedTilesForCurrentLevel();
            appliedThisScene = true;
        }

        private void ApplyPrePlacedTilesForCurrentLevel()
        {
            var gm = FindFirstObjectByType<GridManager>();
            var ssm = ShapeSpriteManager.Instance;
            if (gm == null || ssm == null || currentLevel == null) return;
            foreach (var t in currentLevel.prePlacedTiles)
            {
                var pos = t.position;
                if (!gm.IsValidGridPosition(pos) || gm.IsCellOccupied(pos)) continue;
                var theme = ssm.FindThemeByName(t.themeName);
                SpawnFixedTileAt(gm, t, theme);
            }
        }

        private void SpawnFixedTileAt(GridManager gm, AdventureLevel.PrePlacedTile data, SpriteTheme theme)
        {
            if (data == null) return;
            Vector2Int gridPos = data.position;
            Vector3 world = gm.GridToWorldPosition(gridPos);
            string label = !string.IsNullOrEmpty(data.themeName) ? data.themeName : (theme!=null?theme.themeName:"None");
            var go = new GameObject($"PreTile_{label}_{gridPos.x}_{gridPos.y}");
            go.transform.position = world;
            var shape = go.AddComponent<ColorBlast.Game.Shape>();
            shape.SetShapeOffsets(new List<Vector2Int> { Vector2Int.zero });
            shape.MarkAsPlaced();
            // Occupy grid
            gm.OccupyCell(gridPos);

            // Add theme storage for clear tracking
            var themeStore = go.AddComponent<ShapeThemeStorage>();
            if (theme != null) themeStore.SetTheme(theme);

            // Create visual tile
            var tile = new GameObject("Tile");
            tile.transform.SetParent(go.transform, false);
            tile.transform.localPosition = Vector3.zero;
            tile.transform.localScale = Vector3.one * gm.CellSize; // ensure it fits cell size
            var sr = tile.AddComponent<SpriteRenderer>();
            // Prefer explicit sprite override if provided, else theme sprite, else fallback
            sr.sprite = data.spriteOverride != null
                ? data.spriteOverride
                : ((theme != null && theme.tileSprite != null) ? theme.tileSprite : Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd"));
            sr.color = Color.white;
            sr.sortingOrder = 0;
        }

        private void NormalizeGoals(AdventureLevel lvl)
        {
            if (lvl == null) return;
            if (lvl.goals != null && lvl.goals.Count > 0) return;
            // Convert legacy single objective to goals list
            var g = new AdventureGoal();
            if (lvl.type == ObjectiveType.Score)
            {
                g.type = ObjectiveType.Score; g.targetScore = Mathf.Max(1, lvl.targetScore);
            }
            else
            {
                g.type = ObjectiveType.ClearTheme; g.themeName = lvl.themeName; g.targetCount = Mathf.Max(1, lvl.targetCount);
            }
            lvl.mode = GoalMode.All;
            lvl.goals = new List<AdventureGoal> { g };
        }

        private string DescribeObjective(AdventureLevel lvl)
        {
            if (lvl == null) return "None";
            if (lvl.goals != null && lvl.goals.Count > 0)
            {
                return DescribeGoals(lvl);
            }
            return lvl.type == ObjectiveType.Score
                ? $"Reach score {lvl.targetScore:N0}"
                : $"Clear {lvl.targetCount} of '{lvl.themeName}'";
        }

        private string DescribeGoals(AdventureLevel lvl)
        {
            if (lvl == null || lvl.goals == null || lvl.goals.Count == 0) return "None";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            string joiner = lvl.mode == GoalMode.All ? " AND " : " OR ";
            for (int i = 0; i < lvl.goals.Count; i++)
            {
                var g = lvl.goals[i];
                if (g.type == ObjectiveType.Score) sb.Append($"Reach {g.targetScore:N0}");
                else sb.Append($"Clear {g.targetCount} {g.themeName}");
                if (i < lvl.goals.Count - 1) sb.Append(joiner);
            }
            return sb.ToString();
        }

        private void HandleTilesClearedDetailed(List<LineClearSystem.TileClearInfo> infos)
        {
            if (currentLevel == null || infos == null) return;
            if (currentLevel.goals != null && currentLevel.goals.Count > 0)
            {
                for (int gi = 0; gi < currentLevel.goals.Count; gi++)
                {
                    var g = currentLevel.goals[gi];
                    if (g.type != ObjectiveType.ClearTheme || string.IsNullOrEmpty(g.themeName)) continue;
                    string target = g.themeName.Trim();
                    int add = 0;
                    for (int i = 0; i < infos.Count; i++)
                    {
                        var name = infos[i].themeName;
                        if (!string.IsNullOrEmpty(name) && string.Equals(name.Trim(), target, StringComparison.OrdinalIgnoreCase)) add++;
                    }
                    if (add > 0)
                    {
                        if (gi >= goalProgress.Count) goalProgress.Add(0);
                        goalProgress[gi] += add;
                        if (logDebug) Debug.Log($"Goal {gi}: {goalProgress[gi]}/{g.targetCount} for '{target}'");
                    }
                }
                CheckObjectiveComplete();
                return;
            }
            // Legacy single-goal fallback
            if (currentLevel.type != ObjectiveType.ClearTheme || string.IsNullOrEmpty(currentLevel.themeName)) return;
            string targetLegacy = currentLevel.themeName.Trim();
            int addedLegacy = 0;
            for (int i = 0; i < infos.Count; i++)
            {
                var name = infos[i].themeName;
                if (!string.IsNullOrEmpty(name) && string.Equals(name.Trim(), targetLegacy, StringComparison.OrdinalIgnoreCase)) addedLegacy++;
            }
            if (addedLegacy > 0)
            {
                progressCount += addedLegacy;
                if (logDebug) Debug.Log($"Adventure progress: {progressCount}/{currentLevel.targetCount} for '{targetLegacy}'");
                CheckObjectiveComplete();
            }
        }

        private void HandleScoreChanged(int newScore)
        {
            latestScore = newScore;
            if (currentLevel == null) return;
            if (currentLevel.goals != null && currentLevel.goals.Count > 0)
            {
                CheckObjectiveComplete();
                return;
            }
            if (currentLevel.type == ObjectiveType.Score && newScore >= currentLevel.targetScore)
            {
                CheckObjectiveComplete(force: true);
            }
        }

        private void CheckObjectiveComplete(bool force = false)
        {
            if (currentLevel == null) return;
            bool complete = false;
            if (currentLevel.goals != null && currentLevel.goals.Count > 0)
            {
                int met = 0;
                for (int gi = 0; gi < currentLevel.goals.Count; gi++)
                {
                    var g = currentLevel.goals[gi];
                    bool ok = false;
                    if (g.type == ObjectiveType.Score)
                        ok = latestScore >= Mathf.Max(1, g.targetScore);
                    else if (g.type == ObjectiveType.ClearTheme)
                    {
                        int prog = (gi < goalProgress.Count) ? goalProgress[gi] : 0;
                        ok = prog >= Mathf.Max(1, g.targetCount);
                    }
                    if (ok) met++;
                }
                complete = (currentLevel.mode == GoalMode.All) ? (met == currentLevel.goals.Count) : (met > 0);
            }
            else
            {
                switch (currentLevel.type)
                {
                    case ObjectiveType.Score:
                        complete = force; // guarded by HandleScoreChanged
                        break;
                    case ObjectiveType.ClearTheme:
                        complete = progressCount >= Mathf.Max(1, currentLevel.targetCount);
                        break;
                }
            }
            if (!complete) return;

            // Advance to next level in order
            var total = weeklyLevelIndices.Count;
            int next = Mathf.Min(AdventureSession.CurrentLevelIndex + 1, total - 1);
            AdventureSession.CurrentLevelIndex = next;
            if (logDebug) Debug.Log($"Adventure level complete! Advancing to {next + 1}/{total}");

            // Reload scene to reset board and apply new pre-placements
            UnityEngine.SceneManagement.SceneManager.LoadScene("CoreGame");
        }
    }
}
