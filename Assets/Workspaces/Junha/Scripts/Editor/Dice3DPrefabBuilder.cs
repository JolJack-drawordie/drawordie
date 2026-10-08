using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 3D 주사위 프리팹(Dice3D)과 머티리얼 / RenderTexture / 그림자 텍스처 에셋을 만들고 전투 씬에 배치하는 1회성 에디터 툴.
// - 면: 기본 Quad 6장 + 그 뒤의 몸통 Quad 6장. 면 그림은 DiceManager.diceSprites (맞은편 합이 7: 1-6, 2-5, 3-4)
// - 프리팹은 화면 밖(StagePosition)에 두고 전용 카메라가 RenderTexture로 찍어, 주사위 UI 자리의 RawImage(Dice3DView)에 보여준다
// - 다시 실행하면 에셋을 새로 만들고 씬의 기존 Dice3D / Dice3DView를 교체한다
// 사용법: Tools > Dice > 3D 주사위 프리팹 만들기 (전투 씬) 실행. 전투 씬을 열어 배치하고 저장한다.
public static class Dice3DPrefabBuilder
{
    private const string BattleScenePath = "Assets/Workspaces/Junha/Scene/junhaTest.unity";
    private const string Folder = "Assets/Workspaces/Junha/Dice3D";
    private const string PrefabPath = Folder + "/Dice3D.prefab";
    private const string TexturePath = Folder + "/Dice3DTexture.renderTexture";
    private const string ShadowTexturePath = Folder + "/DiceShadow.png";

    // 다른 오브젝트와 겹치지 않도록 무대를 아주 먼 곳에 둠
    private static readonly Vector3 StagePosition = new Vector3(0f, -1000f, 0f);

    // 화면에 띄우는 영역 (1920x1080 기준 UI 크기) / 렌더 해상도 배율.
    // 주사위가 화면 밖에서 날아 들어오도록 화면 전체보다 조금 크게 (주사위 이미지 중심이 화면 가운데에서 살짝 벗어나 있어도 덮도록)
    private static readonly Vector2 ViewSize = new Vector2(2160f, 1215f);
    private const float RenderScale = 1f;

    // 카메라: 주사위가 멈추는 곳(높이 0.5)을 비스듬히 위에서 내려다봄.
    // 거리는 화면 영역 높이(1215)에 주사위 한 변이 약 124px로 보이도록 맞춤 (예전 900x600 영역일 때와 같은 크기)
    private static readonly Vector3 CameraOffset = new Vector3(0f, 13.4f, -12.9f);
    private const float CameraFov = 30f;
    private const float RestHeight = 0.5f;

