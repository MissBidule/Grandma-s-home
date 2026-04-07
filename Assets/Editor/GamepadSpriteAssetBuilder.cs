using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

/*
 * Menu: Tools → Build Gamepad TMP Sprite Asset
 * Reads all PNGs from Assets/Resources/XboxIcons/, packs them into an atlas,
 * and creates a TMP Sprite Asset ready to use as the default sprite asset.
 */
public static class GamepadSpriteAssetBuilder
{
    private const string IconFolder  = "Assets/Resources/XboxIcons";
    private const string AtlasPath   = "Assets/Resources/GamepadIconsAtlas.png";
    private const string AssetPath   = "Assets/Resources/GamepadIconsAsset.asset";

    [MenuItem("Tools/Build Gamepad TMP Sprite Asset")]
    public static void Build()
    {
        // ── 1. Collect textures ──────────────────────────────────────────────
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { IconFolder });
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", $"No textures found in {IconFolder}", "OK");
            return;
        }

        var textures  = new List<Texture2D>();
        var names     = new List<string>();

        foreach (var guid in guids)
        {
            var path     = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            // Enable read/write so PackTextures can access pixels
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
            {
                textures.Add(tex);
                names.Add(Path.GetFileNameWithoutExtension(path));
            }
        }

        // ── 2. Pack into atlas ───────────────────────────────────────────────
        var atlas = new Texture2D(2048, 2048, TextureFormat.RGBA32, false);
        var rects = atlas.PackTextures(textures.ToArray(), padding: 2, maximumAtlasSize: 4096);

        // ── 3. Save atlas PNG ────────────────────────────────────────────────
        File.WriteAllBytes(AtlasPath, atlas.EncodeToPNG());
        AssetDatabase.ImportAsset(AtlasPath);

        var atlasImporter = (TextureImporter)AssetImporter.GetAtPath(AtlasPath);
        atlasImporter.textureType          = TextureImporterType.Default;
        atlasImporter.alphaIsTransparency  = true;
        atlasImporter.mipmapEnabled        = false;
        atlasImporter.isReadable           = false;
        atlasImporter.textureCompression   = TextureImporterCompression.Uncompressed;
        atlasImporter.SaveAndReimport();

        var savedAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);

        // ── 4. Build TMP Sprite Asset ────────────────────────────────────────
        // Delete old asset first
        if (File.Exists(AssetPath))
            AssetDatabase.DeleteAsset(AssetPath);

        var spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
        spriteAsset.spriteSheet = savedAtlas;

        var glyphTable = new List<TMP_SpriteGlyph>();
        var charTable  = new List<TMP_SpriteCharacter>();

        int atlasW = savedAtlas.width;
        int atlasH = savedAtlas.height;

        for (int i = 0; i < textures.Count; i++)
        {
            var r = rects[i];
            int x = Mathf.RoundToInt(r.x      * atlasW);
            int y = Mathf.RoundToInt(r.y      * atlasH);
            int w = Mathf.RoundToInt(r.width  * atlasW);
            int h = Mathf.RoundToInt(r.height * atlasH);

            var glyph = new TMP_SpriteGlyph
            {
                index     = (uint)i,
                glyphRect = new GlyphRect(x, y, w, h),
                metrics   = new GlyphMetrics(w, h, 0, h * 0.8f, w),
                scale     = 1f,
                atlasIndex = 0
            };
            glyphTable.Add(glyph);

            var character = new TMP_SpriteCharacter(0xFFFE, spriteAsset, glyph)
            {
                name  = names[i],
                scale = 1f
            };
            charTable.Add(character);
        }

        // Use reflection to set readonly backing fields
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;

        var glyphField = typeof(TMP_SpriteAsset).GetField("m_GlyphTable", flags);
        glyphField.SetValue(spriteAsset, glyphTable);

        var charField = typeof(TMP_SpriteAsset).GetField("m_SpriteCharacterTable", flags);
        charField.SetValue(spriteAsset, charTable);

        // Set version to "1.1.0" to prevent UpgradeSpriteAsset() from running
        // (it accesses legacy spriteInfoList which is null for new assets)
        var versionField = typeof(TMP_Asset).GetField("m_Version", flags);
        versionField.SetValue(spriteAsset, "1.1.0");

        // ── 5. Create material ───────────────────────────────────────────────
        var shader = Shader.Find("TextMeshPro/Sprite");
        var mat = new Material(shader) { mainTexture = savedAtlas };
        mat.name = "GamepadIcons Material";

        // ── 6. Save asset ────────────────────────────────────────────────────
        AssetDatabase.CreateAsset(spriteAsset, AssetPath);
        // Add material as sub-asset so the sprite asset has its own material
        AssetDatabase.AddObjectToAsset(mat, AssetPath);
        spriteAsset.material = mat;

        spriteAsset.UpdateLookupTables();
        EditorUtility.SetDirty(spriteAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[GamepadSpriteAssetBuilder] Created {AssetPath} with {textures.Count} sprites: {string.Join(", ", names)}");
    }
}
