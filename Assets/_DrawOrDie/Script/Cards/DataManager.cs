using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;
    public bool isDataLoaded { get; private set; } = false; // 상태 변수
    public event Action OnDataLoaded; // 이벤트

    public Dictionary<int, Adjective> adjectiveTable = new Dictionary<int, Adjective>();
    public Dictionary<int, Gerund> gerundTable = new Dictionary<int, Gerund>();

    [Header("전투 시스템")]
    public int currentMana;
    public int diceResult;

    [Header("카드 패(Hand) 시스템")]
    public GameObject cardPrefab;
    public Transform handArea;

    // 1920x1080 해상도 기준 부채꼴 레이아웃
    public float cardSpacing = 200f;
    public float heightSpacing = 35f;
    public float angleSpacing = -9f;

    [Header("덱/묘지 UI")]
    public Transform deckPile;
    public Transform discardPile;

    void Awake() { Instance = this; }

    private void Start()
    {
        StartCoroutine(LoadCards());
    }

    public void TriggerCardDraw(int diceEnergy)
    {
        currentMana = diceEnergy;
        StartCoroutine(FetchAndDrawCards());
    }
    
    public void ClearHand()
    {
        ComboManager.Instance.ClearAllSlots();
        foreach (Transform child in handArea) Destroy(child.gameObject);
    }

    IEnumerator FetchAndDrawCards()
    {
        ClearHand();

        //string url = "http://localhost:8080/api/game/start-cards";
        //using (UnityWebRequest www = UnityWebRequest.Get(url))
        //{
        //    yield return www.SendWebRequest();
        //    if (www.result == UnityWebRequest.Result.Success)
        //    {
        //        string json = www.downloadHandler.text;
        //        BattleStartHand hand = JsonUtility.FromJson<BattleStartHand>(json);
        //        StartCoroutine(DrawCardsRoutine(hand));
        //    }
        //}

        BattleStartHand hand = DeckManager.Instance.PrepareInitialHand();

        yield return StartCoroutine(DrawCardsRoutine(hand));
    }

    IEnumerator DrawCardsRoutine(BattleStartHand hand)
    {
        for (int i = 0; i < 2; i++)
        {
            GameObject newCard = Instantiate(cardPrefab, handArea);
            CardUI ui = newCard.GetComponent<CardUI>();
            ui.Setup(hand.adjectives[i].id, hand.adjectives[i].name, hand.adjectives[i].costMod.ToString(), 
                     hand.adjectives[i].desc, CardType.Adjective, hand.adjectives[i].costMod, hand.adjectives[i].dmgMod, hand.adjectives[i].shdMod, hand.adjectives[i].healMod);
            
            //ui.Setup(hand.adjectives[i].id, hand.adjectives[i].name, hand.adjectives[i].costMod.ToString(),
            //         hand.adjectives[i].desc, CardType.Adjective, hand.adjectives[i].costMod, hand.adjectives[i].dmgMod);

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
            ui.Setup(hand.gerunds[i].id, hand.gerunds[i].name, hand.gerunds[i].baseCost.ToString(), 
                     hand.gerunds[i].desc, CardType.Gerund, hand.gerunds[i].baseCost, hand.gerunds[i].baseDmg, hand.gerunds[i].baseShd, hand.gerunds[i].baseHeal);
            
            //ui.Setup(hand.gerunds[i].id, hand.gerunds[i].name, hand.gerunds[i].baseCost.ToString(),
            //         hand.gerunds[i].desc, CardType.Gerund, hand.gerunds[i].baseCost, hand.gerunds[i].baseDmg);

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

    // 카드를 덱 파일 위치에 스폰 (DiscardPile과 동일하게 월드 좌표 직접 사용)
    private void SpawnAtDeckPile(RectTransform rect)
    {
        if (deckPile != null)
            rect.position = deckPile.position;
        else
            rect.localPosition = new Vector3(-800f, -400f, 0);

        rect.localScale = Vector3.zero;
    }

    // 턴 끝낼 때: 슬롯 카드 + 패 카드 전부 묘지로 날려보냄
    public void StartDiscardAll()
    {
        // 패 카드를 먼저 수집 (슬롯 카드 re-parent 전에)
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

    // 카드를 묘지로 날려보내는 애니메이션 후 제거
    public void DiscardCard(GameObject cardObj)
    {
        StartCoroutine(DiscardCardRoutine(cardObj));
    }

    private IEnumerator DiscardCardRoutine(GameObject cardObj)
    {
        if (cardObj == null) yield break;

        CardDraggable drag = cardObj.GetComponent<CardDraggable>();
        if (drag != null) drag.enabled = false;

        CardUI cardUI = cardObj.GetComponent<CardUI>();
        if (cardUI != null) cardUI.enabled = false;

        RectTransform rect = cardObj.GetComponent<RectTransform>();
        if (rect == null) { Destroy(cardObj); yield break; }

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
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            rect.position = Vector3.Lerp(startPos, endPos, t);
            rect.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }

        Destroy(cardObj);
    }

    public void AddSynergyCardToHand(Combination comboData)
    {
        GameObject newCard = Instantiate(cardPrefab, handArea);
        CardUI ui = newCard.GetComponent<CardUI>();
        ui.Setup(0, comboData.skillName, comboData.finalCost.ToString(), 
                 comboData.description, CardType.Synergy, comboData.finalCost, comboData.finalDamage, comboData.finalShield, comboData.finalHeal);
        RearrangeHand();
    }

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

    public IEnumerator LoadCards()
    {
        // 1. 가져와야 할 데이터들의 URL (서버 API 주소에 맞게 수정해)
        string adjUrl = "http://localhost:8080/api/game/load-adjectives";
        string gerUrl = "http://localhost:8080/api/game/load-gerunds";

        // 2. 동시 처리를 위해 코루틴을 각각 실행하거나, 
        // 여기선 각각 호출하는 것으로 가정할게.
        yield return StartCoroutine(FetchData<AdjectiveList>(adjUrl, (list) => {
            adjectiveTable.Clear();
            foreach (var adj in list.adjectives)
                adjectiveTable[adj.id] = adj;
        }));

        yield return StartCoroutine(FetchData<GerundList>(gerUrl, (list) => {
            gerundTable.Clear();
            foreach (var ger in list.gerunds)
                gerundTable[ger.id] = ger;
        }));

        isDataLoaded = true;
        OnDataLoaded?.Invoke();
        Debug.Log("모든 카드 데이터 로딩 및 딕셔너리 구축 완료!");
    }

    // 중복 코드를 줄이기 위한 제네릭 Fetch 함수
    private IEnumerator FetchData<T>(string url, System.Action<T> onComplete)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                T data = JsonUtility.FromJson<T>(www.downloadHandler.text);
                onComplete?.Invoke(data);
            }
            else
            {
                Debug.LogError($"{url} 데이터 로딩 실패: {www.error}");
            }
        }
    }
}