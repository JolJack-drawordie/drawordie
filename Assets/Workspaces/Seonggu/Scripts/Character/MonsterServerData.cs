using System;
using System.Collections.Generic;

// 몬스터 공격 패턴 하나 (서버 JSON)
[Serializable]
public class AttackData
{
    public string skillId;
    public string name;
    public int damage;
    public int cooldown;
}

// 서버 Monster.json의 몬스터 하나 (일반 몬스터 / 중간보스 / 보스)
[System.Serializable]
public class MonsterServerData
{
    public int id;
    public string name;
    public int hp;
    public int shield;

    public List<AttackData> attacks;

    public int defend; // 방어 행동 때 얻는 방어도

    // 보스 기믹 수치 (BossGimmickData 참고). 일반 몬스터에는 없음
    public BossGimmickData boss;
}

// 서버 몬스터 목록 응답
[System.Serializable]
public class MonsterServerDataListWrapper
{
    public List<MonsterServerData> monsters;
}