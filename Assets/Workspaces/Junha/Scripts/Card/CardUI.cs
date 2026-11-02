using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

// 손패의 카드 한 장. 카드 수치를 들고 있고, 마우스를 올리면 커지면서 옆 카드를 밀어낸다.
// 위치 / 회전 / 크기는 목표값(target)을 향해 매 프레임 부드럽게 따라간다.
public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public ICard cardData;
    public int CardID; // 서버 카드 id (조합 카드는 0)

    // 카드 효과 수치 (형용사 / 동명사 / 조합 공통)
    public CardType cardType;
    public int Cost;
    public int Damage;
    public int Shield;
    public int Heal;
    public int AdjectiveId; // 조합 카드는 재료 형용사 id, 형용사 카드는 자기 id
    public int GerundId; // 조합 카드는 재료 동명사 id, 동명사 카드는 자기 id (스킬 이펙트 / 세이브용)

    [Header("UI 연결")]
    public TMP_Text nameText;
    public TMP_Text costText;
    public TMP_Text descText;

    // 손패에서의 원래 자리(origin)와 지금 향하는 자리(target)
    public Vector3 originPos;
    public Quaternion originRot;
    public Vector3 targetPos;
    public Quaternion targetRot;
    private Vector3 targetScale = Vector3.one;

    private RectTransform rect;
    private CardDraggable cardDrag;
    private CardVisual cardVisual;
    public bool isInSlot = false; // 조합 슬롯에 들어가 있는지

    // 손패에서의 원래 순서 (마우스를 떼면 이 순서로 되돌림)
    public int siblingIndex; 

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        cardDrag = GetComponent<CardDraggable>();
        cardVisual = GetComponent<CardVisual>();
    }

    // 카드 내용 표시 + 수치 저장 (cost: 화면에 보일 글자, actualCost: 실제 비용)
    public void Setup(int id, string name, string cost, string desc, CardType type, int actualCost, int actualDamage, int actualShield, int actualHeal) 
    {
        CardID = id; 
        nameText.text = name;
        costText.text = cost;
        descText.text = CardText.Describe(type, desc, actualDamage, actualShield, actualHeal);

        cardType = type;
        AdjectiveId = type == CardType.Adjective ? id : 0;
        GerundId = type == CardType.Gerund ? id : 0;
        Cost = actualCost;
        Damage = actualDamage;
        Shield = actualShield;
        Heal = actualHeal;
    }

    // 조합 카드로 설정 (재료 id도 함께 기억)
    public void SetupCombination(Combination combo)
    {
        Setup(0, combo.skillName, combo.finalCost.ToString(), combo.description, CardType.Synergy,
            combo.finalCost, combo.finalDamage, combo.finalShield, combo.finalHeal);
        AdjectiveId = combo.adjectiveId;
        GerundId = combo.gerundId;

        if (cardVisual != null) cardVisual.Apply(combo);
    }

    // 슬롯에 들어간 카드는 마우스를 올려도 반응하지 않음
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isInSlot) OnCardFocus();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OffCardFocus();
    }

    // 마우스를 올린 카드: 커지고 위로 올라오며 맨 앞에 그려지고, 옆 카드들은 좌우로 밀려난다
    public void OnCardFocus()
    {
        targetScale = Vector3.one * 1.2f;
        targetPos = originPos + new Vector3(0, 60f, 0);
        targetRot = Quaternion.identity;

        transform.SetAsLastSibling();

        foreach (Transform child in transform.parent)
        {
            CardUI sibling = child.GetComponent<CardUI>();
            if (sibling != null && sibling != this && !sibling.isInSlot)
            {
                // 이 카드보다 왼쪽 카드는 왼쪽으로, 오른쪽 카드는 오른쪽으로
                float pushAmount = (sibling.siblingIndex < this.siblingIndex) ? -55f : 55f;
                sibling.targetPos = sibling.originPos + new Vector3(pushAmount, -10f, 0);
            }
        }
    }

    // 마우스를 뗀 카드: 원래 크기 / 자리 / 순서로 돌아가고 밀려났던 카드들도 제자리로
    public void OffCardFocus()
    {
        if (cardDrag != null && cardDrag.isDragging) return; // 끌고 있는 중에는 그대로 둠

        targetScale = Vector3.one;
        targetPos = originPos;
        targetRot = originRot;

        transform.SetSiblingIndex(siblingIndex);

        foreach (Transform child in transform.parent)
        {
            CardUI sibling = child.GetComponent<CardUI>();
            if (sibling != null && sibling != this && !sibling.isInSlot)
            {
                sibling.targetPos = sibling.originPos;
            }
        }
    }

    // 서버 카드 데이터(형용사 / 동명사)로 카드 설정 + 프레임 / 아이콘 적용
    public void SetCardData(ICard card)
    {
        cardData = card;

        Setup(
            card.Id,
            card.Name,
            card.Cost.ToString(),
            card.Description,
            card.Type,
            card.Cost,
            card.Damage,
            card.Shield,
            card.Heal
        );

        if (cardVisual != null) cardVisual.Apply(card);
    }

    // 끌고 있거나 슬롯 안에 있으면 위치 애니메이션을 하지 않음
    void Update()
    {
        if (cardDrag != null && cardDrag.isDragging) return;
        if (isInSlot) return; // 슬롯 안의 카드에 손패 좌표를 적용하면 엉뚱한 곳으로 날아가므로

        rect.localPosition = Vector3.Lerp(rect.localPosition, targetPos, Time.deltaTime * 10f);
        rect.localRotation = Quaternion.Lerp(rect.localRotation, targetRot, Time.deltaTime * 10f);
        rect.localScale = Vector3.Lerp(rect.localScale, targetScale, Time.deltaTime * 10f);
    }
}
