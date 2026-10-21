using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class RelicManager : MonoBehaviour
{
    public static RelicManager Instance { get; private set; }

    // 서버에서 불러온 전체 유물 목록
    public List<RelicData> AllRelics { get; private set; }
        = new List<RelicData>();

    // 플레이어가 획득한 유물 목록 (획득 순서 유지)
    // RelicManager는 전투 씬에 있으므로, 로비에서 불러오기 할 때도 복원할 수 있도록 static으로 보관
    private static readonly List<string> ownedRelicKeys =
        new List<string>();

    // 세이브용: 보유 유물 key 목록 (획득 순서)
    public static List<string> GetOwnedRelicKeys()
    {
        return new List<string>(ownedRelicKeys);
    }

    // 불러오기: 저장된 보유 목록으로 교체
    // 최대 HP 같은 획득 효과는 이미 세이브의 maxHp에 반영돼 있으므로 다시 적용하지 않음
    public static void RestoreOwnedRelics(List<string> relicKeys)
    {
        ownedRelicKeys.Clear();
        if (relicKeys == null) return;

        foreach (string key in relicKeys)
        {
            if (!string.IsNullOrEmpty(key) && !ownedRelicKeys.Contains(key))
                ownedRelicKeys.Add(key);
        }
    }

    // 새 게임: 이전 판의 유물 제거
    public static void ClearOwnedRelics()
    {
        ownedRelicKeys.Clear();
    }

    private const string ApiBaseUrl = "http://localhost:8080";

    private void Awake()
    {
        Debug.Log("[RelicManager] Awake 실행");

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log("[RelicManager] Instance 설정 완료 및 씬 이동 유지");
        }
        else if (Instance != this)
        {
            Debug.LogWarning(
                "[RelicManager] 중복 인스턴스 발견. 해당 오브젝트 제거"
            );

            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Debug.Log("[RelicManager] Start 실행");
        LoadRelics();
    }

    // 서버에서 유물 목록 가져오기
    public void LoadRelics()
    {
        Debug.Log("[RelicManager] LoadRelics 호출");
        StartCoroutine(LoadRelicsCoroutine());
    }

    private IEnumerator LoadRelicsCoroutine()
    {
        string url = ApiBaseUrl + "/api/game/load-relics";

        Debug.Log($"[RelicManager] 서버 요청 시작: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            Debug.Log(
                $"[RelicManager] 요청 결과: {request.result}, " +
                $"HTTP 상태 코드: {request.responseCode}"
            );

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    "[RelicManager] 유물 로드 실패\n" +
                    $"HTTP 상태 코드: {request.responseCode}\n" +
                    $"오류: {request.error}\n" +
                    $"응답: {request.downloadHandler.text}"
                );

                yield break;
            }

            string json = request.downloadHandler.text;
            Debug.Log($"[RelicManager] 서버 응답 JSON:\n{json}");

            RelicListResponse response =
                JsonUtility.FromJson<RelicListResponse>(json);

            if (response == null || response.relics == null)
            {
                Debug.LogError(
                    "[RelicManager] 유물 데이터 파싱에 실패했습니다."
                );
                yield break;
            }

            AllRelics = response.relics;

            Debug.Log(
                $"[RelicManager] 유물 데이터 로드 완료: " +
                $"{AllRelics.Count}개"
            );

            foreach (RelicData relic in AllRelics)
            {
                if (relic == null)
                {
                    Debug.LogWarning(
                        "[RelicManager] null 유물 데이터 발견"
                    );
                    continue;
                }

                if (relic.effect != null)
                {
                    Debug.Log(
                        $"[RelicManager] 유물: {relic.name} / " +
                        $"효과: {relic.effect.type} / " +
                        $"값: {relic.effect.value}"
                    );
                }
                else
                {
                    Debug.LogWarning(
                        $"[RelicManager] {relic.name}의 효과 정보가 없습니다."
                    );
                }
            }
        }
    }

    // 유물 획득 처리
    public bool AcquireRelic(string relicKey)
    {
        if (AllRelics == null || AllRelics.Count == 0)
        {
            Debug.LogWarning(
                "[RelicManager] 유물 데이터가 아직 없습니다."
            );
            return false;
        }

        RelicData relic = AllRelics.Find(
            r => r != null && r.key == relicKey
        );

        if (relic == null)
        {
            Debug.LogWarning(
                $"[RelicManager] 유물을 찾을 수 없습니다: {relicKey}"
            );
            return false;
        }

        // 중복 획득 방지
        if (!relic.stackable && ownedRelicKeys.Contains(relic.key))
        {
            Debug.LogWarning(
                $"[RelicManager] 이미 획득한 유물입니다: {relic.name}"
            );
            return false;
        }

        if (relic.effect == null)
        {
            Debug.LogWarning(
                $"[RelicManager] 효과 정보가 없습니다: {relic.name}"
            );
            return false;
        }

        // 유물 효과 적용
        switch (relic.effect.type)
        {
            case "MAX_HP":
                if (StatManager.Instance == null)
                {
                    Debug.LogError(
                        "[RelicManager] StatManager를 찾을 수 없습니다."
                    );
                    return false;
                }

                StatManager.Instance.IncreasePlayerMaxHp(
                    relic.effect.value
                );
                break;

            case "ADD_ENERGY":
                // 에너지 효과는 주사위 조건 충족 시 적용
                // 획득 시에는 보유 목록에만 등록
                break;

            default:
                Debug.LogWarning(
                    $"[RelicManager] 아직 구현되지 않은 효과입니다: " +
                    $"{relic.effect.type}"
                );
                return false;
        }

        if (!ownedRelicKeys.Contains(relic.key))
            ownedRelicKeys.Add(relic.key);

        Debug.Log(
            $"[RelicManager] 유물 획득 완료: {relic.name}"
        );

        return true;
    }

    // 유물 보유 여부 확인
    public bool HasRelic(string relicKey)
    {
        return ownedRelicKeys.Contains(relicKey);
    }

    // 획득 가능한 유물 중 랜덤으로 하나 선택
    public RelicData GetRandomAvailableRelic(System.Random rng)
    {
        if (AllRelics == null || AllRelics.Count == 0)
        {
            Debug.LogWarning(
                "[RelicManager] 유물 목록이 비어 있습니다."
            );
            return null;
        }

        if (rng == null)
        {
            Debug.LogError(
                "[RelicManager] 랜덤 생성기가 없습니다."
            );
            return null;
        }

        List<RelicData> candidates = AllRelics.FindAll(relic =>
            relic != null &&
            relic.effect != null &&
            (relic.stackable ||
             !ownedRelicKeys.Contains(relic.key))
        );

        if (candidates.Count == 0)
        {
            Debug.LogWarning(
                "[RelicManager] 획득 가능한 유물이 없습니다."
            );
            return null;
        }

        int index = rng.Next(0, candidates.Count);
        RelicData selectedRelic = candidates[index];

        Debug.Log(
            $"[RelicManager] 랜덤 유물 선택: {selectedRelic.name}"
        );

        return selectedRelic;
    }

    // 주사위 결과에 따른 유물 효과 처리
    public void OnDiceRolled(int diceValue)
    {
        bool hasLuckyDice = HasRelic("lucky_dice");

        Debug.Log(
            $"[RelicManager] OnDiceRolled 호출: {diceValue}, " +
            $"Lucky Dice 보유 여부: {hasLuckyDice}"
        );

        if (!hasLuckyDice)
        {
            Debug.LogWarning(
                "[RelicManager] Lucky Dice를 보유하지 않아 발동하지 않습니다."
            );
            return;
        }

        RelicData relic = AllRelics.Find(
            r => r != null && r.key == "lucky_dice"
        );

        if (relic == null ||
            relic.condition == null ||
            relic.effect == null)
        {
            Debug.LogWarning(
                "[RelicManager] Lucky Dice 데이터 또는 조건이 없습니다."
            );
            return;
        }

        Debug.Log(
            $"[RelicManager] Lucky Dice 조건: " +
            $"{relic.condition.type} = {relic.condition.value}, " +
            $"주사위 결과 = {diceValue}"
        );

        if (relic.condition.type != "DICE_EQUALS" ||
            diceValue != relic.condition.value)
        {
            Debug.Log("[RelicManager] Lucky Dice 발동 조건 불충족");
            return;
        }

        if (relic.effect.type != "ADD_ENERGY")
        {
            Debug.LogWarning(
                $"[RelicManager] 잘못된 효과 타입: {relic.effect.type}"
            );
            return;
        }

        if (DiceManager.Instance == null)
        {
            Debug.LogError(
                "[RelicManager] DiceManager를 찾을 수 없습니다."
            );
            return;
        }

        DiceManager.Instance.AddEnergy(relic.effect.value);

        Debug.Log(
            $"[RelicManager] Lucky Dice 발동 성공! " +
            $"주사위 {diceValue}, 에너지 +{relic.effect.value}"
        );
    }
}
