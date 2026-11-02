// 전투 진행 단계 (GameManager.currentState)
public enum BattleState
{
    None,
    BattleStart,
    TurnStart,
    PlayerTurn,
    EnemyTurn,
    TurnEnd,
    Victory,
    Defeat
}