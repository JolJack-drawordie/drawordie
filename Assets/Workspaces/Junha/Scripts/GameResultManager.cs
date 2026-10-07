using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

// 게임이 끝났을 때(최종 보스 처치 / 사망) 결과를 서버 랭킹에 등록한다.
// 점수는 서버에서 계산하므로 원래 값(클리어 여부, 도달 Act/층, 플레이 타임)만 보낸다.
// 서버는 결과 등록 시 해당 유저의 세이브 데이터를 삭제한다.
public class GameResultManager : MonoBehaviour
{
    public static GameResultManager Instance;

    private const string ResultUrl = "http://localhost:8080/api/game/result";

    // 결과 등록 요청 중인지 (랭킹 화면은 등록이 끝난 뒤 조회해야 내 기록이 포함됨)
    public bool IsSubmitting { get; private set; }

    // 가장 최근에 등록한 내 기록 (없으면 null)
    public GameResultResponse LastResult { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad()
    {
        if (Instance != null) return;

        GameObject obj = new GameObject("GameResultManager");
        obj.AddComponent<GameResultManager>();
        DontDestroyOnLoad(obj);
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 런을 종료하고 결과를 서버에 등록한다.
    // 새 게임 / 불러오기로 시작한 런이 진행 중일 때만 한 번 등록된다.
    public void EndRun(bool cleared)
    {
        if (PlayTimeTracker.Instance == null || !PlayTimeTracker.Instance.StopRun()) return;

        LastResult = null;

        if (!AuthManager.isLoggedIn)
        {
            Debug.LogWarning("[GameResultManager] 로그인되어 있지 않아 결과를 등록할 수 없습니다.");
            return;
        }

        IsSubmitting = true;
        StartCoroutine(SubmitResultRoutine(
            cleared,
            GameFlowData.currentAct,
            // currentFloor는 0부터 시작하므로 1부터 세도록 보정 (맵 진입 전이면 0)
            Mathf.Max(0, GameFlowData.currentFloor + 1),
            PlayTimeTracker.Instance.GetElapsedSecondsInt()));
    }

    private IEnumerator SubmitResultRoutine(bool cleared, int reachedAct, int reachedFloor, int playTime)
    {
        WWWForm form = new WWWForm();
        form.AddField("cleared", cleared ? "true" : "false");
        form.AddField("reachedAct", reachedAct);
        form.AddField("reachedFloor", reachedFloor);
        form.AddField("playTime", playTime);

        using (UnityWebRequest www = UnityWebRequest.Post(ResultUrl, form))
        {
            // 유저 번호는 서버가 토큰에서 꺼내 씀
            AuthManager.SetAuthHeader(www);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                LastResult = JsonUtility.FromJson<GameResultResponse>(www.downloadHandler.text);
                Debug.Log($"<color=green>[GameResultManager] 결과 등록 완료! {LastResult.rank}위 " +
                          $"(클리어: {cleared}, Act {reachedAct}, {reachedFloor}층, {playTime}초)</color>");
            }
            else if (www.responseCode == 401)
            {
                Debug.LogError("[GameResultManager] 결과 등록 실패: 로그인이 만료되었습니다.");
            }
            else
            {
                Debug.LogError("[GameResultManager] 결과 등록 실패: " + www.error);
            }
        }

        IsSubmitting = false;
    }
}
