using UnityEngine;
using System.Collections.Generic;

// 유닛 스탯 (체력, 방어도, 공격 패턴 등). 플레이어 원본은 에셋으로 두고, 게임 중에는 StatManager가 만든 복사본을 쓴다.
[CreateAssetMenu(fileName = "NewUnitStat", menuName = "Battle/Unit Stat Data")]
public class UnitStatData : ScriptableObject
{
    [Header("기본 정보")]
    public string unitName;
    public int monsterId = -1; // 서버 Monster.json id (아이콘 / 세이브용, 플레이어는 -1)
    public int maxHp;
    public int currentHp;
    public int maxShield;
    public int currentShield;
    public List<AttackData> attacks;
    public int defend;

    // 보스 기믹 수치 (BossGimmickData 참고). 일반 몬스터에는 없음
    public BossGimmickData boss;

    // 체력을 최대 체력으로 채우고 방어도는 0으로 (새 게임 / 복사본을 만들 때)
    public void ResetStat()
    {
        currentHp = maxHp;
        currentShield = 0;
    }
}