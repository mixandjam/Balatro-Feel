using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class CardDropZone {
    private readonly Image dropZoneImage;
    private bool isDraggingOverZone;
    private readonly Color defaultColor;
    private readonly Color validDropColor;
    private readonly Color invalidDropColor;
    private readonly Color hoverColor;
    private readonly bool acceptPlayer1Cards;
    private readonly bool acceptPlayer2Cards;

    public UnityEvent<CardController> OnCardDropped { get; } = new UnityEvent<CardController>();
    public UnityEvent<PointerEventData> OnPointerEnterEvent { get; } = new UnityEvent<PointerEventData>();
    public UnityEvent<PointerEventData> OnPointerExitEvent { get; } = new UnityEvent<PointerEventData>();

    public CardDropZone(
        Image dropZoneImage,
        Color defaultColor,
        Color validDropColor,
        Color invalidDropColor,
        Color hoverColor,
        bool acceptPlayer1Cards = true,
        bool acceptPlayer2Cards = true) {
        this.dropZoneImage = dropZoneImage;
        this.defaultColor = defaultColor;
        this.validDropColor = validDropColor;
        this.invalidDropColor = invalidDropColor;
        this.hoverColor = hoverColor;
        this.acceptPlayer1Cards = acceptPlayer1Cards;
        this.acceptPlayer2Cards = acceptPlayer2Cards;

        if (dropZoneImage != null) {
            dropZoneImage.color = defaultColor;
        }
    }

    public virtual bool CanAcceptCard(CardController card) {
        if (card == null) return false;
        bool canAccept = ValidateCardType(card) &&
            (card.IsPlayer1Card() ? acceptPlayer1Cards : acceptPlayer2Cards);
        UpdateVisualFeedback(canAccept);
        return canAccept;
    }

    protected virtual bool ValidateCardType(CardController card) {
        return card?.GetCardData() != null;
    }

    public void HandleDrop(PointerEventData eventData) {
        var card = eventData.pointerDrag?.GetComponent<CardController>();
        if (card != null && CanAcceptCard(card)) {
            OnCardDropped?.Invoke(card);
        }
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (eventData == null || dropZoneImage == null) return;

        isDraggingOverZone = true;
        if (eventData.pointerDrag != null) {
            var card = eventData.pointerDrag.GetComponent<CardController>();
            if (card != null) {
                CanAcceptCard(card);
            }
        } else {
            dropZoneImage.color = hoverColor;
        }
        OnPointerEnterEvent?.Invoke(eventData);
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (eventData == null || dropZoneImage == null) return;

        isDraggingOverZone = false;
        UpdateVisualFeedback(false);
        OnPointerExitEvent?.Invoke(eventData);
    }

    public void UpdateVisualFeedback(bool isValid) {
        if (dropZoneImage != null) {
            dropZoneImage.color = isDraggingOverZone ?
                (isValid ? validDropColor : invalidDropColor) :
                defaultColor;
        }
    }

    public void ResetVisualFeedback() {
        isDraggingOverZone = false;
        if (dropZoneImage != null) {
            dropZoneImage.color = defaultColor;
        }
    }

    public void Cleanup() {
        OnCardDropped.RemoveAllListeners();
        OnPointerEnterEvent.RemoveAllListeners();
        OnPointerExitEvent.RemoveAllListeners();
    }
}