#if UNITY_EDITOR
using UnityEditor;

// Ensures all Xbox icon textures in Resources/XboxIcons are imported as Sprites.
public class XboxIconsImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/XboxIcons/")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
    }
}
#endif
