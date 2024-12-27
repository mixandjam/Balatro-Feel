using static DebugLogger;
using System.Collections.Generic;
using System.Linq;

public class BattlefieldCombatHandler {
    private readonly GameManager gameManager;
    private HashSet<string> attackingCreatureIds = new HashSet<string>();
    private readonly GameReferences gameReferences;
    private readonly Dictionary<string, BattlefieldSlot> targetedSlots = new Dictionary<string, BattlefieldSlot>();

    public BattlefieldCombatHandler(GameManager gameManager) {
        this.gameManager = gameManager;
        this.gameReferences = GameReferences.Instance;
    }

    public void HandleCreatureCombat(CardController attackingCard, BattlefieldSlot targetSlot) {
        var attackerCreature = FindCreatureByTargetId(attackingCard);

        if (attackerCreature == null) {
            LogWarning("Attacker creature is null", LogTag.Creatures | LogTag.Actions);
            return;
        }

        if (HasCreatureAttacked(attackerCreature.TargetId)) {
            LogWarning($"Creature {attackerCreature.Name} has already attacked this turn", LogTag.Creatures | LogTag.Combat);
            return;
        }

        if (targetSlot == null) {
            LogWarning("Target slot is null", LogTag.Creatures | LogTag.Combat);
            return;
        }

        RegisterAttack(attackerCreature.TargetId, targetSlot);
        QueueCombatAction(attackerCreature, targetSlot);
    }

    private void RegisterAttack(string attackerId, BattlefieldSlot targetSlot) {
        attackingCreatureIds.Add(attackerId);
        targetedSlots[attackerId] = targetSlot;
    }

    private void QueueCombatAction(ICreature attackerCreature, BattlefieldSlot targetSlot) {
        gameManager.ActionsQueue.AddAction(new MarkCombatTargetAction(attackerCreature, targetSlot));
        Log($"{attackerCreature.Name} targets slot {targetSlot.Index}", LogTag.Creatures | LogTag.Combat);
    }

    private ICreature FindCreatureByTargetId(CardController cardController) {
        if (cardController == null) return null;

        string targetId = cardController.GetLinkedCreatureId();
        if (string.IsNullOrEmpty(targetId)) return null;

        return gameManager.Player1.Battlefield.FirstOrDefault(c => c.TargetId == targetId) ??
               gameManager.Player2.Battlefield.FirstOrDefault(c => c.TargetId == targetId);
    }

    public void ResetAttackingCreatures() {
        attackingCreatureIds.Clear();
        targetedSlots.Clear();
        Log("Reset attacking creatures tracking", LogTag.Creatures | LogTag.Combat);
    }

    public bool HasCreatureAttacked(string creatureId) {
        return attackingCreatureIds.Contains(creatureId);
    }

    public BattlefieldSlot GetTargetedSlot(string attackerId) {
        return targetedSlots.TryGetValue(attackerId, out var slot) ? slot : null;
    }
}