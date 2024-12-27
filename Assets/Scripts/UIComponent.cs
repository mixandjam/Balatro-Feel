using UnityEngine;
using System.Collections;
using static DebugLogger;

public abstract class UIComponent : InitializableComponent {
    private bool isInitializationPending = false;
    private const float INITIALIZATION_TIMEOUT = 10f; // Increased timeout
    private const float RETRY_INTERVAL = 0.2f;
    private const int MAX_RETRIES = 3;
    private int currentRetry = 0;

    protected GameManager gameManager => GameManager.Instance;
    protected GameReferences gameReferences => GameReferences.Instance;
    protected GameEvents gameEvents => GameEvents.Instance;

    protected virtual void Start() {
        if (!IsInitialized && !isInitializationPending) {
            isInitializationPending = true;
            StartCoroutine(WaitForInitialization());
        }
    }

    private IEnumerator WaitForInitialization() {
        while (InitializationManager.Instance == null) {
            yield return new WaitForSeconds(0.1f);
        }

        float startTime = Time.time;
        bool success = false;

        while (Time.time - startTime < INITIALIZATION_TIMEOUT && currentRetry < MAX_RETRIES) {
            if (CheckDependencies()) {
                Initialize();
                if (IsInitialized) {
                    success = true;
                    break;
                }
            }

            // Log what dependencies are missing
            LogDependencyStatus();

            currentRetry++;
            yield return new WaitForSeconds(RETRY_INTERVAL);
        }

        isInitializationPending = false;

        if (!success) {
            LogError($"{GetType().Name} initialization timed out after {currentRetry} retries!",
                LogTag.Initialization);
        }
    }

    private void LogDependencyStatus() {
        var initManager = InitializationManager.Instance;
        if (initManager == null) return;

        bool hasRefs = initManager.IsComponentInitialized<GameReferences>();
        bool hasEvents = initManager.IsComponentInitialized<GameEvents>();
        bool hasManager = initManager.IsComponentInitialized<GameManager>();

        Log($"{GetType().Name} waiting for dependencies - References: {hasRefs}, " +
            $"Events: {hasEvents}, Manager: {hasManager} (Retry {currentRetry + 1}/{MAX_RETRIES})",
            LogTag.Initialization);
    }

    protected virtual bool CheckDependencies() {
        var initManager = InitializationManager.Instance;
        if (initManager == null) return false;

        return initManager.IsComponentInitialized<GameReferences>() &&
               initManager.IsComponentInitialized<GameEvents>() &&
               initManager.IsComponentInitialized<GameManager>();
    }

    public override void Initialize() {
        if (IsInitialized) return;

        if (!CheckDependencies()) {
            return;
        }

        Log($"Initializing {GetType().Name}", LogTag.Initialization);
        SubscribeToEvents();
        UpdateUI();
        IsInitialized = true;
        Log($"{GetType().Name} initialized successfully", LogTag.Initialization);
    }

    protected virtual void SubscribeToEvents() {
        if (gameEvents != null) {
            gameEvents.OnGameStateChanged.AddListener(UpdateUI);
        }
    }

    protected virtual void UnsubscribeFromEvents() {
        if (gameEvents != null) {
            gameEvents.OnGameStateChanged.RemoveListener(UpdateUI);
        }
    }

    protected virtual void OnDestroy() {
        UnsubscribeFromEvents();
    }

    public abstract void UpdateUI();
}