    private static readonly Color CoreColor = new Color(0.27f, 0.22f, 0.18f);
    private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.45f);

    // 눈 1~6이 붙는 면 방향
    private static readonly Vector3[] FaceNormals =
    {
        Vector3.up,      // 1
        Vector3.back,    // 2 (카메라 쪽)
        Vector3.right,   // 3
        Vector3.left,    // 4
        Vector3.forward, // 5
        Vector3.down,    // 6
    };

    [MenuItem("Tools/Dice/3D 주사위 프리팹 만들기 (전투 씬)")]
    private static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single);

        DiceManager diceManager = Object.FindFirstObjectByType<DiceManager>();
        if (diceManager == null || diceManager.diceImage == null ||
            diceManager.diceSprites == null || diceManager.diceSprites.Length < 6)
        {
            Debug.LogError("[Dice3DPrefabBuilder] 전투 씬의 DiceManager에 diceImage / diceSprites(6장)가 연결되어 있어야 합니다.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Workspaces/Junha", "Dice3D");

        RenderTexture renderTexture = CreateRenderTexture();
        GameObject prefab = CreatePrefab(diceManager.diceSprites, renderTexture);

        // 씬에 배치 (기존 것은 교체)
        Dice3DRoller old = Object.FindFirstObjectByType<Dice3DRoller>(FindObjectsInactive.Include);
        if (old != null) Object.DestroyImmediate(old.gameObject);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.transform.position = StagePosition;

        RawImage view = CreateView(diceManager.diceImage, renderTexture);

        Dice3DRoller roller = instance.GetComponent<Dice3DRoller>();
        SerializedObject rollerSo = new SerializedObject(roller);
        rollerSo.FindProperty("view").objectReferenceValue = view;
        rollerSo.ApplyModifiedProperties();

        SerializedObject diceSo = new SerializedObject(diceManager);
        diceSo.FindProperty("dice3D").objectReferenceValue = roller;
        diceSo.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeObject = prefab;

        Debug.Log($"<color=green>[Dice3DPrefabBuilder] 3D 주사위 프리팹 생성 및 전투 씬 배치 완료: {PrefabPath}</color>");
    }

    private static RenderTexture CreateRenderTexture()
    {
        AssetDatabase.DeleteAsset(TexturePath);

        RenderTexture texture = new RenderTexture(
            Mathf.RoundToInt(ViewSize.x * RenderScale), Mathf.RoundToInt(ViewSize.y * RenderScale), 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 4,
        };
        AssetDatabase.CreateAsset(texture, TexturePath);
        return texture;
    }

    private static GameObject CreatePrefab(Sprite[] sprites, RenderTexture renderTexture)
    {
        // UI 기본 셰이더: 조명 없이 텍스처 * 색으로 그려지고 2D Renderer에서도 동작
        Shader shader = Canvas.GetDefaultCanvasMaterial().shader;
        Mesh quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

        GameObject root = new GameObject("Dice3D");

        // 카메라 (배경 투명, 평소엔 꺼둠)
        GameObject cameraObj = new GameObject("Camera");
        cameraObj.transform.SetParent(root.transform, false);
        cameraObj.transform.localPosition = CameraOffset;
        cameraObj.transform.LookAt(Vector3.up * RestHeight);

        Camera camera = cameraObj.AddComponent<Camera>();
        camera.fieldOfView = CameraFov;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 50f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.targetTexture = renderTexture;
        camera.depth = -100;
        camera.enabled = false;

        // 주사위
        Transform die = new GameObject("Die").transform;
        die.SetParent(root.transform, false);
        die.localPosition = Vector3.up * RestHeight;

        // 텍스처 없음 = 셰이더 기본값(흰색) → 색만 칠해짐
        Material coreMaterial = SaveMaterial("DiceCore", shader, null, CoreColor);

        Renderer[] faceRenderers = new Renderer[12];
        Transform[] valueFaces = new Transform[6];
        for (int i = 0; i < 6; i++)
        {
            // 몸통 (면 그림 모서리의 투명한 부분을 채움) → 면 그림 순서로 그려지도록 sortingOrder 1, 2
            faceRenderers[i] = CreateQuad("Core" + (i + 1), die, quad, FaceNormals[i], 0.495f, coreMaterial, 1);

            Material faceMaterial = SaveMaterial("DiceFace" + (i + 1), shader, sprites[i].texture, Color.white);
            faceRenderers[i + 6] = CreateQuad("Face" + (i + 1), die, quad, FaceNormals[i], 0.5f, faceMaterial, 2);
            valueFaces[i] = faceRenderers[i + 6].transform;

            if (sprites[i].textureRect.size != new Vector2(sprites[i].texture.width, sprites[i].texture.height))
                Debug.LogWarning($"[Dice3DPrefabBuilder] {sprites[i].name}: 스프라이트가 텍스처 일부만 쓰고 있어 면에 텍스처 전체가 보입니다.");
        }

        // 바닥 그림자
        Material shadowMaterial = SaveMaterial("DiceShadow", shader, CreateShadowTexture(), ShadowColor);
        Renderer shadow = CreateQuad("Shadow", root.transform, quad, Vector3.up, 0.01f, shadowMaterial, 0);

        Dice3DRoller roller = root.AddComponent<Dice3DRoller>();
        SerializedObject so = new SerializedObject(roller);
        so.FindProperty("stageCamera").objectReferenceValue = camera;
        so.FindProperty("die").objectReferenceValue = die;
        so.FindProperty("shadow").objectReferenceValue = shadow;
        SetArray(so.FindProperty("valueFaces"), valueFaces);
        SetArray(so.FindProperty("faceRenderers"), faceRenderers);
        so.FindProperty("restHeight").floatValue = RestHeight;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // normal 방향을 바깥으로 보는 Quad. 기본 Quad는 -Z 쪽에서 볼 때 그림이 바로 보이므로 -Z를 normal로 돌린다
    private static Renderer CreateQuad(string name, Transform parent, Mesh mesh, Vector3 normal, float offset,
        Material material, int sortingOrder)
    {
        Vector3 up = Mathf.Abs(normal.y) > 0.5f ? Vector3.forward : Vector3.up;

        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = normal * offset;
        obj.transform.localRotation = Quaternion.LookRotation(-normal, up);
        obj.AddComponent<MeshFilter>().sharedMesh = mesh;

        MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingOrder = sortingOrder;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return renderer;
    }

    private static Material SaveMaterial(string name, Shader shader, Texture texture, Color color)
    {
        string path = $"{Folder}/{name}.mat";
        AssetDatabase.DeleteAsset(path);

        Material material = new Material(shader) { mainTexture = texture, color = color };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    // 가장자리가 흐린 원 (그림자)
    private static Texture2D CreateShadowTexture()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float alpha = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
            }
        }

        File.WriteAllBytes(ShadowTexturePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(ShadowTexturePath);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(ShadowTexturePath);
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(ShadowTexturePath);
    }

    // 주사위 UI 자리(diceImage 중심)에 렌더 결과를 보여줄 RawImage. 이미 있으면 다시 맞춤
    private static RawImage CreateView(Image diceImage, RenderTexture renderTexture)
    {
        Transform parent = diceImage.transform.parent;
        Transform existing = parent.Find("Dice3DView");

        GameObject viewObj = existing != null ? existing.gameObject : new GameObject("Dice3DView", typeof(RectTransform));
        viewObj.layer = diceImage.gameObject.layer;
        viewObj.transform.SetParent(parent, false);
        viewObj.transform.SetSiblingIndex(diceImage.transform.GetSiblingIndex() + 1);

        RawImage view = viewObj.GetComponent<RawImage>();
        if (view == null) view = viewObj.AddComponent<RawImage>();
        view.texture = renderTexture;
        view.raycastTarget = false;

        RectTransform source = diceImage.rectTransform;
        RectTransform rect = view.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = ViewSize;
        rect.position = source.TransformPoint(source.rect.center);

        viewObj.SetActive(false);
        return view;
    }

    private static void SetArray<T>(SerializedProperty property, T[] values) where T : Object
    {
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
