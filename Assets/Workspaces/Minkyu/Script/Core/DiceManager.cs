using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DiceManager : MonoBehaviour
{
    public static DiceManager Instance;

    [Header("에너지 설정")]
    public int baseEnergy = 3;
    public int diceValue;

    // ⭐️ 1. 에너지가 변경될 때 외부에 알릴 이벤트 추가
    public event System.Action<int> OnEnergyChanged;

    private int currentEnergy;
    public int CurrentEnergy
    {
        get => currentEnergy;
        private set
        {
            currentEnergy = value;
            // ⭐️ 2. 값이 바뀔 때마다 이벤트 발사!
            OnEnergyChanged?.Invoke(currentEnergy);
        }
    }

    [Header("주사위 UI")]
    public Button rollDiceButton; // 버튼 연결
    public GameObject diceImageObject; // 주사위 이미지 부모
    public Image diceImage;
    public Sprite[] diceSprites;

    [Tooltip("3D 주사위가 굴러가는 연출 사용 (끄거나 dice3D가 비어 있으면 기존처럼 이미지만 바뀜)")]
    public bool use3DDice = true;
    // Tools > Dice > 3D 주사위 프리팹 만들기 로 씬에 배치
    public Dice3DRoller dice3D;

    public bool isRollFinished = false; // 턴매니저 대기용 플래그

    // 이번 턴에 주사위로 얻은 에너지 (에너지 게이지의 최대치로 사용)
    public int MaxEnergy { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // 씬에 이미 존재한다면 중복 생성 방지
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

    IEnumerator RollDiceRoutine()
    {
        diceImageObject.SetActive(true);
        
        // 주사위 굴리는 소리
        if (SoundManager.Instance != null) {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.diceRollSound);
        }

        // 매 턴 완전 랜덤 (노드 시드와 무관). 결과를 먼저 정하고, 연출은 그 결과로 끝나도록 보여주기만 함
        diceValue = Random.Range(1, 7);

        // 주사위 굴러가는 애니메이션
        if (dice3D != null)
        {
            // 3D 주사위가 결과 면으로 착지 (2D 이미지는 숨김)
            if (diceImage != null) diceImage.enabled = false;
            yield return StartCoroutine(dice3D.Roll(diceValue));
        }
        else
        {
            for(int i = 0; i < 10; i++)
            {
                diceImage.sprite = diceSprites[Random.Range(0, 6)];
                yield return new WaitForSeconds(0.05f);
            }
        }

        // 이벤트가 발생하기 전에 최대치를 먼저 갱신해야 UI가 올바른 비율로 그려짐
        MaxEnergy = baseEnergy + diceValue;
        CurrentEnergy = MaxEnergy;
        
        if (diceImage != null && diceSprites.Length >= 6)
            diceImage.sprite = diceSprites[diceValue - 1];

        Debug.Log($"Base Energy {baseEnergy} + Dice {diceValue} = Turn Energy: {CurrentEnergy}");

        yield return new WaitForSeconds(1f);
        diceImageObject.SetActive(false);
        if (dice3D != null) dice3D.Hide();

        isRollFinished = true; // 주사위가 끝나면 TurnManager가 다음을 진행함!
    }

    public void UseEnergy(int amount)
    {
        CurrentEnergy -= amount;
        if (CurrentEnergy < 0) CurrentEnergy = 0;
    }

    // ⭐ [로드 기능] 신규 메서드
    // 세이브 로드 시 주사위를 다시 굴리지 않고 저장된 코스트를 그대로 복원
    public void SetCurrentEnergy(int amount, int maxAmount = 0)
    {
        // 저장된 최대치가 없는 구버전 세이브(0)는 남은 코스트를 최대치로 사용
        MaxEnergy = Mathf.Max(maxAmount, amount);
        CurrentEnergy = amount;
    }
}