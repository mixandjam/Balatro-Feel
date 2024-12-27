public interface IEntity : ITarget {
    string Name { get; }
}

public abstract class Entity : IEntity {
    private readonly string id;
    public string TargetId => id;
    public string Name { get; protected set; }

    protected Entity(string name) {
        id = System.Guid.NewGuid().ToString();
        Name = name;
    }

    public virtual bool IsValidTarget(IPlayer controller) {
        return true;
    }
}