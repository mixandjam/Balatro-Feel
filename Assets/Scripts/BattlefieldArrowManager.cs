using UnityEngine;
using System.Collections.Generic;
using static DebugLogger;
using System.Linq;

public class BattlefieldArrowManager {
    private readonly Transform parentTransform;
    private readonly GameManager gameManager;
    private readonly GameReferences gameReferences;
    private ArrowIndicator dragArrowIndicator;
    private Dictionary<string, ArrowIndicator> activeArrows = new Dictionary<string, ArrowIndicator>();

    public BattlefieldArrowManager(Transform parent, GameManager gameManager) {
        this.parentTransform = parent;
        this.gameManager = gameManager;
        this.gameReferences = GameReferences.Instance;
        SetupDragArrow();
    }

    private void SetupDragArrow() {
        dragArrowIndicator = ArrowIndicator.Create(parentTransform);
        dragArrowIndicator.Hide();
    }

    public void ShowDragArrow(Vector3 startPos) {
        startPos.z = 0;
        dragArrowIndicator.Show(startPos, startPos);
    }

    public void UpdateDragArrow(Vector3 worldPos) {
        if (dragArrowIndicator != null && dragArrowIndicator.IsVisible()) {
            worldPos.z = 0;
            dragArrowIndicator.UpdateEndPosition(worldPos);
        }
    }

    public void HideDragArrow() {
        if (dragArrowIndicator != null) {
            dragArrowIndicator.Hide();
        }
    }

    public void UpdateArrowsFromActionsQueue() {
        Log("Starting update of arrows from actions queue", LogTag.Actions);

        ClearExistingArrows();

        if (gameManager.ActionsQueue == null) {
            LogWarning("ActionsQueue is null!", LogTag.Actions);
            return;
        }

        var pendingActions = gameManager.ActionsQueue.GetPendingActions();
        Log($"Number of pending actions: {pendingActions.Count}", LogTag.Actions);

        foreach (var action in pendingActions) {
            Log($"Processing action: {action.GetType().Name}", LogTag.Actions);
            ProcessAction(action);
        }
    }

    private void ClearExistingArrows() {
        foreach (var arrow in activeArrows.Values) {
            if (arrow != null) {
                Object.Destroy(arrow.gameObject);
            }
        }
        activeArrows.Clear();
    }

    private void ProcessAction(IGameAction action) {
        Log($"Attempting to process action: {action.GetType().Name}", LogTag.Actions);

        switch (action) {
            case DamageCreatureAction damageAction:
                CreateArrowForDamageAction(damageAction);
                break;
            case DamagePlayerAction playerDamageAction:
                CreateArrowForPlayerDamageAction(playerDamageAction);
                break;
            case MarkCombatTargetAction markCombatAction:
                Log("Creating arrow for MarkCombatTargetAction", LogTag.Actions);
                CreateArrowForMarkCombatAction(markCombatAction);
                break;
        }
    }

    private void CreateArrowForMarkCombatAction(MarkCombatTargetAction action) {
        var attacker = action.GetAttacker();
        var targetSlot = action.GetTargetSlot();

        if (attacker == null || targetSlot == null) return;

        var arrow = ArrowIndicator.Create(parentTransform);
        Vector3 startPos = GetCreaturePosition(attacker);
        Vector3 endPos = targetSlot.transform.position;

        startPos.z = 0;
        endPos.z = 0;

        arrow.Show(startPos, endPos);
        activeArrows[attacker.TargetId] = arrow;

        Log($"Created combat targeting arrow from {attacker.Name} to slot {targetSlot.Index}", LogTag.Actions | LogTag.UI);
    }

    private void CreateArrowForDamageAction(DamageCreatureAction damageAction) {
        var attacker = damageAction.GetAttacker();
        var target = damageAction.GetTarget();

        if (attacker == null || target == null) return;

        var arrow = ArrowIndicator.Create(parentTransform);
        Vector3 startPos = GetCreaturePosition(attacker);
        Vector3 endPos = GetCreaturePosition(target);

        startPos.z = 0;
        endPos.z = 0;

        arrow.Show(startPos, endPos);
        activeArrows[attacker.TargetId] = arrow;

        Log($"Created arrow from {attacker.Name} to {target.Name}", LogTag.Actions | LogTag.UI);
    }

    private void CreateArrowForPlayerDamageAction(DamagePlayerAction action) {
        if (action == null) return;

        var sourceCreature = FindSourceCreatureForPlayerDamage(action);
        if (sourceCreature == null) return;

        var targetPlayer = action.GetTargetPlayer();
        if (targetPlayer == null) return;

        var arrow = ArrowIndicator.Create(parentTransform);
        Vector3 startPos = GetCreaturePosition(sourceCreature);
        Vector3 endPos = GetPlayerTargetPosition(targetPlayer);

        startPos.z = 0;
        endPos.z = 0;

        arrow.Show(startPos, endPos);
        activeArrows[sourceCreature.TargetId] = arrow;

        Log($"Created arrow from {sourceCreature.Name} to Player {(targetPlayer.IsPlayer1() ? "1" : "2")}",
            LogTag.Actions | LogTag.UI);
    }

    private ICreature FindSourceCreatureForPlayerDamage(DamagePlayerAction action) {
        // Look through the battlefield to find the attacking creature
        var attackingCreature = gameManager.Player1.Battlefield
            .FirstOrDefault(c => gameManager.CombatHandler.HasCreatureAttacked(c.TargetId));

        if (attackingCreature == null) {
            attackingCreature = gameManager.Player2.Battlefield
                .FirstOrDefault(c => gameManager.CombatHandler.HasCreatureAttacked(c.TargetId));
        }

        return attackingCreature;
    }

    private Vector3 GetCreaturePosition(ICreature creature) {
        if (creature == null) return Vector3.zero;

        // Try to find the card in Player 1's battlefield
        var player1Battlefield = gameReferences.GetPlayer1BattlefieldUI();
        var cardController = player1Battlefield?.GetCardControllerByCreatureId(creature.TargetId);

        // If not found in Player 1's battlefield, try Player 2's
        if (cardController == null) {
            var player2Battlefield = gameReferences.GetPlayer2BattlefieldUI();
            cardController = player2Battlefield?.GetCardControllerByCreatureId(creature.TargetId);
        }

        if (cardController != null) {
            var position = cardController.transform.position;
            Log($"Found position for creature {creature.Name} with ID {creature.TargetId}: {position}", LogTag.Actions);
            return position;
        }

        LogWarning($"Could not find CardController for creature {creature.Name} with ID {creature.TargetId}", LogTag.Actions);
        return Vector3.zero;
    }

    private Vector3 GetPlayerTargetPosition(IPlayer player) {
        var playerUI = player.IsPlayer1() ?
            gameReferences.GetPlayer1UI() :
            gameReferences.GetPlayer2UI();

        if (playerUI != null) {
            return playerUI.transform.position;
        }

        LogWarning($"Could not find UI for Player {(player.IsPlayer1() ? "1" : "2")}", LogTag.Actions);
        return Vector3.zero;
    }

    public void Cleanup() {
        ClearExistingArrows();
        if (dragArrowIndicator != null) {
            Object.Destroy(dragArrowIndicator.gameObject);
        }
    }
}