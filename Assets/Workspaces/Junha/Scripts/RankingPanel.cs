using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 랭킹 팝업. RankingScene에 배치되며, AuthScene처럼 Additive로 불러와 현재 화면 위에 띄운다.
// 서버 /api/game/ranking 의 상위 기록을 보여주고, 가장 최근에 등록한 내 기록은 노란색으로 강조한다.
// 여는 법: RankingPanel.Open() / Toggle() (로비, 게임 클리어 화면)
// 로비에서는 오른쪽 메뉴 버튼을 가리지 않도록 창을 왼쪽 빈 공간에 띄우고, 배경을 어둡게 하지 않는다.
// 씬 생성: Tools > Ranking > 랭킹 씬 생성
public class RankingPanel : MonoBehaviour
{
    public const string SceneName = "RankingScene";

    private const string RankingUrl = "http://localhost:8080/api/game/ranking";
    private const string MyRecordColor = "#FFD54A";

    [SerializeField] private TMP_Text myRankText;
    [SerializeField] private TMP_Text listText;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Button closeButton;

    [Header("창 배치")]
    // 비워두면 목록(ScrollRect)의 부모를 창, 창의 부모를 어두운 배경으로 사용
    [SerializeField] private RectTransform windowRect;
    [SerializeField] private Image dimImage;
    [SerializeField] private string lobbySceneName = "LobbyScene";
    // 로비에서 화면 왼쪽 끝으로부터의 창 위치 (1920x1080 기준)
    [SerializeField] private Vector2 lobbyWindowOffset = new Vector2(80f, 0f);

    // 여는 중에 버튼을 여러 번 눌러 중복 로드되는 것을 막음
    private static bool isOpening;

    public static void Open()
    {
        if (isOpening || SceneManager.GetSceneByName(SceneName).isLoaded) return;

        AsyncOperation op = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
        if (op == null)
        {
            Debug.LogError("[RankingPanel] RankingScene 로드 실패 - Build Settings 확인 필요");
            return;
        }

        isOpening = true;
        op.completed += _ => isOpening = false;
    }

    // 열려 있으면 닫고, 닫혀 있으면 연다 (로비 랭킹 버튼)
    public static void Toggle()
    {
        Scene scene = SceneManager.GetSceneByName(SceneName);
        if (scene.isLoaded)
            SceneManager.UnloadSceneAsync(scene);
        else
            Open();
    }

    public void Close()
    {
        SceneManager.UnloadSceneAsync(gameObject.scene);
    }

    private void Start()
    {
        // Additive 로드 시 아래 화면을 덮지 않도록 이 씬의 카메라와 오디오 리스너를 비활성화
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            foreach (Camera cam in root.GetComponentsInChildren<Camera>(true))
                cam.enabled = false;

            foreach (AudioListener listener in root.GetComponentsInChildren<AudioListener>(true))
                listener.enabled = false;
        }

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (SceneManager.GetActiveScene().name == lobbySceneName)
            ApplyLobbyLayout();

        StartCoroutine(LoadRankingRoutine());
    }

    // 로비: 창을 왼쪽 빈 공간에 배치하고, 배경을 어둡게 하지 않아 로비 버튼을 계속 누를 수 있게 함
    private void ApplyLobbyLayout()
    {
        if (windowRect == null && scrollRect != null)
            windowRect = scrollRect.transform.parent as RectTransform;
        if (dimImage == null && windowRect != null && windowRect.parent != null)
            dimImage = windowRect.parent.GetComponent<Image>();

        if (dimImage != null)
        {
            dimImage.color = Color.clear;
            dimImage.raycastTarget = false;
        }

        if (windowRect != null)
        {
            windowRect.anchorMin = windowRect.anchorMax = new Vector2(0f, 0.5f);
            windowRect.pivot = new Vector2(0f, 0.5f);
            windowRect.anchoredPosition = lobbyWindowOffset;
        }
    }

    // =========================
    // 랭킹 불러오기
    // =========================

    private IEnumerator LoadRankingRoutine()
    {
        myRankText.text = "";
        listText.text = "불러오는 중...";

        // 방금 끝난 게임의 결과 등록이 끝나야 내 기록이 랭킹에 포함됨
        while (GameResultManager.Instance != null && GameResultManager.Instance.IsSubmitting)
            yield return null;

        GameResultResponse myResult =
            GameResultManager.Instance != null ? GameResultManager.Instance.LastResult : null;

        if (myResult != null)
        {
            myRankText.text = myResult.rank <= 100
                ? $"최근 기록: {myResult.rank}위"
                : $"최근 기록: {myResult.rank}위 (상위 100위 밖)";
        }

        using (UnityWebRequest www = UnityWebRequest.Get(RankingUrl))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[RankingPanel] 랭킹 불러오기 실패: " + www.error);
                listText.text = "랭킹을 불러오지 못했습니다.";
                yield break;
            }

            RankingListDto data = JsonUtility.FromJson<RankingListDto>(www.downloadHandler.text);
            listText.text = BuildListText(data, myResult != null ? myResult.resultId : -1);
        }

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f; // 맨 위로
    }

    private string BuildListText(RankingListDto data, long myResultId)
    {
        if (data == null || data.rankings == null || data.rankings.Count == 0)
            return "아직 등록된 기록이 없습니다.";

        StringBuilder sb = new StringBuilder();

        foreach (RankingEntry entry in data.rankings)
        {
            bool isMine = entry.resultId == myResultId;
            if (isMine) sb.Append($"<color={MyRecordColor}>");

            string progress = entry.cleared
                ? "<color=#FF5A4A>CLEAR</color>"
                : $"Act {entry.reachedAct} - {entry.reachedFloor}층";

            // 날짜만 표시 ("2026-09-27T12:34:56" → "2026-09-27")
            string date = !string.IsNullOrEmpty(entry.endedAt) && entry.endedAt.Length >= 10
                ? entry.endedAt.Substring(0, 10)
                : "-";

            sb.Append(entry.rank);
            sb.Append("<pos=8%><noparse>").Append(TrimNickname(entry.nickname)).Append("</noparse>");
            sb.Append("<pos=35%>").Append(progress);
            sb.Append("<pos=55%>").Append(entry.score.ToString("N0"));
            sb.Append("<pos=71%>").Append(PlayTimeTracker.FormatTime(entry.playTime));
            sb.Append("<pos=84%>").Append(date);

            if (isMine) sb.Append("</color>");
            sb.Append('\n');
        }

        return sb.ToString();
    }

    private static string TrimNickname(string nickname)
    {
        if (string.IsNullOrEmpty(nickname)) return "-";

        // noparse 태그를 깨지 않도록 제거하고, 다음 열을 침범하지 않도록 길이 제한
        nickname = nickname.Replace("</noparse>", "");
        return nickname.Length > 10 ? nickname.Substring(0, 10) + "…" : nickname;
    }
}
