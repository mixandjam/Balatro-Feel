using UnityEngine;
using System.Collections;
using static DebugLogger;

public class GameBootstrap : MonoBehaviour {
    private static bool hasStartedInitialization = false;
    private bool isInitializing = false;

    private void Awake() {
        Log("GameBootstrap Awake", LogTag.Initialization);
    }

    private void Start() {
        Log("GameBootstrap Start called", LogTag.Initialization);
        if (!hasStartedInitialization && !isInitializing) {
            StartInitialization();
        }
    }

    private void StartInitialization() {
        hasStartedInitialization = true;
        isInitializing = true;
        Log("Starting initialization sequence", LogTag.Initialization);
        StartCoroutine(InitializeGameAsync());
    }

    private IEnumerator InitializeGameAsync() {
        // First ensure all singleton instances exist
        yield return StartCoroutine(WaitForSingletons());

        // Get references to all major components
        var initManager = InitializationManager.Instance;
        var gameReferences = GameReferences.Instance;
        var gameEvents = GameEvents.Instance;
        var gameManager = GameManager.Instance;
        var gameUI = GameUI.Instance;

        if (!ValidateComponents(initManager, gameReferences, gameEvents, gameManager, gameUI)) {
            Log("Failed to validate core components", LogTag.Initialization);
            yield break;
        }

        // Initialize InitializationManager first
        Log("Initializing InitializationManager", LogTag.Initialization);
        initManager.Initialize();
        yield return new WaitUntil(() => initManager.IsInitialized);
        yield return new WaitForSeconds(0.1f);

        // Initialize components in correct order
        // 1. GameReferences
        Log("Starting GameReferences initialization", LogTag.Initialization);
        gameReferences.Initialize();
        yield return new WaitUntil(() => gameReferences.IsInitialized);
        initManager.RegisterComponent(gameReferences);
        Log("GameReferences initialization completed", LogTag.Initialization);
        yield return new WaitForSeconds(0.1f);

        // 2. GameEvents
        Log("Starting GameEvents initialization", LogTag.Initialization);
        gameEvents.Initialize();
        yield return new WaitUntil(() => gameEvents.IsInitialized);
        initManager.RegisterComponent(gameEvents);
        Log("GameEvents initialization completed", LogTag.Initialization);
        yield return new WaitForSeconds(0.1f);

        // 3. GameManager
        Log("Starting GameManager initialization", LogTag.Initialization);
        gameManager.Initialize();
        yield return new WaitUntil(() => gameManager.IsInitialized);
        initManager.RegisterComponent(gameManager);
        Log("GameManager initialization completed", LogTag.Initialization);
        yield return new WaitForSeconds(0.1f);

        // 4. GameUI
        Log("Starting GameUI initialization", LogTag.Initialization);
        gameUI.Initialize();
        yield return new WaitUntil(() => gameUI.IsInitialized);
        initManager.RegisterComponent(gameUI);
        Log("GameUI initialization completed", LogTag.Initialization);

        // Signal that game is fully initialized
        Log("All components initialized successfully", LogTag.Initialization);
        isInitializing = false;
        GameEvents.Instance?.OnGameInitialized?.Invoke();
    }

    private bool ValidateComponents(params object[] components) {
        foreach (var component in components) {
            if (component == null) {
                LogError($"Critical component missing: {component?.GetType().Name ?? "Unknown"}",
                    LogTag.Initialization);
                return false;
            }
            Log($"Found component: {component.GetType().Name}", LogTag.Initialization);
        }
        return true;
    }

    private IEnumerator WaitForSingletons() {
        Log("Waiting for singleton instances...", LogTag.Initialization);
        float timeout = Time.time + 5f;
        bool allFound = false;

        while (Time.time < timeout && !allFound) {
            bool hasInitManager = InitializationManager.Instance != null;
            bool hasReferences = GameReferences.Instance != null;
            bool hasManager = GameManager.Instance != null;
            bool hasEvents = GameEvents.Instance != null;
            bool hasUI = GameUI.Instance != null;

            Log($"Checking singletons - InitManager: {hasInitManager}, References: {hasReferences}, " +
                $"Manager: {hasManager}, Events: {hasEvents}, UI: {hasUI}", LogTag.Initialization);

            allFound = hasInitManager && hasReferences && hasManager && hasEvents && hasUI;

            if (allFound) {
                Log("All singleton instances found", LogTag.Initialization);
                yield break;
            }

            yield return new WaitForSeconds(0.1f);
        }

        if (!allFound) {
            LogError("Timeout waiting for singletons!", LogTag.Initialization);
        }
    }

    // Reset static flag when entering play mode in editor
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() {
        hasStartedInitialization = false;
    }
}