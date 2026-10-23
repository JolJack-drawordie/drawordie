using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 카드를 드롭하는 히트박스. 적/플레이어 양쪽에 붙이고 side로 구분
[RequireComponent(typeof(BoxCollider2D))]
public class EnemyTarget : MonoBehaviour
{
    public enum Side { Enemy, Player }

    [Tooltip("Enemy: 피해가 있는 카드만 받음 / Player: 피해가 없는 카드(방어도·회복 등)만 받음")]
    public Side side = Side.Enemy;

    [Header("드래그 중 대상 표시 (머리 위 화살표)")]
    public Color arrowColor = new Color(1f, 0.85f, 0.3f, 1f);
    public float arrowGap = 0.35f;          // 머리(또는 적 의도 아이콘) 꼭대기와 화살표 사이 간격
    public float arrowBobHeight = 0.12f;
    public float arrowBobSpeed = 6f;
    public int arrowSortingOrder = 2000;

    private static readonly List<EnemyTarget> targets = new List<EnemyTarget>();
    private static Sprite arrowSprite;
    private SpriteRenderer arrow;
    private Vector3 arrowBasePos;

    private DiceManager diceManager;

    // 카드 사용(공격/방어 연출) 진행 중인 개수. 진행 중에는 턴 종료를 막는다.
    private static int activeActionCount = 0;
    private int myActionCount = 0;
    public static bool IsActing => activeActionCount > 0;

    private void Start()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(400, 400);

