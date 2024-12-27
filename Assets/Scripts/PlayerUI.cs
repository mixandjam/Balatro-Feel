using UnityEngine;
using static DebugLogger;

public class PlayerUI : UIComponent {
    private IPlayer player;
    private HandUI handUI;
    private bool isInitializingUI = false;

    public override void Initialize() {
        if (IsInitialized) return;

        if (!InitializationManager.Instance.IsComponentInitialized<GameManager>()) {
            LogWarning("GameManager not initialized yet, will retry later", LogTag.Initialization);
            return;
        }

        SubscribeToEvents();
        IsInitialized = true;
        Log($"PlayerUI base initialization complete", LogTag.Initialization);
    }

    public void SetPlayer(IPlayer newPlayer) {
        if (isInitializingUI) return;

        this.player = newPlayer;
        if (player == null) {
            LogError("Cannot set null player", LogTag.Initialization);
            return;
        }

        InitializeUI();
        Log($"Player set for {(player.IsPlayer1() ? "Player 1" : "Player 2")}", LogTag.Initialization);
    }

    private void InitializeUI() {
        if (isInitializingUI) return;
        isInitializingUI = true;

        try {
            InitializeHandUI();
            InitializeHealthHandler();
            Log($"PlayerUI initialized for {(player.IsPlayer1() ? "Player 1" : "Player 2")}", LogTag.Initialization);
            UpdateUI();
        } finally {
            isInitializingUI = false;
        }
    }

    private void InitializeHandUI() {
        if (player == null) {
            LogError("Cannot initialize HandUI - Player is null", LogTag.Initialization);
            return;
        }

        if (gameReferences == null) {
            LogError("Cannot initialize HandUI - GameReferences is null", LogTag.Initialization);
            return;
        }

        // Get the correct HandUI reference based on player
        handUI = player.IsPlayer1() ?
            gameReferences.GetPlayer1HandUI() :
            gameReferences.GetPlayer2HandUI();

        if (handUI != null) {
            handUI.Initialize(player);
            Log($"HandUI initialized for {(player.IsPlayer1() ? "Player 1" : "Player 2")}", LogTag.Initialization);
        } else {
            LogError($"HandUI reference missing for {(player.IsPlayer1() ? "Player 1" : "Player 2")}",
                LogTag.UI | LogTag.Initialization);
        }
    }

    private void InitializeHealthHandler() {
        if (player == null) {
            LogError("Cannot initialize HealthHandler - Player is null", LogTag.Initialization);
            return;
        }

        var healthText = player.IsPlayer1() ?
            gameReferences.GetPlayer1HealthText() :
            gameReferences.GetPlayer2HealthText();

        if (healthText == null) {
            LogError($"Health text reference missing for {(player.IsPlayer1() ? "Player 1" : "Player 2")}",
                LogTag.UI | LogTag.Initialization);
        }
    }

    protected override void SubscribeToEvents() {
        base.SubscribeToEvents();
        if (gameEvents != null) {
            gameEvents.OnGameStateChanged.AddListener(UpdateUI);
            gameEvents.OnPlayerDamaged.AddListener(OnPlayerDamaged);
        }
    }

    protected override void UnsubscribeFromEvents() {
        base.UnsubscribeFromEvents();
        if (gameEvents != null) {
            gameEvents.OnGameStateChanged.RemoveListener(UpdateUI);
            gameEvents.OnPlayerDamaged.RemoveListener(OnPlayerDamaged);
        }
    }

    public void OnPlayerDamaged(IPlayer damagedPlayer, int damage) {
        if (player != null && damagedPlayer == player) {
            UpdateUI();
        }
    }

    public override void UpdateUI() {
        if (!IsInitialized || player == null) return;

        if (handUI != null && handUI.IsInitialized) {
            handUI.UpdateUI();
        }
    }

    protected override void OnDestroy() {
        UnsubscribeFromEvents();
        base.OnDestroy();
    }
}