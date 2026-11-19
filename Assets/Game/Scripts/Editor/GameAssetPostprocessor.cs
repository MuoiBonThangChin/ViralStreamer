using System.IO;
using UnityEditor;
using UnityEngine;

namespace StreamStar.EditorTools
{
    /// <summary>Applies import settings to everything under Assets/Game/Resources so art drops in with no manual setup.</summary>
    public class GameAssetPostprocessor : AssetPostprocessor
    {
        const string ArtRoot = "Assets/Game/Resources/Art/";
        const string AudioRoot = "Assets/Game/Resources/Audio/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot)) return;
            var importer = (TextureImporter)assetImporter;
            string name = Path.GetFileNameWithoutExtension(assetPath);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.textureCompression = name.StartsWith("ui_") ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);

            // Layer Lab GUI Pro art: border order is left, bottom, right, top
            if (name == "ui_panel") importer.spriteBorder = new Vector4(36, 36, 36, 36);      // 72x72
            else if (name == "ui_button") importer.spriteBorder = new Vector4(30, 45, 30, 44); // 61x90, bottom edge has the 3D lip
            else if (name == "ui_pill") importer.spriteBorder = new Vector4(47, 47, 47, 47);   // 96x96
            else if (assetPath.StartsWith(ArtRoot + "LayerLab/"))
            {
                // untouched Layer Lab sprites keep their own 9-slice borders
                switch (name)
                {
                    case "gauge_frame": importer.spriteBorder = new Vector4(19, 22, 19, 22); break;
                    case "gauge_inner": importer.spriteBorder = new Vector4(14, 17, 14, 17); break;
                    case "gauge_fill": importer.spriteBorder = new Vector4(11, 12, 11, 12); break;
                    case "tag": importer.spriteBorder = new Vector4(85, 0, 86, 0); break;
                    case "tag_outline": importer.spriteBorder = new Vector4(89, 0, 90, 0); break;
                }
            }
        }

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(AudioRoot)) return;
            var importer = (AudioImporter)assetImporter;
            bool music = Path.GetFileNameWithoutExtension(assetPath).StartsWith("bgm_");
            importer.forceToMono = true;
            var settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
            settings.quality = 0.6f;
            importer.defaultSampleSettings = settings;
        }
    }
}
