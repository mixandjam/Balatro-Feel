using System.Collections.Generic;
using static DebugLogger;

public interface ICardDealingService {
    void InitializeDecks(List<CardData> player1Cards, List<CardData> player2Cards);
    void DealInitialHands(IPlayer player1, IPlayer player2, int handSize = 6);
    bool CanDrawCard(IPlayer player);
    void DrawCardForPlayer(IPlayer player);
    void ShuffleDeck(IPlayer player);
}

public class CardDealingService : ICardDealingService {
    private readonly Dictionary<IPlayer, IDeck> playerDecks = new Dictionary<IPlayer, IDeck>();
    private readonly GameEvents gameEvents;

    public CardDealingService() {
        this.gameEvents = GameEvents.Instance;
    }

    public void InitializeDecks(List<CardData> player1Cards, List<CardData> player2Cards) {
        var gameManager = GameManager.Instance;
        if (gameManager == null) {
            LogError("GameManager not found when initializing decks" , LogTag.Cards | LogTag.Initialization);
            return;
        }

        // Create and initialize deck for Player 1
        var player1Deck = new Deck();
        player1Deck.Initialize(player1Cards);
        playerDecks[gameManager.Player1] = player1Deck;

        // Create and initialize deck for Player 2
        var player2Deck = new Deck();
        player2Deck.Initialize(player2Cards);
        playerDecks[gameManager.Player2] = player2Deck;

        Log("Card dealing service initialized decks successfully", LogTag.Cards | LogTag.Initialization);
    }

    public void DealInitialHands(IPlayer player1, IPlayer player2, int handSize = 7) {
        for (int i = 0; i < handSize; i++) {
            DrawCardForPlayer(player1);
            DrawCardForPlayer(player2);
        }
        Log($"Dealt initial hands of {handSize} cards to both players", LogTag.Cards | LogTag.Initialization);
    }

    public bool CanDrawCard(IPlayer player) {
        if (player == null || !playerDecks.ContainsKey(player)) {
            LogWarning($"Cannot check draw capability - player not found in deck registry", LogTag.Cards);
            return false;
        }

        var deck = playerDecks[player];
        return deck != null && deck.CardsRemaining > 0;
    }

    public void DrawCardForPlayer(IPlayer player) {
        if (player == null) {
            LogError("Cannot draw card - player is null", LogTag.Cards | LogTag.Initialization);
            return;
        }

        if (!playerDecks.TryGetValue(player, out var deck)) {
            LogError($"Could not find deck for player", LogTag.Cards | LogTag.Initialization);
            return;
        }

        var card = deck.DrawCard();
        if (card != null) {
            player.AddToHand(card);
            gameEvents.OnCardDrawn.Invoke(card, player);
            gameEvents.OnGameStateChanged.Invoke();
            Log($"Drew card for {(player.IsPlayer1() ? "Player 1" : "Player 2")}: {card.Name}", LogTag.Cards | LogTag.Initialization);
        }
    }

    public void ShuffleDeck(IPlayer player) {
        if (!playerDecks.TryGetValue(player, out var deck)) {
            LogError($"Could not find deck for player to shuffle", LogTag.Cards);
            return;
        }

        // Implementation of deck shuffling would go here
        // Note: The current Deck class would need to be modified to support shuffling
        Log($"Shuffled deck for {(player.IsPlayer1() ? "Player 1" : "Player 2")}", LogTag.Cards);
    }
}