using static DebugLogger;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using System.Linq;

public static class CardFactory {
    private static Dictionary<string, CardData> cardDataCache = new Dictionary<string, CardData>();

    public static ICard CreateCard(CardData cardData) {
        if (cardData == null) return null;

        Log($"Creating card: {cardData.cardName} with {cardData.effects.Count} effects", LogTag.Cards | LogTag.Initialization);

        ICard card = null;
        switch (cardData) {
            case CreatureData creatureData:
                var creature = new Creature(creatureData.cardName, creatureData.attack, creatureData.health);
                foreach (var effect in cardData.effects) {
                    Log($"Adding effect - Trigger: {effect.trigger}, Actions: {effect.actions.Count}", LogTag.Cards | LogTag.Effects);
                    var newEffect = new CardEffect {
                        effectType = effect.effectType,
                        trigger = effect.trigger,
                        actions = effect.actions.Select(a => new EffectAction {
                            actionType = a.actionType,
                            value = a.value,
                            targetType = a.targetType
                        }).ToList()
                    };
                    creature.Effects.Add(newEffect);
                }
                card = creature;
                break;
        }

        if (card != null) {
            Log($"Successfully created {card.Name} with {card.Effects.Count} effects", LogTag.Cards | LogTag.Initialization);
        }

        return card;
    }

    public static CardController CreateCardController(ICard cardData, IPlayer owner, Transform parent, GameReferences gameReferences) {
        var cardPrefab = gameReferences.GetCardPrefab();
        if (cardPrefab == null) {
            LogError("Failed to get card prefab from game references", LogTag.Cards | LogTag.Initialization);
            return null;
        }

        var cardObj = GameObject.Instantiate(cardPrefab, parent);
        var controller = cardObj.GetComponent<CardController>();
        if (controller != null) {
            var data = CreateCardDataFromCard(cardData);
            controller.Initialize(data, owner);
            Log($"Created card controller for {cardData.Name}", LogTag.Cards | LogTag.Initialization);
        }
        return controller;
    }

    private static CardData CreateCardDataFromCard(ICard card) {
        var cardData = ScriptableObject.CreateInstance<CreatureData>();
        cardData.cardName = card.Name;

        if (card is ICreature creature) {
            cardData.attack = creature.Attack;
            cardData.health = creature.Health;
        }

        return cardData;
    }

    public static CardData GetOrCreateCardData(string name, Action<CardData> setup) {
        if (cardDataCache.TryGetValue(name, out var existingData)) {
            Log($"Retrieved cached card data for {name}", LogTag.Cards);
            return existingData;
        }

        var newData = ScriptableObject.CreateInstance<CreatureData>();
        setup(newData);
        cardDataCache[name] = newData;
        Log($"Created new card data for {name}", LogTag.Cards | LogTag.Initialization);
        return newData;
    }

    public static void SetupCardEventHandlers(
        CardController controller,
        UnityAction<CardController> onBeginDrag = null,
        UnityAction<CardController> onEndDrag = null,
        UnityAction<CardController> onDrop = null,
        UnityAction<CardController> onPointerEnter = null,
        UnityAction<CardController> onPointerExit = null) {
        if (controller == null) return;

        CleanupCardEventHandlers(controller);

        if (onBeginDrag != null)
            controller.OnBeginDragEvent.AddListener(onBeginDrag);

        if (onEndDrag != null)
            controller.OnEndDragEvent.AddListener(onEndDrag);

        if (onDrop != null)
            controller.OnCardDropped.AddListener(onDrop);

        if (onPointerEnter != null)
            controller.OnPointerEnterEvent.AddListener(onPointerEnter);

        if (onPointerExit != null)
            controller.OnPointerExitEvent.AddListener(onPointerExit);

        Log($"Set up event handlers for card {controller.name}", LogTag.Cards | LogTag.UI);
    }

    public static void CleanupCardEventHandlers(CardController controller) {
        if (controller == null) return;

        controller.OnBeginDragEvent.RemoveAllListeners();
        controller.OnEndDragEvent.RemoveAllListeners();
        controller.OnCardDropped.RemoveAllListeners();
        controller.OnPointerEnterEvent.RemoveAllListeners();
        controller.OnPointerExitEvent.RemoveAllListeners();

        Log($"Cleaned up event handlers for card {controller.name}", LogTag.Cards | LogTag.UI);
    }

    public static void SetupCardEvents(CardController card, CardContainer container) {
        if (card == null || container == null) return;
        
        card.OnCardDropped.AddListener(new UnityAction<CardController>(container.OnCardDropped));
        card.OnPointerEnterEvent.AddListener(new UnityAction<CardController>(container.OnCardPointerEnter));
        card.OnPointerExitEvent.AddListener(new UnityAction<CardController>(container.OnCardPointerExit));
        card.OnBeginDragEvent.AddListener(new UnityAction<CardController>(container.OnCardBeginDrag));
        card.OnEndDragEvent.AddListener(new UnityAction<CardController>(container.OnCardEndDrag));
    }
}
