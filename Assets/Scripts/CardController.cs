using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using static DebugLogger;

[Serializable]
public class CardUnityEvent : UnityEvent<CardController> { }

public class CardController : UIComponent, IPointerEnterHandler, IPointerExitHandler, IDragHandler, IBeginDragHandler, IEndDragHandler {
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image cardImage;

    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private CanvasGroup canvasGroup;
    private Vector3 originalPosition;
    private Transform originalParent;
    private bool isDragging;
    private CardData cardData;
    private IPlayer player;
    private ICreature linkedCreature;
    private string linkedCreatureId;

    public CardUnityEvent OnBeginDragEvent = new CardUnityEvent();
    public CardUnityEvent OnEndDragEvent = new CardUnityEvent();
    public CardUnityEvent OnCardDropped = new CardUnityEvent();
    public UnityEvent<CardController> OnPointerEnterEvent = new UnityEvent<CardController>();
    public UnityEvent<CardController> OnPointerExitEvent = new UnityEvent<CardController>();

    public UnityAction OnPointerEnterHandler { get; set; }
    public UnityAction OnPointerExitHandler { get; set; }

    public Transform OriginalParent => originalParent;
    public ICreature GetLinkedCreature() => linkedCreature;
    public bool IsPlayer1Card() => player?.IsPlayer1() ?? false;
    public CardData GetCardData() => cardData;
    public string GetLinkedCreatureId() => linkedCreatureId;

    protected override void Awake() {
        base.Awake();
        SetupComponents();
    }

    private void SetupComponents() {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        cardImage = GetComponent<Image>();
    }

    public void Initialize(CardData data, IPlayer owner, string creatureId = null) {
        Log($"Setting up card: {data?.cardName}", LogTag.Cards | LogTag.Initialization);
        cardData = data;
        player = owner;
        linkedCreatureId = creatureId;

        if (cardData is CreatureData) {
            if (!string.IsNullOrEmpty(creatureId)) {
                linkedCreature = FindLinkedCreature();
                if (linkedCreature != null) {
                    linkedCreatureId = linkedCreature.TargetId;
                    Log($"Linked creature found: {linkedCreature.Name} (ID: {linkedCreatureId})",
                        LogTag.Cards | LogTag.Creatures);
                } else {
                    Log($"No linked creature found for ID: {creatureId}",
                        LogTag.Cards | LogTag.Creatures);
                }
            }
            // If no creatureId provided, this is a hand card and should not log a warning
            // TODO: In the future the cards in the hand should also have an ID
        }
        UpdateUI();
    }


    private ICreature FindLinkedCreature() {
        if (cardData == null || player == null || string.IsNullOrEmpty(linkedCreatureId))
            return null;

        // Try to find in player's battlefield first
        var creature = player?.Battlefield.Find(c => c.TargetId == linkedCreatureId);
        if (creature != null) return creature;

        // Try opponent's battlefield if not found
        creature = player?.Opponent?.Battlefield.Find(c => c.TargetId == linkedCreatureId);
        if (creature != null) return creature;

        return null;
    }

    protected override void SubscribeToEvents() {
        base.SubscribeToEvents();
        if (gameEvents != null) {
            gameEvents.OnCreatureDamaged.AddListener(HandleCreatureDamaged);
            gameEvents.OnCreatureDied.AddListener(HandleCreatureDied);
            gameEvents.OnCreatureStatChanged.AddListener(HandleCreatureStatChanged);
        }
    }

    protected override void UnsubscribeFromEvents() {
        base.UnsubscribeFromEvents();
        if (gameEvents != null) {
            gameEvents.OnCreatureDamaged.RemoveListener(HandleCreatureDamaged);
            gameEvents.OnCreatureDied.RemoveListener(HandleCreatureDied);
            gameEvents.OnCreatureStatChanged.RemoveListener(HandleCreatureStatChanged);
        }
    }

    private void HandleCreatureDamaged(ICreature creature, int damage) {
        if (linkedCreature != null && creature.TargetId == linkedCreature.TargetId) {
            Log($"Linked creature {creature.Name} took {damage} damage", LogTag.Combat | LogTag.Creatures);
            linkedCreature = creature;
            UpdateUI();
        }
    }

    private void HandleCreatureDied(ICreature creature) {
        if (linkedCreature != null && creature.TargetId == linkedCreature.TargetId) {
            linkedCreature = null;
            UpdateUI();
        }
    }

    private void HandleCreatureStatChanged(ICreature creature, int value) {
        if (linkedCreature != null && creature.TargetId == linkedCreature.TargetId) {
            Log($"Linked creature {creature.Name} stats changed. New value: {value}", LogTag.Creatures | LogTag.Effects);
            linkedCreature = creature;
            UpdateUI();
        }
    }

    public void OnBeginDrag(PointerEventData eventData) {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        Log($"Begin dragging card: {cardData?.cardName}", LogTag.UI | LogTag.Cards);
        originalPosition = transform.position;
        originalParent = transform.parent;
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
        isDragging = false;
        canvasGroup.blocksRaycasts = true;
        OnEndDragEvent.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        OnPointerEnterEvent?.Invoke(this);
        OnPointerEnterHandler?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData) {
        OnPointerExitEvent?.Invoke(this);
        OnPointerExitHandler?.Invoke();
    }

    public override void UpdateUI() {
        if (cardData == null) return;
        UpdateCardText();
        UpdateCardVisuals();
    }

    private void UpdateCardText() {
        nameText.text = cardData.cardName;
        descriptionText.text = cardData.description ?? string.Empty;

        if (cardData is CreatureData creatureData) {
            statsText.gameObject.SetActive(true);
            statsText.text = linkedCreature != null ?
                $"{linkedCreature.Attack}/{linkedCreature.Health}" :
                $"{creatureData.attack}/{creatureData.health}";
        } else {
            statsText.gameObject.SetActive(false);
        }
    }

    private void UpdateCardVisuals() {
        if (cardImage != null && player != null) {
            cardImage.color = player.IsPlayer1() ?
                gameReferences.GetPlayer1CardColor() :
                gameReferences.GetPlayer2CardColor();
        }
    }

    protected override void OnDestroy() {
        OnBeginDragEvent.RemoveAllListeners();
        OnEndDragEvent.RemoveAllListeners();
        OnCardDropped.RemoveAllListeners();
        OnPointerEnterEvent.RemoveAllListeners();
        OnPointerExitEvent.RemoveAllListeners();
        base.OnDestroy();
    }
}