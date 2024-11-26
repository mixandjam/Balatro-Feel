using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

[Serializable]
public class UnityEventCardButton : UnityEvent<CardButtonController> { }

public class CardButtonController : UIComponent, IPointerEnterHandler, IPointerExitHandler,
    IDragHandler, IBeginDragHandler, IEndDragHandler {
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Vector2 defaultSize = new Vector2(200f, 300f);

    private GameMediator gameMediator;
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private CanvasGroup canvasGroup;
    private bool isDragging;
    private CardData cardData;
    private IPlayer player;

    public UnityEventCardButton OnBeginDragEvent = new UnityEventCardButton();
    public UnityEventCardButton OnEndDragEvent = new UnityEventCardButton();
    public UnityEventCardButton OnCardDropped = new UnityEventCardButton();
    public Action OnPointerEnterHandler;
    public Action OnPointerExitHandler;

    protected override void RegisterEvents() {
        if (gameMediator != null) {
            gameMediator.AddGameStateChangedListener(UpdateUI);
        }
    }

    protected override void UnregisterEvents() {
        if (gameMediator != null) {
            gameMediator.RemoveGameStateChangedListener(UpdateUI);
        }
    }

    private void Awake() {
        gameMediator = GameMediator.Instance;

        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        rectTransform.sizeDelta = defaultSize;

        RegisterEvents();
    }

    public void Setup(CardData data, IPlayer player) {
        cardData = data;
        this.player = player;
        UpdateUI();
    }

    public override void UpdateUI() {
        if (cardData == null) return;

        nameText.text = cardData.cardName;

        if (cardData is CreatureData creatureData) {
            statsText.gameObject.SetActive(true);
            statsText.text = $"{creatureData.attack} / {creatureData.health}";
        } else {
            statsText.gameObject.SetActive(false);
        }

        descriptionText.text = cardData.description ?? string.Empty;
        GetComponent<Image>().color = player.IsPlayer1()
            ? References.GetPlayer1CardColor()
            : References.GetPlayer2CardColor();
    }

    public void OnBeginDrag(PointerEventData eventData) {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        isDragging = true;
        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling();
        OnBeginDragEvent.Invoke(this);
    }

    public void OnDrag(PointerEventData eventData) {
        if (!isDragging) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            eventData.position,
            parentCanvas.worldCamera,
            out Vector2 localPoint)) {
            transform.position = parentCanvas.transform.TransformPoint(localPoint);
        }
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (!isDragging) return;

        isDragging = false;
        canvasGroup.blocksRaycasts = true;
        OnEndDragEvent.Invoke(this);
        OnCardDropped.Invoke(this);

        gameMediator?.NotifyGameStateChanged();
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (!isDragging) {
            OnPointerEnterHandler?.Invoke();
        }
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (!isDragging) {
            OnPointerExitHandler?.Invoke();
        }
    }

    public CardData GetCardData() => cardData;
    public bool IsPlayer1Card() => player.IsPlayer1();

    private void OnDestroy() {
        UnregisterEvents();

        OnBeginDragEvent.RemoveAllListeners();
        OnEndDragEvent.RemoveAllListeners();
        OnCardDropped.RemoveAllListeners();
        OnPointerEnterHandler = null;
        OnPointerExitHandler = null;
    }
}