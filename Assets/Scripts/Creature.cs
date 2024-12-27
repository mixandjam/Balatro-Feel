using static DebugLogger;
using static Enums;
using System.Linq;

public interface ICreature : ICard {
    int Attack { get; }
    int Health { get; }
    void TakeDamage(int damage);
    IPlayer Owner { get; }
}

public class Creature : Card, ICreature {
    public int Attack { get; private set; }
    public int Health { get; private set; }
    private bool isDead = false;
    public IPlayer Owner { get; private set; }
    private ICreature lastAttacker;

    public Creature(string name, int attack, int health, IPlayer owner = null) : base(name) {
        Attack = attack;
        Health = health;
        Owner = owner;
    }

    public void SetOwner(IPlayer newOwner) {
        Owner = newOwner;
    }

    public override void Play(IPlayer owner, ActionsQueue context) {
        Log($"Playing {Name} with {Effects.Count} effects", LogTag.Creatures | LogTag.Cards | LogTag.Actions);
        Owner = owner;

        // Add summon action to the queue
        var summonAction = new SummonCreatureAction(this, owner);
        context.AddAction(summonAction);

        Log($"Added SummonCreatureAction to queue for {Name}", LogTag.Creatures | LogTag.Actions);
    }

    // Implement the interface method
    public void TakeDamage(int damage) {
        TakeDamage(damage, null);
    }

    // Internal method with additional functionality
    internal void TakeDamage(int damage, ICreature attacker) {
        if (isDead) return;

        lastAttacker = attacker;
        Health = System.Math.Max(0, Health - damage);
        Log($"{Name} took {damage} damage, health now: {Health}. Has {Effects.Count} effects", LogTag.Creatures | LogTag.Combat);

        var gameManager = GameManager.Instance;
        if (gameManager != null && Owner != null) {
            Log($"Processing OnDamage effects for {Name}", LogTag.Creatures | LogTag.Effects);
            HandleEffect(EffectTrigger.OnDamage, gameManager.ActionsQueue);
        } else {
            LogError($"Cannot process damage effects - GameManager: {gameManager != null}, Owner: {Owner != null}", LogTag.Creatures | LogTag.Effects);
        }

        var gameEvents = GameEvents.Instance;
        if (gameEvents != null) {
            gameEvents.OnCreatureDamaged.Invoke(this, damage);

            if (Health <= 0 && !isDead) {
                isDead = true;
                if (Owner != null) {
                    Owner.RemoveFromBattlefield(this);
                }
                gameEvents.OnCreatureDied.Invoke(this);
            }
        }

        lastAttacker = null;
    }

    public void HandleEffect(EffectTrigger trigger, ActionsQueue actionsQueue) {
        Log($"Handling {trigger} effect for {Name} with {Effects.Count} effects", LogTag.Creatures | LogTag.Effects);
        foreach (var effect in Effects) {
            Log($"Checking effect - Trigger: {effect.trigger}, Actions: {effect.actions.Count}", LogTag.Creatures | LogTag.Effects);
            if (effect.trigger == trigger) {
                foreach (var action in effect.actions) {
                    Log($"Processing action - Type: {action.actionType}, Value: {action.value}, Target: {action.targetType}", LogTag.Creatures | LogTag.Actions);
                    switch (action.actionType) {
                        case ActionType.Damage:
                            HandleDamageEffect(action, actionsQueue);
                            break;
                    }
                }
            }
        }
    }

    private void HandleDamageEffect(EffectAction action, ActionsQueue actionsQueue) {
        if (Owner == null) {
            LogError($"Cannot handle damage effect for {Name} - Owner is null", LogTag.Creatures | LogTag.Effects);
            return;
        }

        Log($"Getting targets for {Name}'s damage effect. TargetType: {action.targetType}, Damage: {action.value}", LogTag.Creatures | LogTag.Actions);

        // Special handling for OnDamage trigger to target the attacker
        if (lastAttacker != null && Effects.Any(e => e.trigger == EffectTrigger.OnDamage)) {
            Log($"Targeting attacker {lastAttacker.Name} for damage effect", LogTag.Creatures | LogTag.Actions);
            actionsQueue.AddAction(new DirectDamageAction(lastAttacker, action.value, this));
            return;
        }

        // Normal targeting for other effects
        var targets = TargetingSystem.GetValidTargets(Owner, action.targetType);
        Log($"Found {targets.Count} targets for {Name}'s damage effect", LogTag.Creatures | LogTag.Actions);

        foreach (var target in targets) {
            if (target is ICreature creature) {
                // Use DirectDamageAction for area effects (OnPlay trigger)
                if (Effects.Any(e => e.trigger == EffectTrigger.OnPlay)) {
                    Log($"Adding DirectDamageAction - Source: {Name}, Target: {creature.Name}, Damage: {action.value}",
                        LogTag.Creatures | LogTag.Actions | LogTag.Combat);
                    actionsQueue.AddAction(new DirectDamageAction(creature, action.value, this));
                } else {
                    Log($"Adding DamageCreatureAction - Source: {Name}, Target: {creature.Name}, Damage: {action.value}",
                        LogTag.Creatures | LogTag.Actions | LogTag.Combat);
                    actionsQueue.AddAction(new DamageCreatureAction(creature, action.value, this));
                }
            } else if (target is IPlayer player) {
                Log($"Adding DamagePlayerAction - Target: Player {(player.IsPlayer1() ? "1" : "2")}, Damage: {action.value}",
                    LogTag.Creatures | LogTag.Actions | LogTag.Players | LogTag.Combat);
                actionsQueue.AddAction(new DamagePlayerAction(player, action.value));
            }
        }
    }
}