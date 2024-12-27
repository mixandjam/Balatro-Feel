using UnityEngine.Events;
using System;
using System.Collections.Generic;
using static DebugLogger;

[Serializable]
public class PlayerDamagedUnityEvent : UnityEvent<int> { }

public interface IPlayer : IEntity, IDamageable {
    bool IsPlayer1();
    IPlayer Opponent { get; set; }
    List<ICard> Hand { get; }
    List<ICreature> Battlefield { get; }
    void AddToHand(ICard card);
    void AddToBattlefield(ICreature creature);
    void AddToBattlefield(ICreature creature, int slotIndex);
    void RemoveFromBattlefield(ICreature creature);
    PlayerDamagedUnityEvent OnDamaged { get; }
}

public class Player : Entity, IPlayer {
    private const int MAX_BATTLEFIELD_SLOTS = 5;
    private readonly ICreature[] battlefieldSlots = new ICreature[MAX_BATTLEFIELD_SLOTS];

    public int Health { get; private set; } = 20;
    public IPlayer Opponent { get; set; }
    public List<ICard> Hand { get; private set; }
    public List<ICreature> Battlefield { get; private set; }
    public PlayerDamagedUnityEvent OnDamaged { get; } = new PlayerDamagedUnityEvent();

    private readonly GameEvents gameEvents;

    public Player(string name = "Player") : base(name) {
        Hand = new List<ICard>();
        Battlefield = new List<ICreature>();
        gameEvents = GameEvents.Instance;
    }

    public bool IsPlayer1() {
        var gameManager = GameManager.Instance;
        return gameManager != null && gameManager.Player1 == this;
    }

    public void TakeDamage(int amount) {
        Health = Math.Max(0, Health - amount);
        OnDamaged.Invoke(amount);
    }

    public void AddToHand(ICard card) {
        if (card == null) return;
        Hand.Add(card);
        gameEvents.OnGameStateChanged.Invoke();
    }

    public void AddToBattlefield(ICreature creature) {
        AddToBattlefield(creature, -1);
    }

    public void AddToBattlefield(ICreature creature, int slotIndex = -1) {
        if (creature == null) return;

        // If no specific slot is requested, find the first available slot
        if (slotIndex == -1) {
            slotIndex = FindFirstEmptySlot();
        }

        // Validate slot index
        if (slotIndex < 0 || slotIndex >= MAX_BATTLEFIELD_SLOTS) {
            LogWarning($"Invalid slot index {slotIndex}", LogTag.Creatures);
            return;
        }

        // Remove from current slot if already on battlefield
        int currentIndex = Array.IndexOf(battlefieldSlots, creature);
        if (currentIndex != -1) {
            battlefieldSlots[currentIndex] = null;
        }

        // Add to new slot
        battlefieldSlots[slotIndex] = creature;

        // Update the list representation
        Battlefield.Clear();
        foreach (var c in battlefieldSlots) {
            if (c != null) {
                Battlefield.Add(c);
            }
        }

        // Notify if creature is newly added to battlefield
        if (currentIndex == -1) {
            gameEvents?.OnCreatureSummoned.Invoke(creature, this);
        }

        gameEvents?.OnGameStateChanged.Invoke();
    }

    private int FindFirstEmptySlot() {
        for (int i = 0; i < MAX_BATTLEFIELD_SLOTS; i++) {
            if (battlefieldSlots[i] == null) {
                return i;
            }
        }
        return -1;
    }

    public void RemoveFromBattlefield(ICreature creature) {
        if (creature == null) return;

        int index = Array.IndexOf(battlefieldSlots, creature);
        if (index != -1) {
            battlefieldSlots[index] = null;
            Battlefield.Remove(creature);
            Log($"Removed creature from battlefield: {creature.Name}", LogTag.Creatures);
            gameEvents?.OnGameStateChanged.Invoke();
        }
    }

    public int GetCreatureSlotIndex(ICreature creature) {
        return Array.IndexOf(battlefieldSlots, creature);
    }

    public bool HasEmptyBattlefieldSlot() {
        return FindFirstEmptySlot() != -1;
    }

    public ICreature GetCreatureInSlot(int slotIndex) {
        if (slotIndex >= 0 && slotIndex < MAX_BATTLEFIELD_SLOTS) {
            return battlefieldSlots[slotIndex];
        }
        return null;
    }

    public override string ToString() {
        return $"{Name} - Health: {Health}, Hand: {Hand.Count}, Battlefield: {Battlefield.Count}";
    }
}