#if UNITY_EDITOR
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

// 에디터 전용: 전투나 랭킹 등록 없이 실제 컷씬 재생과 로비 복귀를 확인한다.
public class EndingCutscenePreview : MonoBehaviour
{
    private const string PendingKey = "DrawOrDie.EndingPreview";
    private const string VideoPath = "Assets/Workspaces/Taeyoon/gemini_generated_video_2883d136 (online-video-cutter.com).mp4";

    [MenuItem("Tools/Cutscenes/엔딩 컷씬 테스트")]
    private static void Preview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (EditorApplication.isPlaying && FindFirstObjectByType<EndingCutscenePreview>() == null)
                new GameObject("EndingCutscenePreview").AddComponent<EndingCutscenePreview>();
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Workspaces/Taeyoon/Scenes/LobbyScene.unity");
        SessionState.SetBool(PendingKey, true);
        EditorApplication.isPlaying = true;
    }

    [InitializeOnLoadMethod]
    private static void Register()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            SessionState.SetBool(PendingKey, false);
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
        SessionState.SetBool(PendingKey, false);
        new GameObject("EndingCutscenePreview").AddComponent<EndingCutscenePreview>();
    }

    private IEnumerator Start()
    {
        // 로비의 Start에서 BGM 등 초기화가 끝난 뒤 재생한다.
        yield return null;
        VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>(VideoPath);
        if (clip == null)
        {
            Debug.LogError("[CutscenePreview] Ending video not found: " + VideoPath);
            Destroy(gameObject);
            yield break;
        }
        if (SoundManager.Instance != null && SoundManager.Instance.sfxSource != null)
            SoundManager.Instance.sfxSource.Stop();
        yield return WorldMapManager.PlayCutscene(clip);
        SceneManager.LoadScene("LobbyScene");
    }
}
#endif