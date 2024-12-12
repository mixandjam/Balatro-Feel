using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HandUI : CardContainer {
    private void Start() {
        if (InitializationManager.Instance.IsComponentInitialized<GameMediator>()) {
            InitializeReferences();
        } else {
            InitializationManager.Instance.OnSystemInitialized.AddListener(InitializeReferences);
        }
    }

    private void InitializeReferences() {
        if (!InitializationManager.Instance.IsComponentInitialized<GameManager>()) {
            DebugLogger.LogWarning("GameManager not initialized yet, will retry later", LogTag.UI | LogTag.Initialization);
            return;
        }

        if (player != null) {
            Initialize(player);
        }
    }

    public override void Initialize(IPlayer assignedPlayer) {
        player = assignedPlayer;
        base.Initialize(player);
    }

    protected override void RegisterEvents() {
        if (gameMediator != null) {
            gameMediator.AddGameStateChangedListener(UpdateUI);
            gameMediator.AddGameInitializedListener(OnGameInitialized);
        }
    }

    protected override void UnregisterEvents() {
        if (gameMediator != null) {
            gameMediator.RemoveGameStateChangedListener(UpdateUI);
            gameMediator.RemoveGameInitializedListener(OnGameInitialized);
        }
    }

    private void OnGameInitialized() {
        UpdateUI();
    }

    public override void UpdateUI() {
        if (!IsInitialized || player == null) return;

        foreach (var card in cards.ToList()) {
            RemoveCard(card);
            if (card != null) {
                Destroy(card.gameObject);
            }
        }
        cards.Clear();

        foreach (var cardData in player.Hand) {
            var controller = CreateCard(cardData);
            if (controller != null) {
                AddCard(controller);
            }
        }

        UpdateLayout();
    }

    protected override CardController CreateCard(ICard cardData) {
        var cardPrefab = gameReferences.GetCardPrefab();
        if (cardPrefab == null || cardData == null) return null;

        var cardObj = Instantiate(cardPrefab, transform);
        var controller = cardObj.GetComponent<CardController>();
        if (controller != null) {
            var data = CreateCardData(cardData);
            DebugLogger.Log($"Creating card {cardData.Name} with {cardData.Effects.Count} effects", LogTag.UI | LogTag.Cards);
            controller.Setup(data, player);
        }
        return controller;
    }

    private CardData CreateCardData(ICard card) {
        if (card is ICreature creature) {
            var creatureData = ScriptableObject.CreateInstance<CreatureData>();
            creatureData.cardName = creature.Name;
            creatureData.attack = creature.Attack;
            creatureData.health = creature.Health;

            creatureData.effects = new List<CardEffect>();
            foreach (var effect in creature.Effects) {
                var newEffect = new CardEffect {
                    effectType = effect.effectType,
                    trigger = effect.trigger,
                    actions = effect.actions.Select(a => new EffectAction {
                        actionType = a.actionType,
                        value = a.value,
                        targetType = a.targetType
                    }).ToList()
                };
                creatureData.effects.Add(newEffect);
            }

            DebugLogger.Log($"Created CreatureData for {creature.Name} with {creatureData.effects.Count} effects", LogTag.UI | LogTag.Cards | LogTag.Effects);
            return creatureData;
        }
        return null;
    }

    protected override void OnCardBeginDrag(CardController card) {
        if (card == null) return;
        card.transform.SetAsLastSibling();
    }

    protected override void OnCardEndDrag(CardController card) {
        UpdateLayout();
    }

    protected override void OnCardDropped(CardController card) {
        UpdateLayout();
    }

    protected override void OnCardHoverEnter(CardController card) { }
    protected override void OnCardHoverExit(CardController card) { }
}