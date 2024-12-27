#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static Enums;

public class CardDataCreator
{
    [MenuItem("Cards/Create Basic Creature")]
    public static void CreateBasicCreature()
    {
        var creature = ScriptableObject.CreateInstance<CreatureData>();
        creature.cardName = "New Creature";
        creature.attack = 1;
        creature.health = 1;

        AssetDatabase.CreateAsset(creature, "Assets/Resources/Cards/NewCreature.asset");
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = creature;
    }

    [MenuItem("Cards/Create Creature With Effect")]
    public static void CreateCreatureWithEffect()
    {
        var creature = ScriptableObject.CreateInstance<CreatureData>();
        creature.cardName = "Effect Creature";
        creature.attack = 2;
        creature.health = 2;

        var effect = new CardEffect
        {
            effectType = EffectType.Triggered,
            trigger = EffectTrigger.OnDamage,
            actions = new System.Collections.Generic.List<EffectAction>
            {
                new EffectAction
                {
                    actionType = ActionType.Damage,
                    value = 1,
                    targetType = TargetType.Enemy
                }
            }
        };

        creature.effects.Add(effect);

        AssetDatabase.CreateAsset(creature, "Assets/Resources/Cards/EffectCreature.asset");
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = creature;
    }
}
#endif