        diceManager = FindFirstObjectByType<DiceManager>();
    }

    private void Awake()
    {
        // 드롭 판정은 Physics2D로 하므로 UI 클릭은 통과시킴 (겹친 조합 슬롯의 카드를 다시 집을 수 있도록)
        Image hitboxImage = GetComponent<Image>();
        if (hitboxImage != null) hitboxImage.raycastTarget = false;
    }

    private void OnEnable()
    {
        targets.Add(this);
    }

    private void OnDisable()
    {
        targets.Remove(this);
        SetHighlight(false);
    }

    private void Update()
    {
        if (arrow == null || !arrow.gameObject.activeSelf) return;
        arrow.transform.position = arrowBasePos + Vector3.up * (Mathf.Sin(Time.unscaledTime * arrowBobSpeed) * arrowBobHeight);
    }

    // 판정은 드롭 시점 수치 기준: 피해가 있으면 적, 없으면 플레이어 (피해+방어 섞인 카드는 적)
    public bool Accepts(CardUI card)
    {
        return (card.Damage > 0) == (side == Side.Enemy);
    }

    // 드래그 시작 시 이 카드를 받을 수 있는 대상 머리 위에 화살표 표시
    public static void HighlightFor(CardUI card)
    {
        foreach (EnemyTarget t in targets)
            t.SetHighlight(t.Accepts(card));
    }

    public static void ClearHighlights()
    {
        foreach (EnemyTarget t in targets)
            t.SetHighlight(false);
    }

    // 카드를 들고 있는 위치가 대상 위인지 확인 (드래그 중 카드 반응용)
    public static EnemyTarget FindAt(Vector3 position)
    {
        foreach (Collider2D hit in Physics2D.OverlapPointAll(position))
        {
            EnemyTarget t = hit.GetComponent<EnemyTarget>();
            if (t != null) return t;
        }
        return null;
    }

    private void SetHighlight(bool on)
    {
        UnitBase unit = null;
        if (on && GameManager.Instance != null)
            unit = side == Side.Enemy ? GameManager.Instance.Enemy : GameManager.Instance.Player;

        if (unit == null)
        {
            if (arrow != null) arrow.gameObject.SetActive(false);
            return;
        }

        if (arrow == null)
        {
            arrow = new GameObject($"{name}_Arrow").AddComponent<SpriteRenderer>();
            arrow.sprite = GetArrowSprite();
            arrow.sortingOrder = arrowSortingOrder;
        }

        arrow.color = arrowColor;
        arrowBasePos = GetHeadTop(unit) + new Vector3(0f, arrowGap, 0f);
        arrow.transform.position = arrowBasePos;
        arrow.gameObject.SetActive(true);
    }

    // 몸 렌더러 꼭대기 (적은 의도 아이콘과 겹치지 않도록 아이콘 위까지 포함)
    private static Vector3 GetHeadTop(UnitBase unit)
    {
        bool has = false;
        Bounds body = default;
        foreach (Renderer r in unit.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
            if (!has) { body = r.bounds; has = true; }
            else body.Encapsulate(r.bounds);
        }

        float x = has ? body.center.x : unit.transform.position.x;
        float top = has ? body.max.y : unit.transform.position.y + 2f;

        EnemyController enemy = unit.GetComponentInChildren<EnemyController>();
        if (enemy != null && enemy.intentIcon != null && enemy.intentIcon.isActiveAndEnabled)
        {
            Vector3[] corners = new Vector3[4];
            enemy.intentIcon.rectTransform.GetWorldCorners(corners);
            top = Mathf.Max(top, corners[1].y);
        }

        return new Vector3(x, top, unit.transform.position.z);
    }

    // 아래를 가리키는 삼각형 (어두운 테두리 + 밝은 안쪽, 색은 SpriteRenderer.color로 입힘)
    private static Sprite GetArrowSprite()
    {
        if (arrowSprite != null) return arrowSprite;

        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        Vector2 a = new Vector2(0.1f, 0.85f) * size;    // 왼쪽 위
        Vector2 b = new Vector2(0.9f, 0.85f) * size;    // 오른쪽 위
        Vector2 c = new Vector2(0.5f, 0.12f) * size;    // 아래 꼭짓점
        const float border = 4f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                // 세 변까지의 거리 중 최솟값 (안쪽이 양수)
                float d = Mathf.Min(EdgeDistance(p, a, c), Mathf.Min(EdgeDistance(p, c, b), EdgeDistance(p, b, a)));
                float alpha = Mathf.Clamp01(d + 0.5f);
                float shade = Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(d - border + 0.5f));
                tex.SetPixel(x, y, new Color(shade, shade, shade, alpha));
            }
        }
        tex.Apply();

        arrowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0f), 128f);
        return arrowSprite;
    }

    // 변 from->to 기준 왼쪽(삼각형 안쪽)까지의 부호 있는 거리
    private static float EdgeDistance(Vector2 p, Vector2 from, Vector2 to)
    {
        Vector2 e = to - from;
        return (e.x * (p.y - from.y) - e.y * (p.x - from.x)) / e.magnitude;
    }

    private void OnDestroy()
    {
        // 연출 도중 파괴되면 카운트가 남지 않도록 정리
        activeActionCount -= myActionCount;
        myActionCount = 0;

        // 화살표는 UI 밖(월드)에 만들었으므로 따로 정리
        if (arrow != null) Destroy(arrow.gameObject);
    }

    public void ReceiveCard(CardUI card)
    {
        // 플레이어 턴이 아니면(턴 종료 처리 중 등) 카드 사용 불가
        if (GameManager.Instance != null && GameManager.Instance.currentState != BattleState.PlayerTurn)
        {
            card.transform.SetParent(DataManager.Instance.handArea);
            DataManager.Instance.RearrangeHand();
            return;
        }

        if (diceManager != null && diceManager.CurrentEnergy >= card.Cost)
        {
            diceManager.UseEnergy(card.Cost);
            StartCoroutine(ActionSequence(card));
        }
        else
        {
            Debug.Log("마나가 부족합니다!");
            BattleLog.Add($"에너지가 부족해 [{CardName(card)}] 사용 불가 (필요 {card.Cost})");
            card.transform.SetParent(DataManager.Instance.handArea);
            DataManager.Instance.RearrangeHand();
        }
    }

    private static string CardName(CardUI card)
    {
        return card.nameText != null ? card.nameText.text : "카드";
    }

    // 기존 AttackSequence에서 ActionSequence로 변경
    // 방어 행동 추가
    private IEnumerator ActionSequence(CardUI card)
    {
        activeActionCount++;
        myActionCount++;

        // 연출 중 카드가 다시 드래그되지 않도록 막음
        CardDraggable drag = card.GetComponent<CardDraggable>();
        if (drag != null) drag.enabled = false;

        // 연출 대기 중 카드 오브젝트가 파괴될 수 있으므로 값을 미리 저장
        int damage = card.Damage;
        int shield = card.Shield;
        int heal = card.Heal;
        GameObject cardObj = card.gameObject;

        BattleLog.Add($"[{CardName(card)}] 사용 (에너지 -{card.Cost})", BattleLog.Kind.Card);

        if (card.GerundId != 0 && SkillEffectManager.Instance != null)
        {
            Vector3 effectPosition = card.Damage > 0 || GameManager.Instance.Player == null
                ? transform.position
                : GameManager.Instance.Player.transform.position;
            SkillEffectManager.Instance.PlaySkillEffect(card.AdjectiveId, card.GerundId, effectPosition);
        }

        if(damage > 0)
        {
            if (PlayerSpineController.Instance != null)
                yield return StartCoroutine(PlayerSpineController.Instance.AttackRoutine("attack1"));
            GameManager.Instance.Enemy.TakeDamage(damage);
        }

        if(shield > 0)
        {
            if (PlayerSpineController.Instance != null)
            {
                yield return StartCoroutine(PlayerSpineController.Instance.ShieldRoutine());
            }

            GameManager.Instance.Player.AddShield(shield);
        }
        
        if(heal > 0)
        {
            if (PlayerSpineController.Instance != null)
            {
                yield return StartCoroutine(PlayerSpineController.Instance.HealRoutine());
            }

            GameManager.Instance.Player.Heal(heal);
        }
        else if (heal < 0)
        {
            // 치유량이 음수면 체력 소모 (예: 독한 조합 카드)
            GameManager.Instance.Player.LoseHp(-heal);
        }

        GameManager.Instance.CheckBattleResult();

        DataManager.Instance.RearrangeHand();
        if (cardObj != null)
            DataManager.Instance.DiscardCard(cardObj);

        activeActionCount--;
        myActionCount--;

        if (GameManager.Instance.isGameOver)
        {
            UIManager uiManager = FindFirstObjectByType<UIManager>();
            if (uiManager != null)
                uiManager.ShowResult(GameManager.Instance.currentState == BattleState.Victory);
        }
    }
}
