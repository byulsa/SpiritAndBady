#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

internal static class NotoSansKrSetup
{
    private const string SourceFontPath = "Assets/Fonts/NotoSansCJKkr-Regular.otf";
    private const string FontAssetPath =
        "Assets/TextMesh Pro/Resources/Fonts & Materials/NotoSansCJKkr-Regular SDF.asset";

    [MenuItem("Tools/Fonts/Setup Noto Sans KR")]
    public static void Setup()
    {
        AssetDatabase.ImportAsset(SourceFontPath, ImportAssetOptions.ForceSynchronousImport);

        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
            throw new System.InvalidOperationException($"Font could not be imported: {SourceFontPath}");

        var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (fontAsset == null)
        {
            fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                true);

            if (fontAsset == null)
                throw new System.InvalidOperationException("TextMesh Pro font asset creation failed.");

            fontAsset.name = "NotoSansCJKkr-Regular SDF";
            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        var fallbacks = TMP_Settings.fallbackFontAssets ?? new List<TMP_FontAsset>();
        if (!fallbacks.Contains(fontAsset))
        {
            fallbacks.Add(fontAsset);
            TMP_Settings.fallbackFontAssets = fallbacks;
            EditorUtility.SetDirty(TMP_Settings.instance);
        }

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Noto Sans KR is ready as a dynamic TMP fallback: {FontAssetPath}");
    }
}
#endif
