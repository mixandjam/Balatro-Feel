using System.Collections.Generic;
using static DebugLogger;

public interface IDeck {
    int CardsRemaining { get; }
    void Initialize(List<CardData> cardDataList);
    ICard DrawCard();
    void AddCardToTop(ICard card);
    void AddCardToBottom(ICard card);
}

public class Deck : IDeck {
    private Queue<ICard> cards;
    public int CardsRemaining => cards?.Count ?? 0;

    public Deck() {
        cards = new Queue<ICard>();
    }

    public void Initialize(List<CardData> cardDataList) {
        if (cardDataList == null || cardDataList.Count == 0) {
            LogError("Attempted to initialize deck with null or empty card list", LogTag.Cards | LogTag.Initialization);
            return;
        }

        cards.Clear();
        foreach (var cardData in cardDataList) {
            Log($"Creating card {cardData.cardName} with {cardData.effects.Count} effects", LogTag.Cards | LogTag.Initialization);
            var card = CardFactory.CreateCard(cardData);
            if (card != null) {
                Log($"Added {card.Name} to deck with {card.Effects.Count} effects", LogTag.Cards | LogTag.Initialization);
            }
            cards.Enqueue(card);
        }
        Log($"Initialized with {cards.Count} cards", LogTag.Cards | LogTag.Initialization);
    }

    public ICard DrawCard() {
        if (cards.Count == 0) {
            LogWarning("Attempted to draw from empty deck", LogTag.Cards);
            return null;
        }
        var drawnCard = cards.Dequeue();
        return drawnCard;
    }

    public void AddCardToTop(ICard card) {
        if (card == null) {
            LogError("Attempted to add null card to deck", LogTag.Cards);
            return;
        }
        var tempList = new List<ICard>(cards);
        tempList.Add(card);
        cards = new Queue<ICard>(tempList);
        Log($"Added card to top: {card.Name}", LogTag.Cards);
    }

    public void AddCardToBottom(ICard card) {
        if (card == null) {
            LogError("Attempted to add null card to deck", LogTag.Cards);
            return;
        }
        cards.Enqueue(card);
        Log($"Added card to bottom: {card.Name}", LogTag.Cards);
    }
}