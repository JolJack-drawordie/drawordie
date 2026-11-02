using Spine;
using Spine.Unity;
using UnityEngine;

// 적이 돌진해서 멈출 "플레이어 몸 앞(오른쪽 끝)" 위치를 구한다.
// 플레이어(Death Knight, Spine)는 큰 칼과 베기 이펙트 슬롯이 몸 밖으로 크게 뻗어 있어서,
// MeshRenderer 전체 범위를 쓰면 적이 칼끝 앞에서 멈춰 공격이 닿지 않는다 → 칼 / 이펙트 슬롯을 빼고 몸통만 잰다.
public static class PlayerBodyBounds
{
    // 슬롯 이름이나 장착된 이미지 이름에 이 단어가 들어가면 몸 범위에서 제외
    private static readonly string[] IgnoreKeywords = { "sword", "weapon", "vfx", "slash" };

    private static float[] vertexBuffer = new float[8]; // 슬롯 꼭짓점 계산용 (필요하면 늘려서 재사용)

    // 플레이어 몸 앞(오른쪽 끝)의 월드 X. 플레이어가 없으면 false
    public static bool TryGetFrontX(out float frontX)
    {
        frontX = 0f;

        PlayerUnit player = GameManager.Instance != null ? GameManager.Instance.Player : null;
        if (player == null) return false;

        SkeletonRenderer spine = player.GetComponentInChildren<SkeletonRenderer>();
        if (spine != null && spine.skeleton != null && TryGetSpineBodyMaxX(spine, out frontX)) return true;

        // Spine이 아니거나 몸 조각을 못 찾으면 렌더러 전체 범위
        MeshRenderer body = player.GetComponentInChildren<MeshRenderer>();
        frontX = body != null ? body.bounds.max.x : player.transform.position.x;
        return true;
    }

    // 지금 자세에서 보이는 몸통 슬롯들의 꼭짓점 중 가장 오른쪽 X (투명한 슬롯과 칼 / 이펙트 슬롯은 제외)
    private static bool TryGetSpineBodyMaxX(SkeletonRenderer spine, out float maxX)
    {
        maxX = float.MinValue;
        bool has = false;

        Transform t = spine.transform;
        ExposedList<Slot> drawOrder = spine.skeleton.DrawOrder;

        for (int i = 0; i < drawOrder.Count; i++)
        {
            Slot slot = drawOrder.Items[i];
            Attachment attachment = slot.Attachment;
            if (attachment == null || !slot.Bone.Active || slot.A < 0.05f) continue;
            if (IsIgnored(slot.Data.Name) || IsIgnored(attachment.Name)) continue;

            int count = 0;
            if (attachment is RegionAttachment region)
            {
                count = 8;
                EnsureBuffer(count);
                region.ComputeWorldVertices(slot.Bone, vertexBuffer, 0);
            }
            else if (attachment is MeshAttachment mesh)
            {
                count = mesh.WorldVerticesLength;
                EnsureBuffer(count);
                mesh.ComputeWorldVertices(slot, 0, count, vertexBuffer, 0);
            }

            for (int v = 0; v + 1 < count; v += 2)
            {
                // 스켈레톤 좌표 → 월드 좌표 (플레이어 크기 / 뒤집힘 반영)
                float x = t.TransformPoint(vertexBuffer[v], vertexBuffer[v + 1], 0f).x;
                if (x > maxX) maxX = x;
                has = true;
            }
        }

        return has;
    }

    private static bool IsIgnored(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        foreach (string k in IgnoreKeywords)
        {
            if (name.IndexOf(k, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }
        return false;
    }

    private static void EnsureBuffer(int size)
    {
        if (vertexBuffer.Length < size) vertexBuffer = new float[size];
    }
}
