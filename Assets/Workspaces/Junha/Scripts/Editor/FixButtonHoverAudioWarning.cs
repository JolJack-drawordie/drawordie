using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// AuthScene 버튼들이 AudioSource.PlayOneShot을 onClick / EventTrigger(PointerEnter)에서 직접 호출하는데,
// 클릭 시 OpenRegister/OpenLogin 등이 같은 onClick 안에서 먼저 실행되어 버튼(패널)을 비활성화시키면
// 뒤이어 실행되는 PlayOneShot 호출이 "Can not play a disabled audio source" 경고를 낸다.
// 이 툴은 onClick / EventTrigger에서 AudioSource를 직접 부르는 리스너를 안전한 래퍼 호출로 "직접 필드를 덮어써서"
// 교체하고, 소리 재생 리스너가 항상 맨 앞에 오도록 재배치한다 (UnityEventTools API의 타겟 바인딩 문제를 피하기 위함).
// 사용법: AuthScene을 연 상태에서 Tools > Auth > 버튼 사운드 경고 수정 실행.
public static class FixButtonHoverAudioWarning
{
    [MenuItem("Tools/Auth/버튼 사운드 경고 수정")]
    private static void Fix()
    {
        AuthManager authManager = Object.FindFirstObjectByType<AuthManager>();
        if (authManager == null)
        {
            Debug.LogError("[FixButtonHoverAudioWarning] 씬에서 AuthManager를 찾을 수 없습니다. AuthScene을 열어주세요.");
            return;
        }

        Undo.SetCurrentGroupName("버튼 사운드 경고 수정");
        int undoGroup = Undo.GetCurrentGroup();

        int fixedCount = 0;

        foreach (GameObject root in authManager.gameObject.scene.GetRootGameObjects())
        {
            foreach (AudioSource source in root.GetComponentsInChildren<AudioSource>(true))
            {
                GameObject go = source.gameObject;

                Button button = go.GetComponent<Button>();
                if (button != null)
                {
                    SerializedObject so = new SerializedObject(button);
                    SerializedProperty callsProp = FindCallsArray(so.FindProperty("m_OnClick"));
                    FixAudioCalls(button, callsProp, source, ref fixedCount);
                    LogCurrentOrder(button.gameObject.name, callsProp);
                }

                EventTrigger trigger = go.GetComponent<EventTrigger>();
                if (trigger != null)
                {
                    SerializedObject so = new SerializedObject(trigger);
                    SerializedProperty delegates = so.FindProperty("m_Delegates");
                    if (delegates != null)
                    {
                        for (int i = 0; i < delegates.arraySize; i++)
                        {
                            SerializedProperty callsProp = FindCallsArray(delegates.GetArrayElementAtIndex(i).FindPropertyRelative("callback"));
                            FixAudioCalls(trigger, callsProp, source, ref fixedCount);
                        }
                    }
                }
            }
        }

        Undo.CollapseUndoOperations(undoGroup);

        if (fixedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(authManager.gameObject.scene);
            Debug.Log($"<color=green>[FixButtonHoverAudioWarning] 총 {fixedCount}개 수정 완료. 씬을 저장하세요 (Ctrl+S).</color>");
        }
        else
        {
            Debug.Log("[FixButtonHoverAudioWarning] 수정할 대상을 찾지 못했습니다 (이미 수정되었거나 구조가 다릅니다).");
        }
    }

    private static void LogCurrentOrder(string name, SerializedProperty callsProp)
    {
        if (callsProp == null || callsProp.arraySize == 0) return;

        callsProp.serializedObject.Update();
        var parts = new System.Collections.Generic.List<string>();
        for (int k = 0; k < callsProp.arraySize; k++)
        {
            SerializedProperty call = callsProp.GetArrayElementAtIndex(k);
            SerializedProperty target = call.FindPropertyRelative("m_Target");
            SerializedProperty methodName = call.FindPropertyRelative("m_MethodName");
            string targetName = target != null && target.objectReferenceValue != null ? target.objectReferenceValue.GetType().Name : "null";
            parts.Add($"[{k}] {targetName}.{(methodName != null ? methodName.stringValue : "?")}");
        }
        Debug.Log($"[FixButtonHoverAudioWarning] '{name}' 리스너 순서: {string.Join(" -> ", parts)}");
    }

