using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BattlefieldSlot : MonoBehaviour {
    private int index;
    private CardController occupyingCard;
    private RectTransform rectTransform;
    private Image slotImage;
    private CardDropZone dropZoneHandler;

    public int Index => index;
    public bool IsOccupied => occupyingCard != null;
    public CardController OccupyingCard => occupyingCard;

    public void Initialize(int slotIndex, Color defaultColor, Color validDropColor, Color invalidDropColor, Color hoverColor) {
        index = slotIndex;
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null) {
            rectTransform = gameObject.AddComponent<RectTransform>();
        }

        slotImage = GetComponent<Image>();
        if (slotImage == null) {
            slotImage = gameObject.AddComponent<Image>();
        }

        dropZoneHandler = new CardDropZone(
            slotImage,
            defaultColor,
            validDropColor,
            invalidDropColor,
            hoverColor
        );
    }

    public void SetPosition(Vector2 position) {
        if (rectTransform != null) {
            rectTransform.anchoredPosition = position;
        }
    }

    public void OccupySlot(CardController card) {
        occupyingCard = card;
        if (card != null) {
            card.transform.SetParent(transform);
            card.transform.localPosition = Vector3.zero;
        }
    }

    public void ClearSlot() {
        occupyingCard = null;
    }

    public void UpdateVisuals(bool isValid) {
        dropZoneHandler?.UpdateVisualFeedback(isValid);
    }

    public void ResetVisuals() {
        dropZoneHandler?.ResetVisualFeedback();
    }

    private void OnDestroy() {
        dropZoneHandler?.Cleanup();
    }
}