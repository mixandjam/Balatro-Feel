using static DebugLogger;
using static Enums;
using System.Collections.Generic;
using UnityEngine;

public class TestSetup : MonoBehaviour {
    public List<CardData> CreateTestCards() {
        var cards = new List<CardData>();

        // Basic Ogre (no changes needed)
        var ogre = ScriptableObject.CreateInstance<CreatureData>();
        ogre.cardName = "Ogre";
        ogre.description = "High attack power";
        ogre.attack = 5;
        ogre.health = 3;
        cards.Add(ogre);

        // Thorned - Modified to target the attacker
        var thornCreature = ScriptableObject.CreateInstance<CreatureData>();
        thornCreature.cardName = "Thorned";
        thornCreature.description = "Returns 1 damage when attacked";
        thornCreature.attack = 2;
        thornCreature.health = 6;

        var thornEffect = new CardEffect {
            effectType = EffectType.Triggered,
            trigger = EffectTrigger.OnDamage,
            actions = new List<EffectAction> {
                new EffectAction {
                    actionType = ActionType.Damage,
                    value = 1,
                    targetType = TargetType.EnemyCreatures // This will be filtered to just the attacker in the Creature.HandleDamageEffect
                }
            }
        };
        thornCreature.effects = new List<CardEffect> { thornEffect };
        Log($"Created Thorned card with {thornCreature.effects.Count} effects", LogTag.Cards | LogTag.Effects | LogTag.Initialization);
        cards.Add(thornCreature);

        // Dragon - Modified to hit all enemy creatures
        var dragon = ScriptableObject.CreateInstance<CreatureData>();
        dragon.cardName = "Dragon";
        dragon.description = "Deals 2 damage to all enemy creatures when played";
        dragon.attack = 4;
        dragon.health = 4;

        var dragonEffect = new CardEffect {
            effectType = EffectType.Triggered,
            trigger = EffectTrigger.OnPlay,
            actions = new List<EffectAction> {
                new EffectAction {
                    actionType = ActionType.Damage,
                    value = 2,
                    targetType = TargetType.EnemyCreatures
                }
            }
        };
        dragon.effects = new List<CardEffect> { dragonEffect };
        Log($"Created Dragon card with {dragon.effects.Count} effects", LogTag.Cards | LogTag.Effects | LogTag.Initialization);
        cards.Add(dragon);

        // Rest of the cards remain unchanged
        var guardian = ScriptableObject.CreateInstance<CreatureData>();
        guardian.cardName = "Angel";
        guardian.description = "Heals friendly creatures at the start of your turn";
        guardian.attack = 2;
        guardian.health = 5;

        var guardianEffect = new CardEffect {
            effectType = EffectType.Triggered,
            trigger = EffectTrigger.StartOfTurn,
            actions = new List<EffectAction> {
                new EffectAction {
                    actionType = ActionType.Heal,
                    value = 1,
                    targetType = TargetType.FriendlyCreatures
                }
            }
        };
        guardian.effects.Add(guardianEffect);
        cards.Add(guardian);

        var berserker = ScriptableObject.CreateInstance<CreatureData>();
        berserker.cardName = "Berserker";
        berserker.description = "Deals 1 damage to all creatures at end of turn";
        berserker.attack = 3;
        berserker.health = 3;

        var berserkerEffect = new CardEffect {
            effectType = EffectType.Triggered,
            trigger = EffectTrigger.EndOfTurn,
            actions = new List<EffectAction> {
                new EffectAction {
                    actionType = ActionType.Damage,
                    value = 1,
                    targetType = TargetType.AllCreatures
                }
            }
        };
        berserker.effects.Add(berserkerEffect);
        cards.Add(berserker);

        var warChief = ScriptableObject.CreateInstance<CreatureData>();
        warChief.cardName = "War Chief";
        warChief.description = "Damages enemy creatures when played";
        warChief.attack = 3;
        warChief.health = 5;

        var warChiefEffect = new CardEffect {
            effectType = EffectType.Triggered,
            trigger = EffectTrigger.OnPlay,
            actions = new List<EffectAction> {
                new EffectAction {
                    actionType = ActionType.Damage,
                    value = 1,
                    targetType = TargetType.EnemyCreatures
                }
            }
        };
        warChief.effects.Add(warChiefEffect);
        cards.Add(warChief);

        Log("All cards created", LogTag.Cards | LogTag.Initialization);

        return cards;
    }
}