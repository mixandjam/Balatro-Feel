using System.Collections.Generic;
using System.Linq;
using static Enums;

public interface ITarget {
    string TargetId { get; }
    bool IsValidTarget(IPlayer controller);
}

public static class TargetingSystem {
    public static List<ITarget> GetValidTargets(IPlayer controller, TargetType targetType) {
        var validTargets = new List<ITarget>();

        switch (targetType) {
            case TargetType.Enemy:
                validTargets.Add(controller.Opponent);
                break;
            case TargetType.Player:
                validTargets.Add(controller);
                break;
            case TargetType.AllCreatures:
                validTargets.AddRange(controller.Battlefield.Where(c => c.IsValidTarget(controller)));
                validTargets.AddRange(controller.Opponent.Battlefield.Where(c => c.IsValidTarget(controller)));
                break;
            case TargetType.FriendlyCreatures:
                validTargets.AddRange(controller.Battlefield.Where(c => c.IsValidTarget(controller)));
                break;
            case TargetType.EnemyCreatures:
                validTargets.AddRange(controller.Opponent.Battlefield.Where(c => c.IsValidTarget(controller)));
                break;
        }

        return validTargets;
    }
}