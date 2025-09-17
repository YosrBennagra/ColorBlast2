using UnityEngine;
using System.Collections.Generic;
using ColorBlast.Game;

[ExecuteAlways]
public partial class ShapeSpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    [SerializeField] private GameObject[] shapePrefabs;
    [SerializeField] private Transform[] spawnPoints = new Transform[3];
    [SerializeField] private bool autoSpawnOnStart = true;
    
    [Header("Hierarchy Management")]
    [Tooltip("Automatically create or re-use a parent transform for spawned shapes.")]
    [SerializeField] private bool autoCreateShapesParent = true;
    [SerializeField] private Transform shapesParent;
    [SerializeField] private string shapesParentName = "Shap Partial";

    [Header("Adaptive Assist")]
    [Range(0f, 1f)]
    [SerializeField] private float assistLevel = 0.6f;
    [Tooltip("Dynamically adjust assist based on board state and tempo.")]
    [SerializeField] private bool adaptiveAssistMode = true;
    [SerializeField, Range(0f, 1f)] private float minAssist = 0.25f;
    [SerializeField, Range(0f, 1f)] private float maxAssist = 0.95f;

    [Header("Editor Preview & Layout")]
    [SerializeField] private bool alignSpawnPointsVertically = true;
    [SerializeField] private float verticalSpacing = 2f;
    [SerializeField] private float alignAtX = 0f;
    [SerializeField] private bool alignSpawnPointsHorizontally = false;
    [SerializeField] private float horizontalSpacing = 2f;
    [SerializeField] private float alignAtY = 0f;
    [SerializeField] private bool showSpawnGizmos = true;
    [SerializeField] private Color spawnGizmoColor = new Color(0.3f, 0.9f, 1f, 0.6f);
    [SerializeField] private Vector2 previewSize = new Vector2(2f, 2f);
    [SerializeField] private bool centerSpawnedShapesInGizmo = true;
    [SerializeField] private bool autoKeepTrayOutsideGrid = true;
    [SerializeField] private float trayMarginFromGrid = 0.25f;

    [Header("Sprite Theme Settings")]
    [SerializeField] private ShapeSpriteManager spriteManager;
    [SerializeField] private bool useRandomThemes = true;

    [Header("Selection Mode")]
    [SerializeField] private bool useDeterministicSelection = true;
    [SerializeField] private bool preventDuplicatesInSet = true;
    [SerializeField] private bool ensurePlaceableInSet = true;
    [SerializeField, Min(0)] private int noRepeatWindow = 4;
    [SerializeField] private bool preventSameFamilyInSet = true;
    [SerializeField] private string[] shapeFamilyLabels = new string[0];
    [SerializeField] private bool randomizeInitialDeterministicBag = true;
    [SerializeField] private bool randomizeInitialCursor = true;
    [Tooltip("Compose the set of 3 using board-aware heuristics.")]
    [SerializeField] private bool useAdaptiveSetComposition = true;

    [Header("Shape Variants (Orientation)")]
    [SerializeField] private bool enableOrientationVariants = true;
    [SerializeField] private bool allowRotate90 = true;
    [SerializeField] private bool allowRotate180 = true;
    [SerializeField] private bool allowRotate270 = true;
    [SerializeField] private bool allowMirrorX = true;
    [SerializeField] private bool allowMirrorY = false;

    [Header("Spawn Effects")]
    [SerializeField] private float spawnEffectDuration = 0.3f;
    [SerializeField] private float minSpawnScale = 0.1f;
    [SerializeField] private float maxSpawnScale = 1.0f;
    [SerializeField] private bool clampSpawnEffectToPreview = true;

    [Header("Legacy Settings")]
    [SerializeField] private float spawnCheckInterval = 2f;

    [Header("Shape Size Control")]
    [SerializeField] private Vector3 globalShapeScale = Vector3.one;
    [SerializeField] private Vector3[] perSlotScale = new Vector3[3];
    [SerializeField] private bool spawnEffectFromBaseScale = true;
    [SerializeField] private bool fitShapesToPreviewBox = true;
    [SerializeField] private float previewPadding = 0.05f;
    [SerializeField] private bool enforceMinSpacingFromPreview = true;
    [SerializeField] private float spacingMargin = 0.1f;
    [SerializeField] private bool setSpawnSortingOrders = true;
    [SerializeField] private int spawnSortingOrderBase = 0;
    [SerializeField] private int spawnSortingOrderStep = 1;

    [Header("Dead-end Forgiveness")]
    [Tooltip("Automatically reroll tray when no valid moves for a short duration.")]
    [SerializeField] private bool enableAutoRerollOnDeadEnd = true;
    [SerializeField] private float deadEndRerollDelay = 2.0f;
    [SerializeField, Min(0)] private int maxRerollsPerSession = 2;
    [Tooltip("Immediately reroll the tray once if the freshly spawned set has no valid moves (after orientation variants are applied).")]
    [SerializeField] private bool guaranteeMoveOnSpawn = true;
    [Tooltip("How many immediate reroll attempts are allowed per spawn to ensure at least one valid move.")]
    [SerializeField, Min(0)] private int maxImmediateRerollAttempts = 1;

    [Header("Challenge Rounds")]
    [Tooltip("Occasionally present a set that forces a meaningful choice (overlapping best placements, scarcity, etc.)")]
    [SerializeField] private bool enableChallengeRounds = true;
    [Tooltip("How often to trigger a challenge set (every N spawns). 0 disables cadence-based triggering.")]
    [SerializeField, Min(0)] private int challengeEveryNSets = 5;
    [Tooltip("Minimum number of overlapping cells between two pieces' best placements to count as a challenge pair.")]
    [SerializeField, Min(0)] private int minChallengeOverlapCells = 1;
    [Tooltip("Prefer picking the smallest third piece to increase tactical tension.")]
    [SerializeField] private bool challengePreferSmallThird = true;
    [Tooltip("Optionally relax assist during challenge sets to avoid too-helpful picks.")]
    [SerializeField] private bool challengeRelaxAssist = true;
    [SerializeField, Range(0f, 1f)] private float challengeAssistFloor = 0.2f;

    // Runtime state
    private GameObject[] currentShapes = new GameObject[3];
    private bool allShapesPlaced = false;
    private float lastCheckTime = 0f;
    private bool[] shapeStatusCache = new bool[3];
    [SerializeField] private bool centerByRenderers = true;

    // Deterministic bag state
    private readonly List<int> bag = new List<int>();
    private int bagCursor = 0;
    private readonly Queue<int> recentIndices = new Queue<int>();
    private readonly Queue<int> deferredIndices = new Queue<int>();
    private bool initialBagRandomized = false;

    // Flow & pacing
    private float lastSpawnTime;
    private int setsClearedStreak = 0;
    private int bestStreak = 0;
    private float noMoveTimer = 0f;
    private int rerollsUsed = 0;
    private int setsSpawnedCount = 0;
    private int immediateSpawnRerollAttempts = 0;

    // Perf: cache expensive board-wide checks briefly
    private float lastValidMoveCheckTime = -999f;
    private bool lastHasAnyValidMove = true;

    [Header("Difficulty")]
    [Tooltip("0 = easiest, 1 = hardest. Affects set composition, challenge frequency, and perfect-clear generosity.")]
    [SerializeField, Range(0f, 1f)] private float difficulty = 0.7f;
    [Tooltip("Gradually ramp difficulty up over time.")]
    [SerializeField] private bool rampDifficultyOverTime = true;
    [Tooltip("Number of spawned sets to reach max ramp contribution.")]
    [SerializeField, Min(1)] private int setsToReachMaxDifficulty = 20;

    [Header("Perfect Clear Opportunities")]
    [Tooltip("Occasionally, if a single placement can clear the entire board, include that exact shape in the tray.")]
    [SerializeField] private bool enablePerfectClearOpportunities = true;
    [Tooltip("Chance to inject a perfect-clear shape when an opportunity exists.")]
    [SerializeField, Range(0f,1f)] private float perfectClearChance = 0.35f;
    [Tooltip("Only search when the board has at least this many occupied cells (avoids trivial empty-board checks).")]
    [SerializeField, Min(0)] private int perfectClearMinOccupied = 6;
    [Tooltip("For a perfect-clear piece, keep its identity orientation (no rotate/mirror) to preserve the opportunity.")]
    [SerializeField] private bool perfectClearKeepIdentityOrientation = true;

    // Runtime marks for the last injected perfect-clear piece
    private int lastPerfectClearSlot = -1;
    private int lastPerfectClearIndex = -1;

    [Header("Early Big-Shape Surprises")]
    [Tooltip("During the first few sets, very rarely inject one large piece to spice up early decisions.")]
    [SerializeField] private bool enableEarlyBigShapeSurprises = true;
    [Tooltip("How many initial sets are considered the 'beginning' window.")]
    [SerializeField, Min(1)] private int earlyBigShapeSetsWindow = 5;
    [Tooltip("Chance per spawn to inject a big shape during the early window (rare).")]
    [SerializeField, Range(0f,1f)] private float earlyBigShapeChance = 0.06f;
    [Tooltip("Minimum tile count to consider a shape 'big'.")]
    [SerializeField, Min(1)] private int earlyBigShapeMinTiles = 5;
    [Tooltip("Prefer big shapes that are actually placeable on the current board.")]
    [SerializeField] private bool earlyBigShapePreferPlaceable = true;
}
