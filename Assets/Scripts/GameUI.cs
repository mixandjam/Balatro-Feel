using static DebugLogger;
using UnityEngine;

public class GameUI : UIComponent {
    private static GameUI instance;
    public static GameUI Instance => instance;

    private PlayerUI player1UI;
    private PlayerUI player2UI;
    private BattlefieldUI player1BattlefieldUI;
    private BattlefieldUI player2BattlefieldUI;
    private bool hasInitializedSubComponents = false;

    protected override void Awake() {
        base.Awake();
        if (instance != null && instance != this) {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    protected override bool CheckDependencies() {
        if (!base.CheckDependencies()) return false;

        // Additional checks specific to GameUI
        if (gameManager.Player1 == null || gameManager.Player2 == null) {
            LogWarning("Players not initialized in GameManager", LogTag.Initialization);
            return false;
        }

        return true;
    }

    public override void Initialize() {
        if (IsInitialized) return;

        if (!CheckDependencies()) {
            return;
        }

        Log("Initializing GameUI", LogTag.Initialization);
        GetReferences();
        InitializeUI();
        SubscribeToEvents();

        IsInitialized = true;
        Log("GameUI initialized successfully", LogTag.Initialization);
    }

    private void GetReferences() {
        player1UI = gameReferences.GetPlayer1UI();
        player2UI = gameReferences.GetPlayer2UI();
        player1BattlefieldUI = gameReferences.GetPlayer1BattlefieldUI();
        player2BattlefieldUI = gameReferences.GetPlayer2BattlefieldUI();

        if (player1UI == null || player2UI == null ||
            player1BattlefieldUI == null || player2BattlefieldUI == null) {
            LogError("Missing UI component references", LogTag.Initialization);
        }
    }

    private void InitializeUI() {
        if (hasInitializedSubComponents) return;

        // First initialize the player UIs
        if (player1UI != null) {
            // First set the player
            player1UI.SetPlayer(gameManager.Player1);
            // Then initialize
            player1UI.Initialize();
            Log("Player1UI initialized", LogTag.Initialization);
        }

        if (player2UI != null) {
            player2UI.SetPlayer(gameManager.Player2);
            player2UI.Initialize();
            Log("Player2UI initialized", LogTag.Initialization);
        }

        // Give a frame for player UIs to initialize
        StartCoroutine(InitializeBattlefields());
        hasInitializedSubComponents = true;
    }

    private System.Collections.IEnumerator InitializeBattlefields() {
        yield return new WaitForEndOfFrame();

        // Then initialize battlefields
        if (player1BattlefieldUI != null) {
            player1BattlefieldUI.Initialize(gameManager.Player1);
            Log("Player1BattlefieldUI initialized", LogTag.Initialization);
        }

        if (player2BattlefieldUI != null) {
            player2BattlefieldUI.Initialize(gameManager.Player2);
            Log("Player2BattlefieldUI initialized", LogTag.Initialization);
        }

        gameEvents?.OnGameStateChanged?.Invoke();
    }

    protected override void SubscribeToEvents() {
        base.SubscribeToEvents();
        if (gameEvents != null) {
            gameEvents.OnGameStateChanged.AddListener(HandleGameStateChanged);
            gameEvents.OnGameInitialized.AddListener(HandleGameInitialized);
        }
    }

    protected override void UnsubscribeFromEvents() {
        base.UnsubscribeFromEvents();
        if (gameEvents != null) {
            gameEvents.OnGameStateChanged.RemoveListener(HandleGameStateChanged);
            gameEvents.OnGameInitialized.RemoveListener(HandleGameInitialized);
        }
    }

    private void HandleGameStateChanged() {
        UpdateUI();
    }

    private void HandleGameInitialized() {
        if (!hasInitializedSubComponents) {
            InitializeUI();
        }
        UpdateUI();
    }

    public override void UpdateUI() {
        if (!IsInitialized) return;

        if (player1UI != null && player1UI.IsInitialized)
            player1UI.UpdateUI();

        if (player2UI != null && player2UI.IsInitialized)
            player2UI.UpdateUI();

        if (player1BattlefieldUI != null && player1BattlefieldUI.IsInitialized)
            player1BattlefieldUI.UpdateUI();

        if (player2BattlefieldUI != null && player2BattlefieldUI.IsInitialized)
            player2BattlefieldUI.UpdateUI();
    }

    protected override void OnDestroy() {
        if (instance == this) {
            instance = null;
        }
        base.OnDestroy();
    }
}