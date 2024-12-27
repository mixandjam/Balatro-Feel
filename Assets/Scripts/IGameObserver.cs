public interface IGameObserver {
    void OnGameStateChanged();
    void OnGameInitialized();
    void OnPlayerDamaged(IPlayer player, int damage);
    void OnCreatureDamaged(ICreature creature, int damage);
    void OnCreatureDied(ICreature creature);
    void OnGameOver(IPlayer winner);
    void OnCreaturePreSummon(ICreature creature);
    void OnCreatureSummoned(ICreature creature, IPlayer owner);
} 