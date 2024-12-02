using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public abstract class BaseCardContainer : UIComponent {
    [SerializeField] protected float cardSpacing = 220f;
    [SerializeField] protected float cardOffset = 50f;
    [SerializeField] protected float cardMoveDuration = 0.15f;
    [SerializeField] protected Ease cardMoveEase = Ease.OutBack;
    [SerializeField] protected float cardHoverOffset = 30f;
    [SerializeField] protected float cardDragScale = 1.1f;

    protected List<CardController> cards = new List<CardController>();
    protected IPlayer player;
    protected CardContainer container;
    protected RectTransform containerRect;

    public virtual void Initialize(CardContainer container, IPlayer player) {
        this.container = container;
        this.player = player;
        containerRect = container.GetComponent<RectTransform>();

        if (containerRect == null) {
            containerRect = container.gameObject.AddComponent<RectTransform>();
        }

        UpdateContainerSettings();
        IsInitialized = true;
        UpdateUI();
    }

    protected virtual void UpdateContainerSettings() {
        if (container == null) return;

        var settings = new ContainerSettings {
            layoutType = ContainerLayout.Horizontal,
            spacing = cardSpacing,
            offset = cardOffset,
            cardMoveDuration = cardMoveDuration,
            cardMoveEase = cardMoveEase,
            cardHoverOffset = cardHoverOffset
        };

        container.SetSettings(settings);
        container.SetPlayer(player);
    }

    protected virtual void UpdateContainerSize(int cardCount) {
        if (containerRect == null) return;

        // Calculate the total width needed for all cards
        float totalWidth = cardOffset * 2 + (cardSpacing * (Mathf.Max(1, cardCount - 1)));
        containerRect.sizeDelta = new Vector2(totalWidth, containerRect.sizeDelta.y);

        // Update card positions to be spread evenly
        for (int i = 0; i < cards.Count; i++) {
            UpdateCardPosition(cards[i], i, cards.Count);
        }
    }

    protected virtual void UpdateCardPosition(CardController card, int index, int totalCards) {
        if (card == null) return;
        var rectTransform = card.GetComponent<RectTransform>();
        if (rectTransform == null) return;

        float xPosition;
        if (totalCards <= 1) {
            // If there's only one card, place it in the middle
            xPosition = containerRect.rect.width / 2;
        } else {
            // Calculate position to spread cards evenly
            float availableWidth = containerRect.rect.width - (2 * cardOffset);
            float step = availableWidth / (totalCards - 1);
            xPosition = cardOffset + (step * index);
        }

        rectTransform.anchorMin = new Vector2(0, 0.5f);
        rectTransform.anchorMax = new Vector2(0, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(xPosition, 0);
    }

    protected virtual void ClearCards() {
        foreach (var card in cards) {
            if (card != null) {
                Destroy(card.gameObject);
            }
        }
        cards.Clear();
    }

    protected virtual CardController CreateCard(ICard cardData, Transform parent) {
        var cardPrefab = gameReferences.GetCardPrefab();
        if (cardPrefab == null) return null;

        var cardObj = Instantiate(cardPrefab, parent);
        var controller = cardObj.GetComponent<CardController>();

        if (controller != null) {
            var data = CreateCardData(cardData);
            controller.Setup(data, player);
            SetupCardEventHandlers(controller);
        }

        return controller;
    }

    protected virtual CardData CreateCardData(ICard card) {
        var cardData = ScriptableObject.CreateInstance<CreatureData>();
        cardData.cardName = card.Name;

        if (card is ICreature creature) {
            cardData.attack = creature.Attack;
            cardData.health = creature.Health;
        }

        return cardData;
    }

    protected virtual void SetupCardEventHandlers(CardController controller) {
        if (controller == null) return;

        controller.OnBeginDragEvent.AddListener(OnCardBeginDrag);
        controller.OnEndDragEvent.AddListener(OnCardEndDrag);
        controller.OnCardDropped.AddListener(OnCardDropped);
        controller.OnPointerEnterHandler += () => OnCardHoverEnter(controller);
        controller.OnPointerExitHandler += () => OnCardHoverExit(controller);
    }

    protected virtual void OnCardBeginDrag(CardController card) {
        card.transform.SetAsLastSibling();
        card.transform.DOScale(cardDragScale, cardMoveDuration);
    }

    protected virtual void OnCardEndDrag(CardController card) {
        card.transform.DOScale(1f, cardMoveDuration);
    }

    protected virtual void OnCardDropped(CardController card) {
        // Override in derived classes if needed
    }

    protected virtual void OnCardHoverEnter(CardController card) {
        // Override in derived classes if needed
    }

    protected virtual void OnCardHoverExit(CardController card) {
        // Override in derived classes if needed
    }

    protected override void OnDestroy() {
        ClearCards();
        base.OnDestroy();
    }
}