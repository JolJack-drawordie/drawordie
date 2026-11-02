using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 턴마다 주사위를 굴려 에너지(카드 비용)를 정한다: 기본 에너지 + 주사위 눈 + 유물 보너스.
// 3D 주사위 연출(Dice3DRoller)이 있으면 그 연출로, 없으면 2D 이미지를 빠르게 바꾸는 방식으로 보여 준다.
public class DiceManager : MonoBehaviour
{
    public static DiceManager Instance;

    [Header("에너지 설정")]
    public int baseEnergy = 3;
    public int diceValue; // 이번 턴 주사위 눈 (1~6)

    // 에너지가 바뀔 때 알림 (에너지 UI가 구독)
    public event System.Action<int> OnEnergyChanged;

    private int currentEnergy;
    public int CurrentEnergy
    {
        get => currentEnergy;
        private set
        {
            currentEnergy = value;
            OnEnergyChanged?.Invoke(currentEnergy);
        }
    }

    [Header("주사위 UI")]
    public Button rollDiceButton; // 주사위 굴리기 버튼
    public GameObject diceImageObject; // 주사위 이미지 부모 (굴릴 때만 보임)
    public Image diceImage;
    public Sprite[] diceSprites; // 눈 1~6 이미지 (순서 = 눈 - 1)

    [Tooltip("3D 주사위가 굴러가는 연출 사용 (끄거나 dice3D가 비어 있으면 기존처럼 이미지만 바뀜)")]
    public bool use3DDice = true;
    // 3D 주사위 연출 (전투 씬의 Dice3D 프리팹)
    public Dice3DRoller dice3D;

    public bool isRollFinished = false; // TurnManager가 주사위가 끝날 때까지 기다리는 표시

    // 이번 턴 에너지 최대치 (기본 + 주사위 + 유물 보너스, 에너지 게이지의 최대치)
    public int MaxEnergy { get; private set; }

    // 이번 턴 유물 효과로 추가된 에너지 (전투 로그에서 주사위 에너지와 구분용)
    public int BonusEnergy { get; private set; }

    // 이번 턴에 에너지를 준 유물 목록 (전투 로그용, 주사위를 굴릴 때마다 비움)
    public readonly System.Collections.Generic.List<(string source, int amount)> BonusSources
        = new System.Collections.Generic.List<(string source, int amount)>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // 씬에 이미 있으면 중복 생성 방지
        }
    }

    void Start()
    {
        if (diceImageObject != null) diceImageObject.SetActive(false);
        if (rollDiceButton != null)
        {
            rollDiceButton.gameObject.SetActive(false);
            rollDiceButton.onClick.AddListener(OnClickRollButton);
        }

        if (!use3DDice) dice3D = null;
    }

    // 플레이어 턴 시작 시 TurnManager가 호출
    public void ShowRollButton()
    {
        isRollFinished = false;
        if (rollDiceButton != null) rollDiceButton.gameObject.SetActive(true);
    }

    private void OnClickRollButton()
    {
        rollDiceButton.gameObject.SetActive(false);
        StartCoroutine(RollDiceRoutine());
    }

    // 주사위 연출 → 에너지 계산 → 유물 효과 → 잠깐 결과를 보여 준 뒤 숨김
    IEnumerator RollDiceRoutine()
    {
        diceImageObject.SetActive(true);

        // 매 턴 완전 무작위 (노드 시드와 무관). 결과를 먼저 정하고, 연출은 그 결과로 끝나게 보여 주기만 한다
        diceValue = Random.Range(1, 7);

        if (dice3D != null)
        {
            // 3D 주사위가 결과 면으로 착지 (2D 이미지는 숨김). 소리는 처음 바닥에 닿을 때
            if (diceImage != null) diceImage.enabled = false;
            yield return StartCoroutine(dice3D.Roll(diceValue, PlayDiceSound));
        }
        else
        {
            // 2D: 이미지를 빠르게 바꾸며 굴리는 느낌만
            PlayDiceSound();
            for(int i = 0; i < 10; i++)
            {
                diceImage.sprite = diceSprites[Random.Range(0, 6)];
                yield return new WaitForSeconds(0.05f);
            }
        }

        // 에너지 계산 (유물 효과가 AddEnergy로 더 올릴 수 있음)
        MaxEnergy = baseEnergy + diceValue;
        BonusEnergy = 0;
        BonusSources.Clear();
        CurrentEnergy = MaxEnergy;

        if (RelicManager.Instance != null)
        {
            RelicManager.Instance.OnDiceRolled(diceValue);
        }

        if (diceImage != null && diceSprites.Length >= 6)
            diceImage.sprite = diceSprites[diceValue - 1];

        Debug.Log($"Base Energy {baseEnergy} + Dice {diceValue} = Turn Energy: {CurrentEnergy}");

        yield return new WaitForSeconds(1f);
        diceImageObject.SetActive(false);
        if (dice3D != null) dice3D.Hide();

        isRollFinished = true; // 이걸 보고 TurnManager가 다음 단계로 진행
    }

    // 주사위 굴리는 소리
    private void PlayDiceSound()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundManager.Instance.diceRollSound);
    }

    // 카드를 낼 때 에너지 사용
    public void UseEnergy(int amount)
    {
        CurrentEnergy -= amount;
        if (CurrentEnergy < 0) CurrentEnergy = 0;
    }

    // 세이브 로드: 주사위를 다시 굴리지 않고 저장된 에너지를 그대로 복원
    public void SetCurrentEnergy(int amount, int maxAmount = 0)
    {
        // 저장된 최대치가 없는 예전 세이브(0)는 남은 에너지를 최대치로
        MaxEnergy = Mathf.Max(maxAmount, amount);
        // 유물 보너스 내역은 세이브에 없으므로 불러온 뒤엔 합계만 표시
        BonusEnergy = 0;
        BonusSources.Clear();
        CurrentEnergy = amount;
    }

    // 유물 효과로 에너지 추가 (source: 전투 로그에 표시할 유물 이름)
    public void AddEnergy(int amount, string source = null)
    {
        if (amount <= 0)
        {
            return;
        }

        // 최대치를 먼저 올려야 OnEnergyChanged를 받는 UI가 올바른 최대치로 그린다
        MaxEnergy += amount;
        BonusEnergy += amount;
        BonusSources.Add((string.IsNullOrEmpty(source) ? "유물" : source, amount));
        CurrentEnergy += amount;

        Debug.Log(
            $"[DiceManager] 추가 에너지 +{amount} " +
            $"-> 현재 에너지: {CurrentEnergy}, 최대 에너지: {MaxEnergy}"
        );
    }
}