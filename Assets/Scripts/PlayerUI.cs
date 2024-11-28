using UnityEngine;

public class PlayerUI : MonoBehaviour {
    private IPlayer player;
    private GameMediator gameMediator;
    private bool isInitialized;
    private GameReferences gameReferences;
    private HandUI handUI;
    private HealthUI healthUI;

    public void Initialize(IPlayer player) {
        this.player = player;

        InitializeDependencies();

        if (gameReferences == null) {
            Debug.LogError("GameReferences not found during PlayerUI initialization");
            return;
        }

        InitializeHealthUI();
        InitializeHandUI();
        RegisterEvents();

        isInitialized = true;
    }

    private void InitializeDependencies() {
        gameMediator = GameMediator.Instance;
        gameReferences = GameReferences.Instance;
    }

    private void InitializeHealthUI() {
        if (player == null) {
            Debug.LogError("Player is null on PlayerUI");
            return;
        }

        if (healthUI == null) {
            healthUI = gameObject.AddComponent<HealthUI>();
            var healthText = player.IsPlayer1() ?
                gameReferences.GetPlayer1HealthText() :
                gameReferences.GetPlayer2HealthText();

            if (healthText == null) {
                Debug.LogError("Health text reference missing for " +
                    (player.IsPlayer1() ? "Player 1" : "Player 2"));
                return;
            }

            healthUI.Initialize(healthText, player);
        }
    }

    private void InitializeHandUI() {
        if (player == null) {
            Debug.Log("Player is null on PlayerUI");
        }

        if (handUI == null) {
            handUI = gameObject.AddComponent<HandUI>();
            handUI.Initialize(player);
        }
    }

    private void RegisterEvents() {
        if (gameMediator != null) {
            gameMediator.AddGameStateChangedListener(UpdateUI);
            gameMediator.AddPlayerDamagedListener(HandlePlayerDamaged);
        }
    }

    private void UnregisterEvents() {
        if (gameMediator != null) {
            gameMediator.RemoveGameStateChangedListener(UpdateUI);
            gameMediator.RemovePlayerDamagedListener(HandlePlayerDamaged);
        }
    }

    public void SetPlayer(IPlayer player) {
        this.player = player;
        if (handUI != null) {
            handUI.Initialize(player);
        }
        if (healthUI != null) {
            healthUI.Initialize(gameReferences.GetPlayer1HealthText(), player);
        }
        isInitialized = player != null;
        UpdateUI();
    }

    public void UpdateUI() {
        if (!isInitialized || player == null) return;
        healthUI?.UpdateUI();
        handUI?.UpdateUI();
    }

    private void HandlePlayerDamaged(IPlayer damagedPlayer, int damage) {
        if (damagedPlayer == player) {
            healthUI?.UpdateUI();
        }
    }

    private void OnDestroy() {
        UnregisterEvents();
    }
}