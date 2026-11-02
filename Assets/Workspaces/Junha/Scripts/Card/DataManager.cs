using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 전투 손패 관리: 카드 뽑기(덱 더미에서 날아오는 연출), 손패 부채꼴 배치, 버린 카드 더미로 날려 보내기,
// 조합 미리보기, 세이브 / 로드 시 손패 복원.
public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    [Header("카드 패(Hand) 시스템")]
    public GameObject cardPrefab;
    public GameObject previewCardPrefab;
    public Transform handArea;
    public Transform resultCard;

    // 손패 부채꼴 배치 간격 (1920x1080 기준)
    public float cardSpacing = 200f;
    public float heightSpacing = 35f;
    public float angleSpacing = -9f;

    [Header("덱/묘지 UI")]
    public Transform deckPile;
    public Transform discardPile;

    void Awake() { Instance = this; } // 전투 씬마다 새로 만들어지는 매니저

    // 턴 시작 시 TurnManager가 호출: 손패를 비우고 새로 뽑는다
    public void TriggerCardDraw()
    {
        StartCoroutine(FetchAndDrawCards());
    }

    // 손패와 조합 슬롯의 카드를 모두 지운다
    public void ClearHand()
    {
        ComboManager.Instance.ClearAllSlots();
        foreach (Transform child in handArea) Destroy(child.gameObject);
    }

    // 세이브 로드: 이미 복원된 손패 데이터로 카드 UI만 다시 만든다
    public void RestoreHand(List<ICard> hand)
    {
        ClearHand();

        foreach (ICard card in hand)
        {
            GameObject newCard = Instantiate(cardPrefab, handArea);
            CardUI ui = newCard.GetComponent<CardUI>();
            ui.SetCardData(card);

            RectTransform rect = newCard.GetComponent<RectTransform>();
            SpawnAtDeckPile(rect);
        }

        RearrangeHand();
    }

    // 저장용: 손패에 있는 조합 카드의 재료 id (버린 카드 더미로 날아가는 중인 카드는 제외)
    public List<ComboSaveData> GetComboCardsInHand()
    {
        List<ComboSaveData> combos = new List<ComboSaveData>();
        if (handArea == null) return combos;

        foreach (Transform child in handArea)
        {
            CardUI ui = child.GetComponent<CardUI>();
            if (ui == null || ui.cardType != CardType.Synergy || discardingCards.Contains(child.gameObject)) continue;

            combos.Add(new ComboSaveData(ui.AdjectiveId, ui.GerundId));
        }

        return combos;
    }

    // 세이브 로드: 저장된 조합 카드를 조합표에서 다시 찾아 손패에 만든다 (RestoreHand 다음에 호출)
    public void RestoreComboCards(List<ComboSaveData> combos)
    {
        if (combos == null || ComboManager.Instance == null) return;

        foreach (ComboSaveData saved in combos)
        {
            Combination combo = ComboManager.Instance.Combine(saved.adjectiveId, saved.gerundId);
            if (combo != null)
                AddSynergyCardToHand(combo);
            else
                Debug.LogWarning($"[DataManager] 저장된 조합 카드를 찾을 수 없음: {saved.adjectiveId}_{saved.gerundId}");
        }
    }

    // 덱에서 이번 턴 손패(형용사 2장 + 동명사 3장)를 받아 차례로 뽑는다
    IEnumerator FetchAndDrawCards()
    {
        ClearHand();

        BattleStartHand hand = DeckManager.Instance.PrepareInitialHand();

        yield return StartCoroutine(DrawCardsRoutine(hand));
    }

    // 한 장씩 덱 더미에서 날아오게 뽑는다 (형용사 2장 → 동명사 3장)
    IEnumerator DrawCardsRoutine(BattleStartHand hand)
    {
        for (int i = 0; i < 2; i++)
        {
            GameObject newCard = Instantiate(cardPrefab, handArea);
            CardUI ui = newCard.GetComponent<CardUI>();

            ICard cardModel = new AdjectiveCard(hand.adjectives[i]);
            ui.SetCardData(cardModel);

            RectTransform rect = newCard.GetComponent<RectTransform>();
            SpawnAtDeckPile(rect);
            RearrangeHand();

            // 카드 뽑는 소리
            if (SoundManager.Instance != null) {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.drawCardSound, true);
            }

            yield return new WaitForSeconds(0.15f);
        }

        for (int i = 0; i < 3; i++)
        {
            GameObject newCard = Instantiate(cardPrefab, handArea);
            CardUI ui = newCard.GetComponent<CardUI>();

            ICard cardModel = new GerundCard(hand.gerunds[i]);
            ui.SetCardData(cardModel);

            RectTransform rect = newCard.GetComponent<RectTransform>();
            SpawnAtDeckPile(rect);
            RearrangeHand();

            // 카드 뽑는 소리
            if (SoundManager.Instance != null) {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.drawCardSound, true);
            }

            yield return new WaitForSeconds(0.15f);
        }
    }

    // 카드를 덱 더미 위치에서 크기 0으로 시작 (손패 자리로 날아가며 커짐)
    private void SpawnAtDeckPile(RectTransform rect)
    {
        if (deckPile != null)
            rect.position = deckPile.position;
        else
            rect.localPosition = new Vector3(-800f, -400f, 0);

        rect.localScale = Vector3.zero;
    }

    // 턴 종료: 조합 슬롯의 카드와 손패 카드를 전부 버린 카드 더미로 날려 보낸다
    public void StartDiscardAll()
    {
        // 손패 카드를 먼저 모은 뒤 슬롯 카드를 손패로 옮겨 함께 버림
        List<GameObject> toDiscard = new List<GameObject>();
        foreach (Transform child in handArea) toDiscard.Add(child.gameObject);

        CardSlot adjSlot = ComboManager.Instance.adjSlot;
        CardSlot gerSlot = ComboManager.Instance.gerSlot;

        if (adjSlot.isOccupied && adjSlot.currentCard != null)
        {
            GameObject card = adjSlot.currentCard;
            card.transform.SetParent(handArea, true);
            adjSlot.RemoveCard();
            toDiscard.Add(card);
        }
        if (gerSlot.isOccupied && gerSlot.currentCard != null)
        {
            GameObject card = gerSlot.currentCard;
            card.transform.SetParent(handArea, true);
            gerSlot.RemoveCard();
            toDiscard.Add(card);
        }
        ComboManager.Instance.HideSlots();

        foreach (GameObject card in toDiscard) DiscardCard(card);
    }

    // 버린 카드 더미로 날아가는 중인 카드 (카드 사용 직후 턴 종료를 누르면 같은 카드가 두 번 처리되는 것 방지)
    private readonly HashSet<GameObject> discardingCards = new HashSet<GameObject>();

    // 버린 카드 더미로 날아가는 연출 후 제거
    public void DiscardCard(GameObject cardObj)
    {
        if (cardObj == null || !discardingCards.Add(cardObj)) return;
        StartCoroutine(DiscardCardRoutine(cardObj));
    }

    private IEnumerator DiscardCardRoutine(GameObject cardObj)
    {
        yield return DiscardCardAnimation(cardObj);

        discardingCards.Remove(cardObj);
        if (cardObj != null) Destroy(cardObj);
    }

    private IEnumerator DiscardCardAnimation(GameObject cardObj)
    {
        if (cardObj == null) yield break;

        CardDraggable drag = cardObj.GetComponent<CardDraggable>();
        if (drag != null) drag.enabled = false;

        CardUI cardUI = cardObj.GetComponent<CardUI>();
        if (cardUI != null) cardUI.enabled = false;

        RectTransform rect = cardObj.GetComponent<RectTransform>();
        if (rect == null) yield break;

        // 카드 버리는 소리
        if (SoundManager.Instance != null) {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.drawCardSound, true);
        }

        Vector3 startPos = rect.position;
        Vector3 endPos = discardPile != null ? discardPile.position : startPos;
        Vector3 startScale = rect.localScale;

        float elapsed = 0f;
        float duration = 0.25f;

        while (elapsed < duration)
        {
            // 연출 중 다른 곳(ClearHand 등)에서 카드가 지워지면 중단
            if (rect == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            rect.position = Vector3.Lerp(startPos, endPos, t);
            rect.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }
    }

    // 조합 카드를 손패에 추가
    public void AddSynergyCardToHand(Combination comboData)
    {
        GameObject newCard = Instantiate(cardPrefab, handArea);
        CardUI ui = newCard.GetComponent<CardUI>();
        ui.SetupCombination(comboData);

        RearrangeHand();
    }

    // 조합 슬롯에 재료 두 장이 들어오면 결과 카드를 미리 보여준다
    public void CombinationPreview(Combination comboData)
    {
        ClearPreview();

        GameObject newCard = Instantiate(previewCardPrefab, resultCard);

        PreviewCardUI cardUI = newCard.GetComponent<PreviewCardUI>();

        cardUI.Setup(comboData);
    }

    public void ClearPreview()
    {
        foreach (Transform child in resultCard)
        {
            Destroy(child.gameObject);
        }
    }

    // 슬롯 밖의 카드들을 가운데 기준 부채꼴로 다시 배치 (가운데에서 멀수록 아래로, 바깥쪽으로 기울어짐)
    public void RearrangeHand()
    {
        List<CardUI> cardsInHand = new List<CardUI>();
        foreach (Transform child in handArea)
        {
            CardUI ui = child.GetComponent<CardUI>();
            if (ui != null && !ui.isInSlot) cardsInHand.Add(ui);
        }

        int total = cardsInHand.Count;
        for (int i = 0; i < total; i++)
        {
            float centerOffset = i - (total - 1) / 2f; 
            float xPos = centerOffset * cardSpacing;
            float yPos = -Mathf.Abs(centerOffset) * heightSpacing; 
            float zRot = centerOffset * angleSpacing;              

            cardsInHand[i].siblingIndex = i;
            cardsInHand[i].originPos = new Vector3(xPos, yPos, 0);
            cardsInHand[i].originRot = Quaternion.Euler(0, 0, zRot);

            cardsInHand[i].targetPos = cardsInHand[i].originPos;
            cardsInHand[i].targetRot = cardsInHand[i].originRot;
        }
    }
}