    private static SerializedProperty FindCallsArray(SerializedProperty unityEventProp)
    {
        if (unityEventProp == null) return null;
        SerializedProperty persistentCalls = unityEventProp.FindPropertyRelative("m_PersistentCalls");
        return persistentCalls != null ? persistentCalls.FindPropertyRelative("m_Calls") : null;
    }

    // calls 배열에서 AudioSource를 직접(또는 잘못) 가리키는 항목을 찾아 필드를 직접 덮어써서
    // ButtonHoverSound.PlaySafely 호출로 만들고, 맨 앞으로 옮긴다.
    private static void FixAudioCalls(Component owner, SerializedProperty callsProp, AudioSource source, ref int fixedCount)
    {
        if (callsProp == null) return;

        for (int j = callsProp.arraySize - 1; j >= 0; j--)
        {
            SerializedProperty call = callsProp.GetArrayElementAtIndex(j);
            SerializedProperty methodName = call.FindPropertyRelative("m_MethodName");
            SerializedProperty target = call.FindPropertyRelative("m_Target");
            if (methodName == null || target == null) continue;

            bool targetsThisAudioSource = target.objectReferenceValue == source;
            bool isRawAudioMethod = methodName.stringValue == "PlayOneShot" || methodName.stringValue == "Play";
            bool isBrokenSafeCall = methodName.stringValue == "PlaySafely" && targetsThisAudioSource; // Run B에서 남은 잘못된 상태
            bool isCleanSafeCall = methodName.stringValue == "PlaySafely" && target.objectReferenceValue is ButtonHoverSound safe && safe.GetComponent<AudioSource>() == source;

            bool needsRewrite = (isRawAudioMethod && targetsThisAudioSource) || isBrokenSafeCall;

            if (!needsRewrite && !isCleanSafeCall) continue;

            if (needsRewrite)
            {
                SerializedProperty args = call.FindPropertyRelative("m_Arguments");
                SerializedProperty objArg = args != null ? args.FindPropertyRelative("m_ObjectArgument") : null;
                AudioClip clip = objArg != null ? objArg.objectReferenceValue as AudioClip : null;

                Undo.RecordObject(owner, "버튼 오디오 안전화");
                ButtonHoverSound safePlayer = source.GetComponent<ButtonHoverSound>();
                if (safePlayer == null)
                    safePlayer = Undo.AddComponent<ButtonHoverSound>(source.gameObject);

                target.objectReferenceValue = safePlayer;
                call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = "ButtonHoverSound, Assembly-CSharp";
                methodName.stringValue = "PlaySafely";
                call.FindPropertyRelative("m_Mode").intValue = 2; // PersistentListenerMode.Object
                call.FindPropertyRelative("m_CallState").intValue = 2; // EditorAndRuntime

                if (args != null)
                {
                    if (objArg != null) objArg.objectReferenceValue = clip;
                    SerializedProperty objArgType = args.FindPropertyRelative("m_ObjectArgumentAssemblyTypeName");
                    if (objArgType != null) objArgType.stringValue = "UnityEngine.AudioClip, UnityEngine";
                }

                callsProp.serializedObject.ApplyModifiedProperties();

                fixedCount++;
                Debug.Log($"[FixButtonHoverAudioWarning] '{owner.gameObject.name}' ({owner.GetType().Name}) 의 오디오 호출을 안전 재생 방식으로 교체했습니다.");
            }

            // 소리 재생 리스너를 맨 앞으로 이동 (패널 전환보다 먼저 실행되도록)
            if (j > 0)
            {
                Undo.RecordObject(owner, "버튼 오디오 순서 조정");
                callsProp.MoveArrayElement(j, 0);
                callsProp.serializedObject.ApplyModifiedProperties();

                if (!needsRewrite)
                {
                    fixedCount++;
                    Debug.Log($"[FixButtonHoverAudioWarning] '{owner.gameObject.name}' ({owner.GetType().Name}) 의 오디오 리스너 순서를 맨 앞으로 옮겼습니다.");
                }
            }
        }
    }
}
