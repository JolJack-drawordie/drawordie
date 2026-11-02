using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

// 한글 폰트 아틀라스 미리 굽기 (Tools > Fonts > 한글 폰트 아틀라스 굽기)
// 빌드에서 동적 폰트가 실행 중에 글자를 그려 넣다가 일부 글자를 빠뜨리는 문제가 있어서,
// 게임에서 쓰는 글자(FontCharacters.txt)를 에디터에서 미리 아틀라스에 그려 두고 빌드할 때 지우지 않게 한다.
// 목록에 없는 글자(닉네임 등)는 지금처럼 실행 중에 추가된다.
// 글자를 새로 많이 쓰게 되면 FontCharacters.txt에 추가하고 다시 실행.
public static class FontAtlasBaker
{
    private const string CharactersPath = "Assets/Workspaces/Junha/Fonts/FontCharacters.txt";
    private const int AtlasSize = 2048; // 1024는 90pt 기준 80자 정도라 아틀라스가 너무 많이 생김

    private static readonly string[] FontAssetPaths =
    {
        "Assets/Workspaces/Junha/Resources/온글잎 콘콘체 SDF.asset",
        "Assets/Workspaces/Junha/Fonts/NanumGothic SDF.asset",
    };

    [MenuItem("Tools/Fonts/한글 폰트 아틀라스 굽기")]
    private static void Bake()
    {
        if (!File.Exists(CharactersPath))
        {
            Debug.LogError("[FontAtlasBaker] 글자 목록이 없습니다: " + CharactersPath);
            return;
        }
        string characters = File.ReadAllText(CharactersPath).Replace("\r", "").Replace("\n", "");

        foreach (string path in FontAssetPaths)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font == null)
            {
                Debug.LogError("[FontAtlasBaker] 폰트 에셋을 찾을 수 없습니다: " + path);
                continue;
            }

            // 빌드할 때 아틀라스를 비우지 않게 하고, 아틀라스 크기를 키운다
            SerializedObject so = new SerializedObject(font);
            so.FindProperty("m_ClearDynamicDataOnBuild").boolValue = false;
            so.FindProperty("m_AtlasWidth").intValue = AtlasSize;
            so.FindProperty("m_AtlasHeight").intValue = AtlasSize;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 기존 글자를 지우고 목록의 글자를 새로 그린다 (동적 모드는 그대로 → 목록에 없는 글자는 실행 중에 추가)
            font.ClearFontAssetData(true);
            font.TryAddCharacters(characters, out string missing);

            EditorUtility.SetDirty(font);
            foreach (Texture2D atlas in font.atlasTextures)
                if (atlas != null) EditorUtility.SetDirty(atlas);

            Debug.Log($"[FontAtlasBaker] {font.name}: 글자 {font.characterTable.Count}개, 아틀라스 {font.atlasTextureCount}장"
                      + (string.IsNullOrEmpty(missing) ? "" : $", 폰트에 없는 글자 {missing.Length}개: {missing}"));
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[FontAtlasBaker] 완료 — 다시 빌드하세요.");
    }
}
