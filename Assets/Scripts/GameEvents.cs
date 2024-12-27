using UnityEngine.Events;
using static DebugLogger;

public class GameEvents : Singleton<GameEvents> {
    // Game State Events
    public UnityEvent OnGameStateChanged { get; private set; }
    public UnityEvent OnGameInitialized { get; private set; }
    public UnityEvent<IPlayer> OnGameOver { get; private set; }
    public UnityEvent<IPlayer> OnTurnStarted { get; private set; }
    public UnityEvent<IPlayer> OnTurnEnded { get; private set; }
    // Combat Events
    public UnityEvent<ICreature, int> OnCreatureDamaged { get; private set; }
    public UnityEvent<ICreature> OnCreatureDied { get; private set; }
    public UnityEvent<IPlayer, int> OnPlayerDamaged { get; private set; }
    // Battlefield Events
    public UnityEvent<ICreature> OnCreaturePreSummon { get; private set; }
    public UnityEvent<ICreature, IPlayer> OnCreatureSummoned { get; private set; }
    public UnityEvent<ICreature, int> OnCreatureStatChanged { get; private set; }
    public UnityEvent<ICreature, int, int> OnCreaturePositionChanged { get; private set; }
    // Hand Events
    public UnityEvent<ICard, IPlayer> OnCardDrawn { get; private set; }
    public UnityEvent<ICard, IPlayer> OnCardPlayed { get; private set; }

    protected override void Awake() {
        Log("GameEvents Awake starting", LogTag.Initialization);
        base.Awake();
        if (this != Instance) return;

        InitializeEvents();
        Log("GameEvents Awake completed", LogTag.Initialization);
    }

    private void InitializeEvents() {
        Log("Initializing GameEvents", LogTag.Initialization);

        // Game State Events
        OnGameStateChanged = new UnityEvent();
        OnGameInitialized = new UnityEvent();
        OnGameOver = new UnityEvent<IPlayer>();
        OnTurnStarted = new UnityEvent<IPlayer>();
        OnTurnEnded = new UnityEvent<IPlayer>();

        // Combat Events
        OnCreatureDamaged = new UnityEvent<ICreature, int>();
        OnCreatureDied = new UnityEvent<ICreature>();
        OnPlayerDamaged = new UnityEvent<IPlayer, int>();

        // Battlefield Events
        OnCreaturePreSummon = new UnityEvent<ICreature>();
        OnCreatureSummoned = new UnityEvent<ICreature, IPlayer>();
        OnCreatureStatChanged = new UnityEvent<ICreature, int>();
        OnCreaturePositionChanged = new UnityEvent<ICreature, int, int>();

        // Hand Events
        OnCardDrawn = new UnityEvent<ICard, IPlayer>();
        OnCardPlayed = new UnityEvent<ICard, IPlayer>();

        Log("GameEvents initialization completed", LogTag.Initialization);
    }

    public override void Initialize() {
        if (IsInitialized) return;

        Log("GameEvents Initialize called", LogTag.Initialization);

        // Ensure events are initialized
        if (OnGameStateChanged == null) {
            InitializeEvents();
        }

        IsInitialized = true;
        Log("GameEvents Initialize completed", LogTag.Initialization);
    }

    protected override void OnDestroy() {
        if (this == Instance) {
            ClearAllListeners();
        }
        base.OnDestroy();
    }

    public void ClearAllListeners() {
        OnGameStateChanged?.RemoveAllListeners();
        OnGameInitialized?.RemoveAllListeners();
        OnGameOver?.RemoveAllListeners();
        OnTurnStarted?.RemoveAllListeners();
        OnTurnEnded?.RemoveAllListeners();
        OnCreatureDamaged?.RemoveAllListeners();
        OnCreatureDied?.RemoveAllListeners();
        OnPlayerDamaged?.RemoveAllListeners();
        OnCreaturePreSummon?.RemoveAllListeners();
        OnCreatureSummoned?.RemoveAllListeners();
        OnCreatureStatChanged?.RemoveAllListeners();
        OnCreaturePositionChanged?.RemoveAllListeners();
        OnCardDrawn?.RemoveAllListeners();
        OnCardPlayed?.RemoveAllListeners();
    }
}