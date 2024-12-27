using System.Linq;
using static DebugLogger;

public class GameManager : Singleton<GameManager> {
    public IPlayer Player1 { get; private set; }
    public IPlayer Player2 { get; private set; }
    public ActionsQueue ActionsQueue { get; private set; }
    private BattlefieldCombatHandler combatHandler;

    private GameReferences gameReferences;
    private GameEvents gameEvents;
    private ICardDealingService cardDealingService;
    private System.Random random = new System.Random();

    public BattlefieldCombatHandler CombatHandler => combatHandler;
    private bool isInitializing = false;

    protected override void Awake() {
        base.Awake();
        Log("GameManager Awake", LogTag.Initialization);
        var initManager = InitializationManager.Instance;
        if (initManager != null) {
            initManager.RegisterComponent(this);
        }
    }

    public override void Initialize() {
        if (IsInitialized || isInitializing) return;

        isInitializing = true;
        Log("Starting GameManager initialization", LogTag.Initialization);

        // Get and verify dependencies
        gameReferences = GameReferences.Instance;
        gameEvents = GameEvents.Instance;
        var initManager = InitializationManager.Instance;

        if (!ValidateDependencies()) {
            LogError("Cannot initialize GameManager - missing dependencies", LogTag.Initialization);
            isInitializing = false;
            return;
        }

        try {
            cardDealingService = new CardDealingService();
            combatHandler = new BattlefieldCombatHandler(this);
            ActionsQueue = new ActionsQueue(combatHandler);

            InitializeGameSystem();
            IsInitialized = true;
            Log("GameManager initialization completed successfully", LogTag.Initialization);
        } catch (System.Exception e) {
            LogError($"Error during GameManager initialization: {e}", LogTag.Initialization);
        } finally {
            isInitializing = false;
        }
    }

    private bool ValidateDependencies() {
        var initManager = InitializationManager.Instance;
        if (initManager == null) {
            LogError("InitializationManager missing", LogTag.Initialization);
            return false;
        }

        if (gameReferences == null) {
            LogError("GameReferences missing", LogTag.Initialization);
            return false;
        }

        if (!gameReferences.IsInitialized) {
            LogError("GameReferences not initialized", LogTag.Initialization);
            return false;
        }

        if (gameEvents == null) {
            LogError("GameEvents missing", LogTag.Initialization);
            return false;
        }

        if (!gameEvents.IsInitialized) {
            LogError("GameEvents not initialized", LogTag.Initialization);
            return false;
        }

        Log("All GameManager dependencies validated", LogTag.Initialization);
        return true;
    }

    private void InitializeGameSystem() {
        InitializePlayers();
        InitializeCards();
        PlaceInitialCreatures();
        SetupInitialGameState();
        SetupResolveButton();

        Log("Game systems initialized", LogTag.Initialization);
    }

    private void InitializePlayers() {
        Player1 = new Player("Player 1");
        Player2 = new Player("Player 2");
        Player1.Opponent = Player2;
        Player2.Opponent = Player1;

        Log("Players initialized", LogTag.Initialization);
    }

    private void InitializeCards() {
        var testSetup = gameObject.AddComponent<TestSetup>();
        var testCards = testSetup.CreateTestCards();
        cardDealingService.InitializeDecks(testCards, testCards);
    }

    private void PlaceInitialCreatures() {
        var testSetup = gameObject.AddComponent<TestSetup>();
        var availableCreatures = testSetup.CreateTestCards()
            .Where(card => card is CreatureData)
            .Cast<CreatureData>()
            .ToList();

        // Place creatures for Player 1
        for (int i = 0; i < 2; i++) {
            int randomIndex = random.Next(availableCreatures.Count);
            var creatureData = availableCreatures[randomIndex];
            var creature = CardFactory.CreateCard(creatureData) as Creature;
            if (creature != null) {
                creature.SetOwner(Player1);
                ((Player)Player1).AddToBattlefield(creature, i);
                Log($"Added {creature.Name} to Player 1's battlefield slot {i} with {creature.Effects.Count} effects",
                    LogTag.Creatures | LogTag.Initialization);
            }
        }

        // Place creatures for Player 2
        for (int i = 0; i < 2; i++) {
            int randomIndex = random.Next(availableCreatures.Count);
            var creatureData = availableCreatures[randomIndex];
            var creature = CardFactory.CreateCard(creatureData) as Creature;
            if (creature != null) {
                creature.SetOwner(Player2);
                ((Player)Player2).AddToBattlefield(creature, i);
                Log($"Added {creature.Name} to Player 2's battlefield slot {i} with {creature.Effects.Count} effects",
                    LogTag.Creatures | LogTag.Initialization);
            }
        }
    }

    private void SetupInitialGameState() {
        cardDealingService.DealInitialHands(Player1, Player2);
        LogGameState("Game Initialized");
        gameEvents.OnGameInitialized.Invoke();
        gameEvents.OnGameStateChanged.Invoke();
    }

    private void SetupResolveButton() {
        var resolveButton = gameReferences.GetResolveActionsButton();
        if (resolveButton != null) {
            resolveButton.onClick.AddListener(OnResolveButtonClicked);
        }
    }

    private void OnResolveButtonClicked() {
        if (ActionsQueue != null) {
            ActionsQueue.ResolveActions();
        }
    }

    public void PlayCard(CardData cardData, IPlayer player) {
        if (!IsInitialized) {
            LogError("Cannot play card - GameManager not initialized", LogTag.Actions | LogTag.Initialization);
            return;
        }

        ICard card = CardFactory.CreateCard(cardData);
        card.Play(player, ActionsQueue);
        gameEvents.OnGameStateChanged.Invoke();
    }

    public void ResolveActions() {
        if (!IsInitialized) {
            LogError("Cannot resolve actions - GameManager not initialized", LogTag.Actions | LogTag.Initialization);
            return;
        }

        ActionsQueue.ResolveActions();
        LogGameState("Actions resolved");
    }

    private void LogGameState(string action = "") {
        if (!IsInitialized) return;

        var state = $"=== Game State {(string.IsNullOrEmpty(action) ? "" : $"- {action}")} ===\n";

        if (Player1 != null) {
            state += $"Player 1 - Health: {Player1.Health}, Hand: {Player1.Hand.Count}, Battlefield: {Player1.Battlefield.Count}\n";
        }

        if (Player2 != null) {
            state += $"Player 2 - Health: {Player2.Health}, Hand: {Player2.Hand.Count}, Battlefield: {Player2.Battlefield.Count}\n";
        }

        if (ActionsQueue != null) {
            state += $"Pending Actions: {ActionsQueue.GetPendingActionsCount()}\n";
        }

        Log(state, LogTag.Players | LogTag.Creatures | LogTag.Actions);
    }

    protected override void OnDestroy() {
        var resolveButton = gameReferences?.GetResolveActionsButton();
        if (resolveButton != null) {
            resolveButton.onClick.RemoveListener(OnResolveButtonClicked);
        }

        if (ActionsQueue != null) {
            ActionsQueue.Cleanup();
        }

        base.OnDestroy();
    }
}