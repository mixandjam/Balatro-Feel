using static DebugLogger;
using System.Collections.Generic;
using UnityEngine.Events;

public class ActionsQueue {
    private List<IGameAction> actionsList = new List<IGameAction>();
    private int maxIterationDepth = 3;
    private int currentIterationDepth = 0;
    private Dictionary<string, IGameAction> activeCreatureActions = new Dictionary<string, IGameAction>();
    private readonly GameEvents gameEvents;
    private readonly BattlefieldCombatHandler combatHandler;

    public readonly UnityEvent OnActionsQueued = new UnityEvent();
    public readonly UnityEvent OnActionsResolved = new UnityEvent();

    public ActionsQueue(BattlefieldCombatHandler combatHandler) {
        this.combatHandler = combatHandler;
        this.gameEvents = GameEvents.Instance;
    }

    private int GetActionPriority(IGameAction action) {
        if (action == null) return int.MaxValue;

        return action switch {
            DirectDamageAction => 0,
            SwapCreaturesAction => 1,
            DamageCreatureAction => 2,
            _ => 3
        };
    }

    private string GetActiveCreatureId(IGameAction action) {
        if (action == null) return null;

        return action switch {
            DamageCreatureAction damageAction => damageAction.GetAttacker()?.TargetId,
            SwapCreaturesAction swapAction => swapAction.GetCreature1()?.TargetId,
            MarkCombatTargetAction combatAction => combatAction.GetAttacker()?.TargetId,
            _ => null
        };
    }

    public void AddAction(IGameAction action) {
        if (action == null) {
            LogError("Attempted to add null action to queue", LogTag.Actions);
            return;
        }

        if (currentIterationDepth >= maxIterationDepth) {
            LogWarning("Maximum iteration depth reached, skipping action", LogTag.Actions);
            return;
        }

        string activeCreatureId = GetActiveCreatureId(action);
        Log($"Adding action for creature ID: {activeCreatureId}", LogTag.Actions);

        if (!string.IsNullOrEmpty(activeCreatureId)) {
            if (activeCreatureActions.ContainsKey(activeCreatureId)) {
                Log($"Replacing existing action for creature {activeCreatureId}", LogTag.Actions);
                actionsList.Remove(activeCreatureActions[activeCreatureId]);
            }
            activeCreatureActions[activeCreatureId] = action;
        }

        InsertActionWithPriority(action);
        Log($"Added action to queue: {action.GetType()}", LogTag.Actions);

        OnActionsQueued?.Invoke();
        gameEvents?.OnGameStateChanged?.Invoke();
    }

    private void InsertActionWithPriority(IGameAction action) {
        int priority = GetActionPriority(action);
        int insertIndex = actionsList.Count;

        for (int i = 0; i < actionsList.Count; i++) {
            if (GetActionPriority(actionsList[i]) > priority) {
                insertIndex = i;
                break;
            }
        }

        actionsList.Insert(insertIndex, action);
    }

    public void ResolveActions() {
        currentIterationDepth++;
        Log($"Resolving actions. Queue size: {actionsList.Count}", LogTag.Actions);

        while (actionsList.Count > 0) {
            var action = actionsList[0];
            actionsList.RemoveAt(0);

            string activeCreatureId = GetActiveCreatureId(action);
            if (activeCreatureId != null) {
                activeCreatureActions.Remove(activeCreatureId);
            }

            action.Execute();
        }

        currentIterationDepth--;
        activeCreatureActions.Clear();

        if (combatHandler != null) {
            combatHandler.ResetAttackingCreatures();
        }

        OnActionsResolved.Invoke();
        gameEvents.OnGameStateChanged.Invoke();
    }

    public bool HasActiveAction(string creatureId) {
        return activeCreatureActions.ContainsKey(creatureId);
    }

    public IGameAction GetActiveAction(string creatureId) {
        activeCreatureActions.TryGetValue(creatureId, out var action);
        return action;
    }

    public int GetPendingActionsCount() => actionsList.Count;

    public IReadOnlyCollection<IGameAction> GetPendingActions() => actionsList.AsReadOnly();

    public void Cleanup() {
        actionsList.Clear();
        activeCreatureActions.Clear();
        OnActionsQueued.RemoveAllListeners();
        OnActionsResolved.RemoveAllListeners();
    }
}