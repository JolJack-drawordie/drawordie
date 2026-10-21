using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RelicIconUI : MonoBehaviour
{
    public static RelicIconUI Instance { get; private set; }

    [Header("유물 아이콘 UI")]
    [SerializeField] private Transform relicIconContainer;
    [SerializeField] private GameObject relicIconPrefab;

    [Header("유물별 아이콘 연결")]
    [SerializeField]
    private List<RelicIconEntry> relicIcons =
        new List<RelicIconEntry>();

    [Serializable]
    public class RelicIconEntry
    {
        public string relicKey;
        public Sprite icon;
    }

    private readonly HashSet<string> displayedRelicKeys =
        new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "[RelicIconUI] 중복 인스턴스를 제거합니다."
            );

            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    private IEnumerator Start()
    {
        Debug.Log("[RelicIconUI] 아이콘 초기화 시작");

        // RelicManager 생성 대기
        while (RelicManager.Instance == null)
        {
            yield return null;
        }

        // 서버 유물 데이터 로드 대기
        while (RelicManager.Instance.AllRelics == null ||
               RelicManager.Instance.AllRelics.Count == 0)
        {
            yield return null;
        }

        RefreshRelicIcons();

        Debug.Log("[RelicIconUI] 아이콘 초기화 완료");
    }

    // 현재 보유한 유물 아이콘을 다시 표시
    public void RefreshRelicIcons()
    {
        if (relicIconContainer == null)
        {
            Debug.LogError(
                "[RelicIconUI] RelicIconContainer가 연결되지 않았습니다."
            );
            return;
        }

        if (relicIconPrefab == null)
        {
            Debug.LogError(
                "[RelicIconUI] 유물 아이콘 프리팹이 연결되지 않았습니다."
            );
            return;
        }

        if (RelicManager.Instance == null)
        {
            Debug.LogWarning(
                "[RelicIconUI] RelicManager를 찾을 수 없습니다."
            );
            return;
        }

        if (RelicManager.Instance.AllRelics == null)
        {
            Debug.LogWarning(
                "[RelicIconUI] 유물 목록이 아직 준비되지 않았습니다."
            );
            return;
        }

        // 이전에 생성한 아이콘을 정리하고 다시 구성
        foreach (Transform child in relicIconContainer)
        {
            Destroy(child.gameObject);
        }

        displayedRelicKeys.Clear();

        foreach (RelicData relic in RelicManager.Instance.AllRelics)
        {
            if (relic == null || string.IsNullOrEmpty(relic.key))
            {
                continue;
            }

            if (!RelicManager.Instance.HasRelic(relic.key))
            {
                continue;
            }

            // 동일한 유물의 아이콘 중복 생성 방지
            if (!displayedRelicKeys.Add(relic.key))
            {
                continue;
            }

            Sprite iconSprite = FindRelicIcon(relic.key);

            if (iconSprite == null)
            {
                Debug.LogWarning(
                    $"[RelicIconUI] 아이콘 스프라이트가 없습니다: {relic.key}"
                );

                displayedRelicKeys.Remove(relic.key);
                continue;
            }

            GameObject iconObject = Instantiate(
                relicIconPrefab,
                relicIconContainer
            );

            Image iconImage = iconObject.GetComponent<Image>();

            if (iconImage == null)
            {
                iconImage = iconObject.GetComponentInChildren<Image>();
            }

            if (iconImage == null)
            {
                Debug.LogError(
                    "[RelicIconUI] 프리팹에서 Image 컴포넌트를 찾을 수 없습니다."
                );

                Destroy(iconObject);
                displayedRelicKeys.Remove(relic.key);
                continue;
            }

            iconImage.sprite = iconSprite;
            iconImage.enabled = true;

            iconObject.name = $"RelicIcon_{relic.key}";

            Debug.Log(
                $"[RelicIconUI] 유물 아이콘 표시: {relic.name}"
            );
        }
    }

    // 유물 키에 맞는 스프라이트 검색
    private Sprite FindRelicIcon(string relicKey)
    {
        foreach (RelicIconEntry entry in relicIcons)
        {
            if (entry != null &&
                entry.relicKey == relicKey)
            {
                return entry.icon;
            }
        }

        return null;
    }
}
