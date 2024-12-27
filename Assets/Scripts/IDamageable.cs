public interface IDamageable {
    int Health { get; }
    void TakeDamage(int amount);
}