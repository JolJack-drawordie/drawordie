using System.Collections;
using UnityEngine.Video;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("에너지 UI")]
    public Slider energySlider;
    public TextMeshProUGUI energySliderText;
    public TextMeshProUGUI energyText;

    [Header("기본 UI")]
    public TextMeshProUGUI playerHpText;
    public TextMeshProUGUI playerShieldText;
    public TextMeshProUGUI enemyHpText;
    public TextMeshProUGUI enemyShieldText;

    public AnimatedBar playerHpBar;
    public AnimatedBar playerShieldBar;
    public AnimatedBar enemyHpBar;
    public AnimatedBar enemyShieldBar;

    private HpProvider playerHpProvider;
    private HpProvider enemyHpProvider;
    private ShieldProvider playerShieldProvider;
    private ShieldProvider enemyShieldProvider;

    [Header("참조")]
    public DiceManager diceManager;

    [Header("전투 결과 UI")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public GameObject rewardPanel;
    public TextMeshProUGUI rewardText;

    [Header("클리어 랭킹 UI")]
    public Button clearRankingButton;

    [SerializeField] private VideoClip endingVideo;
    private bool leavingAfterClear;

    private Button defeatLobbyButton;

    public static UIManager Instance;

    private void Awake()
    {
        Instance = this;

        // 결과 랭킹 씬처럼 로비 이동 버튼용으로만 둔 UIManager에는 resultPanel이 없음
        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (clearRankingButton != null)
        {
            clearRankingButton.onClick.AddListener(RankingPanel.Open);
            clearRankingButton.gameObject.SetActive(false);
        }

        if (playerHpBar != null)
            playerHpProvider = playerHpBar.GetComponent<HpProvider>();

        if (playerShieldBar != null)
            playerShieldProvider = playerShieldBar.GetComponent<ShieldProvider>();

        if (enemyHpBar != null)
            enemyHpProvider = enemyHpBar.GetComponent<HpProvider>();

        if (enemyShieldBar != null)
            enemyShieldProvider = enemyShieldBar.GetComponent<ShieldProvider>();
    }

    private void OnEnable()
    {
        if (DiceManager.Instance != null)
            DiceManager.Instance.OnEnergyChanged += UpdateEnergyUI;
    }

    private void OnDisable()
    {
        if (DiceManager.Instance != null)
            DiceManager.Instance.OnEnergyChanged -= UpdateEnergyUI;
    }

    private void Start()
    {
        if (DiceManager.Instance != null)
            UpdateEnergyUI(DiceManager.Instance.CurrentEnergy);
    }

    // =========================
    // 에너지 UI
    // =========================

    public void UpdateEnergyUI(int newEnergy)
    {
        int maxEnergy = DiceManager.Instance != null
            ? DiceManager.Instance.MaxEnergy
            : newEnergy;

        if (energySlider != null)
        {
            energySlider.interactable = false;
            energySlider.minValue = 0;
            energySlider.maxValue = Mathf.Max(1, maxEnergy);
            energySlider.value = newEnergy;
        }

        if (energySliderText != null)
            energySliderText.text = $"{newEnergy} / {maxEnergy}";

        if (energyText != null)
            energyText.text = "Energy : " + newEnergy;
    }

    // =========================
    // 전투 결과
    // =========================

    public void ShowResult(bool isVictory)
    {
        if (isVictory)
        {
            if (GameFlowData.currentNodeType == MapNode.NodeType.Boss && GameFlowData.IsFinalAct()) // 게임 클리어 시
            {
                if (rewardPanel != null)
                    rewardPanel.SetActive(false);

                if (resultPanel != null)
                    resultPanel.SetActive(true);

                if (resultText != null)
                    resultText.text = "게임 승리";

                GoToMapAfterVictory();
            }
            else
            {
                if (DeckManager.Instance != null)
                {
                    DeckManager.Instance.DiscardHand();
                    DeckManager.Instance.RefillDeckFromDiscard(true);
                    DeckManager.Instance.RefillDeckFromDiscard(false);
                }

                if (rewardPanel != null)
                    rewardPanel.SetActive(true);

                if (rewardText != null)
                    rewardText.text = "승리! 보상을 선택하세요.";

                if (BattleRewardManager.Instance != null)
                    BattleRewardManager.Instance.GenerateRewardChoices();
            }
        }
        else
        {
            if (rewardPanel != null)
                rewardPanel.SetActive(false);

            if (resultPanel != null)
                resultPanel.SetActive(true);

            if (resultText != null)
                resultText.text = "게임 패배";

            if (SoundManager.Instance != null)
            {
                // 전투 BGM은 서서히 끄고, 사망음 뒤에 패배 징글
                SoundManager.Instance.FadeOutBGM(1.5f);
                SoundManager.Instance.PlaySFX(SoundManager.Instance.deathSound);
                StartCoroutine(PlayDefeatSoundWithDelay(1.0f));
            }

            StartCoroutine(OpenResultWithDelay(3.0f));

            //CreateDefeatLobbyButton();
        }
    }

    private IEnumerator PlayDefeatSoundWithDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundManager.Instance.defeatSound);
    }

    private IEnumerator OpenResultWithDelay(float delaySeconds)
    {
        // 지정한 시간(초)만큼 대기 (게임 속도나 유니티 타임스케일에 영향 안 받고 싶다면 WaitForSecondsRealtime 사용 가능)
        yield return new WaitForSeconds(delaySeconds);

        if (resultText != null)
        {
            resultText.text = "";
        }

        // 3초 뒤에 결과용 랭킹 씬 오픈
        RankingPanel.OpenResult();
    }

    public void HideResult()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (defeatLobbyButton != null)
            defeatLobbyButton.gameObject.SetActive(false);
    }

    // =========================
    // 패배 후 로비 이동 버튼
    // =========================

    private void CreateDefeatLobbyButton()
    {
        if (resultPanel == null)
        {
            Debug.LogError("[UIManager] ResultPanel이 연결되지 않았습니다.");
            return;
        }

        // 이미 생성된 버튼이 있으면 중복 생성하지 않음
        if (defeatLobbyButton != null)
        {
            defeatLobbyButton.gameObject.SetActive(true);
            return;
        }

        GameObject buttonObject = new GameObject(
            "DefeatLobbyButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );

        buttonObject.transform.SetParent(resultPanel.transform, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -60f);
        rect.sizeDelta = new Vector2(220f, 55f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.25f, 0.12f, 0.12f, 1f);

        defeatLobbyButton = buttonObject.GetComponent<Button>();
        defeatLobbyButton.targetGraphic = image;
        defeatLobbyButton.onClick.AddListener(GoToLobbyAfterDefeat);

        GameObject textObject = new GameObject(
            "ButtonText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );

        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI buttonText =
            textObject.GetComponent<TextMeshProUGUI>();

        buttonText.text = "로비로 돌아가기";
        buttonText.fontSize = 24;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;

        Debug.Log("[UIManager] 패배 후 로비 이동 버튼을 생성했습니다.");
    }

    public void GoToLobbyAfterDefeat()
    {
        Debug.Log("[UIManager] 패배 후 LobbyScene으로 이동합니다.");

        if (SoundManager.Instance != null)
            SoundManager.Instance.StopBGM();

        // 새 게임 시작 시 LobbyManager가 런 상태와 플레이어 스탯을 초기화함
        SceneManager.LoadScene("LobbyScene");
    }

    // =========================
    // 승리 후 맵 이동
    // =========================

    public void GoToMapAfterVictory()
    {
        bool isBossNode =
            GameFlowData.currentNodeType == MapNode.NodeType.Boss;

        if (isBossNode)
        {
            if (GameFlowData.IsFinalAct())
            {
                Debug.Log("최종 보스 클리어! LobbyScene으로 이동합니다.");

                // ShowResult가 여러 번 불려도 팡파레와 엔딩은 한 번만
                if (leavingAfterClear) return;
                leavingAfterClear = true;

                // 전투 BGM은 서서히 끄고 클리어 팡파레
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.FadeOutBGM(1.5f);
                    SoundManager.Instance.PlaySFX(SoundManager.Instance.victorySound);
                }

                StartCoroutine(PlayEndingAndReturnToLobby());
                return;
            }

            Debug.Log(
                $"Act {GameFlowData.currentAct} 클리어! 다음 Act로 이동합니다."
            );

            GameFlowData.MoveToNextAct();
        }

        GameFlowData.clearedNodeLevel++;

        // 보스를 잡으면 월드맵에서 다음 Act 섬이 열린 걸 보여주고, 섬을 눌러 다음 맵으로 이동
        SceneManager.LoadScene(isBossNode ? "WorldMapScene" : "MapScene");

        if (SoundManager.Instance != null)
            SoundManager.Instance.StopBGM();
    }

    private IEnumerator PlayEndingAndReturnToLobby()
    {
        // 팡파레가 끝난 뒤 엔딩 영상 시작 (팡파레가 없거나 짧으면 기본 3초)
        float waitSeconds = 3.0f;
        if (SoundManager.Instance != null && SoundManager.Instance.victorySound != null)
            waitSeconds = Mathf.Max(waitSeconds, SoundManager.Instance.victorySound.length);

        yield return new WaitForSeconds(waitSeconds);

        if (resultPanel != null)
            resultPanel.SetActive(false);

        // 보상 처리가 모두 끝나 패널을 닫은 다음 프레임부터 컷씬을 시작한다.
        if (clearRankingButton != null)
            clearRankingButton.gameObject.SetActive(false);
        if (SoundManager.Instance != null && SoundManager.Instance.sfxSource != null)
            SoundManager.Instance.sfxSource.Stop();
        yield return null;
        yield return WorldMapManager.PlayCutscene(endingVideo, false);

        // 클리어 랭킹 화면 BGM (로비로 가면 LobbyManager가 메인 BGM으로 바꿈)
        SoundManager sound = SoundManager.Instance;
        if (sound != null)
        {
            AudioClip endingClip = sound.endingRankingBackgroundSound != null
                ? sound.endingRankingBackgroundSound
                : sound.mainBackgroundSound;
            sound.PlayBGM(endingClip);
        }

        // 패배 때처럼 텍스트 없는 결과 패널로 아래 손패/덱 더미의 클릭·호버를 막음
        if (resultText != null)
            resultText.text = "";
        if (resultPanel != null)
            resultPanel.SetActive(true);

        RankingPanel.OpenResult();
        //SceneManager.LoadScene("LobbyScene");
    }
    // =========================
    // 유닛 UI 연결
    // =========================

    public void LinkUnitToUI(UnitBase unit)
    {
        if (unit == null)
            return;

        if (unit is PlayerUnit)
        {
            if (playerHpProvider != null && playerHpBar != null)
            {
                playerHpProvider.SetTarget(unit);
                playerHpBar.SetProvider(playerHpProvider);
            }

            if (playerShieldProvider != null && playerShieldBar != null)
            {
                playerShieldProvider.SetTarget(unit);
                playerShieldBar.SetProvider(playerShieldProvider);
            }
        }
        else if (unit is EnemyUnit)
        {
            if (enemyHpProvider != null && enemyHpBar != null)
            {
                enemyHpProvider.SetTarget(unit);
                enemyHpBar.SetProvider(enemyHpProvider);
            }

            if (enemyShieldProvider != null && enemyShieldBar != null)
            {
                enemyShieldProvider.SetTarget(unit);
                enemyShieldBar.SetProvider(enemyShieldProvider);
            }
        }
    }
}