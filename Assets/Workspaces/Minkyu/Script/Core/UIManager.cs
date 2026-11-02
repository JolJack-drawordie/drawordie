using System.Collections;
using UnityEngine.Video;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// 전투 화면 UI: 에너지 게이지, 체력 / 방어도 바 연결, 전투 결과(승리 보상 / 패배 / 게임 클리어 엔딩) 처리.
public class UIManager : MonoBehaviour
{
    [Header("에너지 UI")]
    public Slider energySlider;
    public TextMeshProUGUI energySliderText;
    public TextMeshProUGUI energyText;

    // 체력 / 방어도 바와, 바가 읽을 값을 주는 Provider (유닛이 생성되면 LinkUnitToUI로 연결)
    public AnimatedBar playerHpBar;
    public AnimatedBar playerShieldBar;
    public AnimatedBar enemyHpBar;
    public AnimatedBar enemyShieldBar;

    private HpProvider playerHpProvider;
    private HpProvider enemyHpProvider;
    private ShieldProvider playerShieldProvider;
    private ShieldProvider enemyShieldProvider;

    [Header("전투 결과 UI")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public GameObject rewardPanel;
    public TextMeshProUGUI rewardText;

    [Header("클리어 랭킹 UI")]
    public Button clearRankingButton;

    [SerializeField] private VideoClip endingVideo; // 게임 클리어 엔딩 영상
    private bool leavingAfterClear; // 클리어 연출을 이미 시작했는지 (중복 방지)

    public static UIManager Instance;

    private void Awake()
    {
        Instance = this;

        // 결과 랭킹 씬처럼 로비 버튼용으로만 둔 UIManager에는 resultPanel이 없음
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

    // ===== 에너지 UI =====

    // 에너지가 바뀔 때마다 게이지와 "현재 / 최대" 표시 갱신
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

    // ===== 전투 결과 =====

    private bool resultWaitingForBossDeath; // 보스 처치 연출이 끝나기를 기다리는 중인지

    // 승리: 일반 전투는 보상 선택, 마지막 Act 보스는 게임 클리어 / 패배: 결과 랭킹 화면
    public void ShowResult(bool isVictory)
    {
        // 보스 처치 연출(BossDeathSequence)이 끝날 때까지 결과 / 보상 화면을 미룸
        if (isVictory && BossDeathSequence.IsPlaying)
        {
            if (!resultWaitingForBossDeath) StartCoroutine(ShowResultAfterBossDeath());
            return;
        }

        if (isVictory)
        {
            if (GameFlowData.currentNodeType == MapNode.NodeType.Boss && GameFlowData.IsFinalAct()) // 게임 클리어
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
                // 보상을 고르기 전에 손패와 버린 카드를 덱으로 되돌려 둔다
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
        }
    }

    // 보스 처치 연출이 끝나면 결과 표시
    private IEnumerator ShowResultAfterBossDeath()
    {
        resultWaitingForBossDeath = true;
        while (BossDeathSequence.IsPlaying) yield return null;
        resultWaitingForBossDeath = false;
        ShowResult(true);
    }

    private IEnumerator PlayDefeatSoundWithDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundManager.Instance.defeatSound);
    }

    // 패배: 몇 초 뒤 결과 랭킹 창
    private IEnumerator OpenResultWithDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        if (resultText != null)
        {
            resultText.text = "";
        }

        RankingPanel.OpenResult();
    }

    public void HideResult()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    // ===== 승리 후 이동 =====

    // 보상을 고른 뒤 다음으로: 일반 노드는 맵, Act 보스는 월드맵, 마지막 Act 보스는 엔딩
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

        // 보스를 잡으면 월드맵에서 다음 Act 섬이 열리는 연출을 보여 주고, 섬을 눌러 다음 맵으로 이동
        SceneManager.LoadScene(isBossNode ? "WorldMapScene" : "MapScene");

        if (SoundManager.Instance != null)
            SoundManager.Instance.StopBGM();
    }

    // 게임 클리어: 팡파레 → 엔딩 영상 → 결과 랭킹 창 (엔딩 영상의 마지막 화면이 랭킹 창 배경으로 남음)
    private IEnumerator PlayEndingAndReturnToLobby()
    {
        // 팡파레가 끝난 뒤 엔딩 영상 시작 (팡파레가 없거나 짧으면 3초)
        float waitSeconds = 3.0f;
        if (SoundManager.Instance != null && SoundManager.Instance.victorySound != null)
            waitSeconds = Mathf.Max(waitSeconds, SoundManager.Instance.victorySound.length);

        yield return new WaitForSeconds(waitSeconds);

        if (resultPanel != null)
            resultPanel.SetActive(false);

        // 보상 처리가 모두 끝나 패널을 닫은 다음 프레임부터 컷씬 시작
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

        // 패배 때처럼 글자 없는 결과 패널로 아래 손패 / 덱 더미의 클릭 · 호버를 막음
        if (resultText != null)
            resultText.text = "";
        if (resultPanel != null)
            resultPanel.SetActive(true);

        RankingPanel.OpenResult();
    }
    // ===== 유닛 UI 연결 =====

    // 생성된 유닛을 해당 쪽(플레이어 / 적) 체력 · 방어도 바에 연결
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