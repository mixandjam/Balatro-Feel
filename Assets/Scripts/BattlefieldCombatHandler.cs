using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using static DebugLogger;

public class BattlefieldCombatHandler {
    private readonly GameManager gameManager;
    private HashSet<string> attackingCreatureIds = new HashSet<string>();

    public BattlefieldCombatHandler(GameManager gameManager) {
        this.gameManager = gameManager;
    }

    public void HandleCreatureCombat(CardController attackingCard) {
        var attackerCreature = FindCreatureByTargetId(attackingCard);

        if (attackerCreature == null) {
            LogWarning("Attacker creature is null", LogTag.Creatures | LogTag.Actions);
            return;
        }

        var targetCard = FindTargetCard(attackingCard);

        if (targetCard == null) {
            LogWarning("Target card is null", LogTag.Creatures | LogTag.Actions);
            return;
        }

        var targetCreature = FindCreatureByTargetId(targetCard);

        if (targetCreature != null) {
            // Create and queue the damage action
            CreateAndQueueDamageAction(attackerCreature, targetCreature);

            // Mark creature as having attacked only after the action is queued
            attackingCreatureIds.Add(attackerCreature.TargetId);
        }
    }

    private CardController FindTargetCard(CardController attackingCard) {
        var pointerEventData = new PointerEventData(EventSystem.current) {
            position = Input.mousePosition
        };

        var raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);

        foreach (var result in raycastResults) {
            var targetCard = result.gameObject.GetComponent<CardController>();
            if (IsValidTarget(targetCard, attackingCard)) {
                return targetCard;
            }
        }

        return null;
    }

    private bool IsValidTarget(CardController targetCard, CardController attackingCard) {
        return targetCard != null &&
               targetCard != attackingCard &&
               targetCard.IsPlayer1Card() != attackingCard.IsPlayer1Card();
    }

    private ICreature FindCreatureByTargetId(CardController cardController) {
        if (cardController == null) return null;

        string targetId = cardController.GetLinkedCreatureId();
        if (string.IsNullOrEmpty(targetId)) return null;

        return gameManager.Player1.Battlefield.FirstOrDefault(c => c.TargetId == targetId) ??
               gameManager.Player2.Battlefield.FirstOrDefault(c => c.TargetId == targetId);
    }

    private void CreateAndQueueDamageAction(ICreature attacker, ICreature target) {
        Log($"Creating DamageCreatureAction - Attacker: {attacker.Name}, Target: {target.Name}",
            LogTag.Actions | LogTag.Creatures);

        var damageAction = new DamageCreatureAction(target, attacker.Attack, attacker);
        gameManager.ActionsQueue.AddAction(damageAction);
    }

    public void ResetAttackingCreatures() {
        attackingCreatureIds.Clear();
        Log("Reset attacking creatures tracking", LogTag.Creatures | LogTag.Combat);
    }

    public bool HasCreatureAttacked(string creatureId) {
        return attackingCreatureIds.Contains(creatureId);
    }
}