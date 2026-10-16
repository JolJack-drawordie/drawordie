using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 월드맵(Act 진행도) 화면: 섬마다 클리어/현재/잠김 상태와 라벨(Act 제목)을 표시하고,
// 현재 Act의 섬을 누르면 그 섬으로 확대되며 안개가 몰려와 덮는 진입 연출 후 MapScene으로 이동하고,
// 맵 씬에서 안개가 양옆으로 걷힌다 (FogTransition). 안개 이미지가 없으면 검게 페이드된다.
// 진입 경로: 로비 "새 게임", Act 보스 클리어(최종 보스 제외)
public class WorldMapManager : MonoBehaviour
{
    public enum IslandState { Cleared, Current, Locked }

    [Serializable]
    public class ActInfo
    {
        public WorldMapIsland island;
        public string title;
    }

    [SerializeField] private ActInfo[] acts;

    [Tooltip("섬을 잇는 점선 (0: Act1→2, 1: Act2→3 ...)")]
    [SerializeField] private WorldMapPath[] paths;

    [Header("섬 색상")]
    [SerializeField] private Color clearedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] private Color lockedColor = new Color(0.2f, 0.2f, 0.25f, 1f);

    [Header("잠긴 섬 안내 메시지")]
    [SerializeField] private CanvasGroup lockedMessage;
    [SerializeField] private TMP_Text lockedMessageText;
    [SerializeField] private float messageDuration = 2f;
    [SerializeField] private float messageFadeTime = 0.4f;

    [Header("씬")]
    [SerializeField] private string mapSceneName = "MapScene";

    [Header("진입 연출")]
    [Tooltip("Background와 Islands를 묶은 부모 (이걸 확대/이동)")]
    [SerializeField] private RectTransform mapRoot;
    [Tooltip("화면 전체를 덮는 이미지 (평소엔 투명)")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float enterZoom = 1.8f;
    [SerializeField] private float othersFadeTime = 0.3f;
    [SerializeField] private float zoomStart = 0.15f;
    [SerializeField] private float zoomEnd = 1.05f;
    [SerializeField] private float fadeStart = 0.7f;
    [SerializeField] private float fadeEnd = 1.2f;
    [Tooltip("월드맵에 들어올 때 검은 화면에서 밝아지는 시간")]
    [SerializeField] private float fadeInTime = 0.5f;
    [Tooltip("화면이 밝아진 뒤 새로 열린 섬의 안개가 걷히기 시작할 때까지 기다리는 시간")]
    [SerializeField] private float revealDelay = 0.3f;
    [Tooltip("화면이 밝아진 뒤 방금 클리어한 섬에 도장이 찍히기 시작할 때까지 기다리는 시간")]
    [SerializeField] private float stampDelay = 0.3f;
    [Tooltip("도장이 찍힌 뒤 점선이 그려지기 시작할 때까지 기다리는 시간")]
    [SerializeField] private float pathDelay = 0.2f;

    [Header("안개 전환 (비워두면 검은 페이드)")]
    [SerializeField] private Sprite fogDense;
    [SerializeField] private Sprite fogMid;
    [SerializeField] private Sprite fogThin;
    [Tooltip("안개 사이 빈틈을 메우는 색")]
    [SerializeField] private Color fogBackColor = new Color(0.42f, 0.43f, 0.45f, 1f);
    [Tooltip("fadeStart부터 안개가 화면을 다 덮을 때까지 걸리는 시간")]
    [SerializeField] private float fogCoverTime = 0.8f;
    [Tooltip("맵 씬에 도착한 뒤 안개가 걷히기 전까지 기다리는 시간 (Act 제목이 보이는 시간)")]
    [SerializeField] private float fogHoldTime = 1f;
    [SerializeField] private float fogClearTime = 1.6f;

    [Header("테스트")]
    [Tooltip("켜면 GameFlowData 대신 아래 Act를 현재 Act로 보고 표시")]
    [SerializeField] private bool usePreviewAct = false;
    [SerializeField, Range(1, GameFlowData.maxAct)] private int previewAct = 1;

    private int currentAct;
    private Coroutine messageRoutine;
    private bool entering;

    // 이번에 안개 걷힘 연출을 할 Act (0이면 없음)
    private int revealAct;

    // 이번에 클리어 도장 찍기 연출을 할 Act (0이면 없음)
    private int stampAct;

    private void Start()
    {
        currentAct = usePreviewAct ? previewAct : GameFlowData.currentAct;

        if (lockedMessage != null)
        {
            lockedMessage.alpha = 0f;
            lockedMessage.blocksRaycasts = false;
        }

        // 새로 열린 섬(새 게임의 Act 1, 보스 클리어 후 다음 Act)은 안개가 걷히는 연출.
        // 마지막으로 보여준 Act는 GameFlowData에 두어 새 게임(ResetRun) 때 함께 초기화됨
        revealAct = currentAct != GameFlowData.worldMapRevealedAct ? currentAct : 0;
        GameFlowData.worldMapRevealedAct = currentAct;

        // 다음 Act가 새로 열렸다면 직전 Act를 방금 클리어한 것이므로 도장 연출
        stampAct = revealAct > 1 ? revealAct - 1 : 0;

        for (int i = 0; i < acts.Length; i++)
        {
            if (acts[i].island == null) continue;
            int act = i + 1;
            IslandState state = GetState(act);
            bool fogged = state == IslandState.Locked || act == revealAct;
            acts[i].island.Init(this, act, acts[i].title, state, clearedColor, lockedColor, fogged, act == stampAct);
        }

        // 길 i는 Act i+1 → i+2. 도착하는 섬이 열린 길만 보이고, 방금 열린 길은 숨겨 뒀다가 그림
        if (paths != null)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                if (paths[i] == null) continue;
                int toAct = i + 2;
                paths[i].gameObject.SetActive(toAct <= currentAct);
                if (toAct <= currentAct)
                    paths[i].Init(toAct != revealAct);
            }
        }

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayBGM(SoundManager.Instance.worldMapBackgroundSound);

        if (fadeImage != null)
            StartCoroutine(FadeInRoutine());
        else
            StartCoroutine(IntroRoutine());
    }

    // 로비/전투에서 검은 화면으로 넘어오므로 검은 화면에서 서서히 밝아짐 (그동안 클릭 막기)
    private IEnumerator FadeInRoutine()
    {
        fadeImage.raycastTarget = true;

        float time = 0f;
        while (time < fadeInTime)
        {
            time += Time.deltaTime;
            fadeImage.color = new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(time / fadeInTime));
            yield return null;
        }

        fadeImage.color = new Color(0f, 0f, 0f, 0f);

        yield return IntroRoutine();

        if (!entering)
            fadeImage.raycastTarget = false;
    }

    // 방금 클리어한 섬에 도장이 찍히고, 다음 섬으로 점선이 그려진 뒤, 새로 열린 섬의 안개가 걷힘
    private IEnumerator IntroRoutine()
    {
        if (stampAct > 0 && acts[stampAct - 1].island != null)
        {
            yield return acts[stampAct - 1].island.PlayStamp(stampDelay);
        }

        WorldMapPath newPath = GetPathTo(revealAct);
        if (newPath != null)
        {
            yield return new WaitForSeconds(pathDelay);
            yield return newPath.Draw();
        }

        if (revealAct > 0 && acts[revealAct - 1].island != null)
            yield return RevealRoutine(acts[revealAct - 1].island);
    }

    // toAct 섬으로 들어오는 길 (없으면 null)
    private WorldMapPath GetPathTo(int toAct)
    {
        int index = toAct - 2;
        if (paths == null || index < 0 || index >= paths.Length) return null;
        return paths[index];
    }

    // 새로 열린 섬의 안개가 걷히는 동안은 클릭을 막아 둠 (fadeImage가 계속 클릭을 막고 있음)
    private IEnumerator RevealRoutine(WorldMapIsland island)
    {
        yield return new WaitForSeconds(revealDelay);

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundManager.Instance.worldMapRevealSound);

        yield return island.RevealFog();
    }

    // 갈 수 있는 섬에 마우스를 올렸을 때
    public void PlayHoverSound()
    {
        if (entering) return;
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundManager.Instance.worldMapHoverSound);
    }

    // 클리어 도장이 찍히는 순간
    public void PlayStampSound()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundManager.Instance.worldMapStampSound);
    }

    public IslandState GetState(int act)
    {
        if (act < currentAct) return IslandState.Cleared;
        if (act == currentAct) return IslandState.Current;
        return IslandState.Locked;
    }

    public void Depart(int act)
    {
        if (entering || GetState(act) != IslandState.Current) return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundManager.Instance.worldMapClickSound);

        StartCoroutine(EnterRoutine(act));
    }

    // 다른 섬을 흐리게 하고, 누른 섬을 화면 가운데로 끌어오며 확대한 뒤 안개(없으면 검은색)로 덮는다
    private IEnumerator EnterRoutine(int act)
    {
        entering = true;

        WorldMapIsland target = acts[act - 1].island;

        // 연출 중 클릭 막기
        if (fadeImage != null)
            fadeImage.raycastTarget = true;

        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
            messageRoutine = null;
        }
        if (lockedMessage != null)
            lockedMessage.alpha = 0f;

        target.BeginEnter();

        bool useFog = fogDense != null;
        float coverEnd = useFog ? fadeStart + fogCoverTime : fadeEnd;
        FogTransition fog = null;

        // 화면이 덮이는 동안 BGM도 함께 줄어듦
        if (SoundManager.Instance != null)
            SoundManager.Instance.FadeOutBGM(coverEnd);
        bool enterSoundPlayed = false;

        // mapRoot 기준 섬 중심 위치 (mapRoot의 피벗이 화면 중앙)
        Vector2 islandLocal = mapRoot != null
            ? (Vector2)mapRoot.InverseTransformPoint(target.transform.position)
            : Vector2.zero;
        Vector2 startPos = mapRoot != null ? mapRoot.anchoredPosition : Vector2.zero;

        float total = Mathf.Max(coverEnd, zoomEnd);
        float time = 0f;

        while (time < total)
        {
            time += Time.deltaTime;

            float others = 1f - Mathf.Clamp01(time / othersFadeTime);
            for (int i = 0; i < acts.Length; i++)
            {
                if (acts[i].island != null && acts[i].island != target)
                    acts[i].island.SetAlpha(others);
            }
            if (paths != null)
            {
                foreach (WorldMapPath path in paths)
                {
                    if (path != null)
                        path.SetAlpha(others);
                }
            }

            // 확대가 시작되는 순간 빨려 들어가는 효과음
            if (!enterSoundPlayed && time >= zoomStart)
            {
                enterSoundPlayed = true;
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlaySFX(SoundManager.Instance.worldMapEnterSound);
            }

            if (mapRoot != null)
            {
                // 처음엔 천천히, 갈수록 빠르게 (ease-in)
                float z = Mathf.Clamp01((time - zoomStart) / (zoomEnd - zoomStart));
                z = z * z * z;
                float scale = Mathf.Lerp(1f, enterZoom, z);
                mapRoot.localScale = new Vector3(scale, scale, 1f);
                // 섬 중심이 화면 중앙으로 오도록 이동
                mapRoot.anchoredPosition = startPos - islandLocal * scale * z;
            }

            if (useFog)
            {
                // 안개는 씬이 바뀌어도 남아 있다가 맵 씬에서 걷힘
                if (fog == null && time >= fadeStart)
                {
                    fog = FogTransition.Create(fogDense, fogMid, fogThin, fogBackColor);
                    fog.SetTitle($"ACT {act}", acts[act - 1].title);
                    fog.StartCoroutine(fog.Cover(fogCoverTime));

                    if (SoundManager.Instance != null)
                        SoundManager.Instance.PlaySFX(SoundManager.Instance.fogCoverSound);
                }
            }
            else if (fadeImage != null)
            {
                float f = Mathf.Clamp01((time - fadeStart) / (fadeEnd - fadeStart));
                fadeImage.color = new Color(0f, 0f, 0f, f);
            }

            yield return null;
        }

        Debug.Log($"[WorldMap] Act {act} 출발");
        if (fog != null)
            fog.LoadSceneAndClear(mapSceneName, fogHoldTime, fogClearTime);
        else
            SceneManager.LoadScene(mapSceneName);
    }

    // 잠긴 섬을 눌렀을 때 화면 아래에 잠깐 안내 문구를 띄운다
    public void ShowLockedMessage(int act)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundManager.Instance.worldMapLockedSound);

        if (lockedMessage == null || lockedMessageText == null) return;

        // 숫자 읽기에 맞춘 조사 (1을, 2를, 3을)
        int prev = act - 1;
        string particle = prev % 10 is 2 or 4 or 5 or 9 ? "를" : "을";
        lockedMessageText.text = $"먼저 Act {prev}{particle} 클리어해야 합니다.";

        if (messageRoutine != null)
            StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(ShowMessageRoutine());
    }

    private IEnumerator ShowMessageRoutine()
    {
        lockedMessage.alpha = 1f;
        yield return new WaitForSeconds(messageDuration);

        float t = 0f;
        while (t < messageFadeTime)
        {
            t += Time.deltaTime;
            lockedMessage.alpha = 1f - t / messageFadeTime;
            yield return null;
        }

        lockedMessage.alpha = 0f;
        messageRoutine = null;
    }
}
