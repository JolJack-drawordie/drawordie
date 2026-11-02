using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

// 전투 중 유닛 위에 피해 / 막음 / 회복 / 방어도 숫자를 띄운다. (UnitBase.OnCombatNumber 이벤트를 받음)
// 씬에 없으면 첫 숫자가 뜰 때 자동으로 만들어진다. 색이나 크기를 바꾸려면 전투 씬에 직접 배치해서 인스펙터에서 조정한다.
public class DamagePopupManager : MonoBehaviour
{
    private static DamagePopupManager instance;

    [Header("글자")]
    public TMP_FontAsset font; // 비우면 Resources의 '온글잎 콘콘체 SDF'
    public float fontSize = 4.5f;
    public int sortingOrder = 2000; // 캐릭터/배경 스프라이트보다 위에 그려지도록

    [Header("색")]
    public Color damageColor = new Color(1f, 0.3f, 0.25f);
    public Color blockedColor = new Color(0.7f, 0.78f, 0.88f);
    public Color healColor = new Color(0.4f, 0.95f, 0.45f);
    public Color shieldColor = new Color(0.4f, 0.7f, 1f);
    public Color hpLossColor = new Color(0.8f, 0.45f, 1f);

    [Header("연출")]
    public float duration = 0.8f;
    public float riseHeight = 1.2f;
    public float randomX = 0.3f; // 숫자가 겹치지 않게 좌우로 흩뜨리는 범위
    public float stackGap = 0.5f; // 연속으로 뜰 때 위로 쌓는 간격
    public float stackWindow = 0.3f; // 이 시간 안에 같은 유닛에 또 뜨면 위로 쌓음
    public int bigHitThreshold = 20; // 이 이상의 피해는 크게 표시
    public float bigHitScale = 1.3f;

    private ObjectPool<DamagePopup> pool;
    // 유닛별 마지막으로 숫자가 뜬 시간과 쌓인 칸 (연속 표시용)
    private readonly Dictionary<UnitBase, (float time, int index)> stacks = new Dictionary<UnitBase, (float, int)>();

    // 게임 시작 시 한 번 이벤트를 구독 (도메인 리로드가 꺼져 있어도 중복 구독되지 않게 먼저 해제)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Bootstrap()
    {
        UnitBase.OnCombatNumber -= HandleNumber;
        UnitBase.OnCombatNumber += HandleNumber;
    }

    // 숫자를 띄울 일이 생기면 매니저를 (없으면 만들어서) 사용
    private static void HandleNumber(UnitBase unit, UnitBase.NumberKind kind, int amount)
    {
        if (unit == null || amount <= 0) return;
        if (instance == null) new GameObject("DamagePopupManager").AddComponent<DamagePopupManager>();
        instance.Spawn(unit, kind, amount);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        if (font == null) font = Resources.Load<TMP_FontAsset>("온글잎 콘콘체 SDF");

        pool = new ObjectPool<DamagePopup>(
            CreatePopup,
            actionOnRelease: p => p.gameObject.SetActive(false),
            actionOnDestroy: p => Destroy(p.gameObject),
            collectionCheck: false,
            defaultCapacity: 10,
            maxSize: 30);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private DamagePopup CreatePopup()
    {
        GameObject go = new GameObject("DamagePopup");
        go.transform.SetParent(transform, false);
        DamagePopup popup = go.AddComponent<DamagePopup>();
        popup.Setup(font, fontSize, sortingOrder, p => pool.Release(p));
        go.SetActive(false);
        return popup;
    }

    private void Spawn(UnitBase unit, UnitBase.NumberKind kind, int amount)
    {
        Vector3 pos = GetAnchor(unit);
        pos += new Vector3(Random.Range(-randomX, randomX), NextStackIndex(unit) * stackGap, 0f);

        bool isHit = kind == UnitBase.NumberKind.Damage || kind == UnitBase.NumberKind.HpLoss;
        float scale = isHit && amount >= bigHitThreshold ? bigHitScale : 1f;

        pool.Get().Play(pos, GetText(kind, amount), GetColor(kind), scale, duration, riseHeight);
    }

    // 짧은 간격으로 연속으로 뜨면 (다단 히트, 피해 + 막음 등) 위로 한 칸씩 쌓는다
    private int NextStackIndex(UnitBase unit)
    {
        int index = 0;
        if (stacks.TryGetValue(unit, out var last) && Time.time - last.time < stackWindow)
            index = last.index + 1;

        stacks[unit] = (Time.time, index);
        return index;
    }

    // 몸 렌더러 범위의 상체쯤에 띄운다 (몬스터마다 키가 달라도 맞도록)
    private static Vector3 GetAnchor(UnitBase unit)
    {
        bool has = false;
        Bounds body = default;

        foreach (Renderer r in unit.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;

            if (!has) { body = r.bounds; has = true; }
            else body.Encapsulate(r.bounds);
        }

        if (!has) return unit.transform.position + Vector3.up * 1.5f;
        return new Vector3(body.center.x, body.center.y + body.extents.y * 0.5f, unit.transform.position.z);
    }

    // 막음은 숫자만, 회복/방어도는 +, 피해/체력 소모는 -
    private static string GetText(UnitBase.NumberKind kind, int amount)
    {
        switch (kind)
        {
            case UnitBase.NumberKind.Blocked: return amount.ToString();
            case UnitBase.NumberKind.Heal:
            case UnitBase.NumberKind.Shield: return $"+{amount}";
            default: return $"-{amount}";
        }
    }

    private Color GetColor(UnitBase.NumberKind kind)
    {
        switch (kind)
        {
            case UnitBase.NumberKind.Blocked: return blockedColor;
            case UnitBase.NumberKind.Heal: return healColor;
            case UnitBase.NumberKind.Shield: return shieldColor;
            case UnitBase.NumberKind.HpLoss: return hpLossColor;
            default: return damageColor;
        }
    }
}
