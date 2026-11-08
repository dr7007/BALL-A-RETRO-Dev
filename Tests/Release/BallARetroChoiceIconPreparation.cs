#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

// Run only in the isolated release project after all nine source PNGs arrive.
// Unity writes the native SpriteAtlasV2 asset and importer metadata; copy those
// two generated source files back for review before the final audit/build.
public static class BallARetroChoiceIconPreparation
{
    public const string Folder = "Assets/Public/Roguelike/GeneratedIcons/";
    public const string AtlasPath = Folder + "ChoiceIcons.spriteatlasv2";
    public static readonly string[] Names = {
        "Choice_0_BonusChipScore", "Choice_1_ChipRebound", "Choice_2_BonusMultiplier",
        "Choice_3_GoldenChip", "Choice_4_TotalScore", "Choice_5_SecretWeapon",
        "Choice_6_FlipperRecharge", "Choice_7_UpperFloorPass", "Choice_8_ExtraBalls"
    };

    public static void Prepare()
    {
        if (EditorSettings.spritePackerMode != SpritePackerMode.SpriteAtlasV2)
            throw new InvalidOperationException("Expected the reviewed SpriteAtlasV2 project setting.");
        foreach (var name in Names)
        {
            string path = Folder + name + ".png";
            if (!File.Exists(path)) throw new FileNotFoundException("Generated choice icon is missing", path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }
        var sprites = Names.Select(name => AssetDatabase.LoadAssetAtPath<Sprite>(Folder + name + ".png")).ToArray();
        if (sprites.Any(sprite => sprite == null)) throw new InvalidOperationException("All nine PNGs must import as Single sprites.");
        var asset = new SpriteAtlasAsset();
        asset.Add(sprites.Cast<UnityEngine.Object>().ToArray());
        asset.SetIsVariant(false);
        SpriteAtlasAsset.Save(asset, AtlasPath);
        AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(AtlasPath) as SpriteAtlasImporter;
        if (importer == null) throw new InvalidOperationException("Unity did not create a SpriteAtlasV2 importer.");
        importer.includeInBuild = true;
        var packing = importer.packingSettings;
        packing.enableRotation = false;
        packing.enableTightPacking = false;
        packing.padding = 4;
        packing.enableAlphaDilation = true;
        importer.packingSettings = packing;
        var texture = importer.textureSettings;
        texture.generateMipMaps = false;
        texture.readable = false;
        texture.filterMode = FilterMode.Bilinear;
        texture.anisoLevel = 1;
        importer.textureSettings = texture;
        var defaults = importer.GetPlatformSettings("DefaultTexturePlatform");
        defaults.name = "DefaultTexturePlatform";
        defaults.maxTextureSize = 2048;
        defaults.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SetPlatformSettings(defaults);
        var windows = importer.GetPlatformSettings("Standalone");
        windows.name = "Standalone";
        windows.overridden = true;
        windows.maxTextureSize = 2048;
        windows.format = TextureImporterFormat.BC7;
        windows.compressionQuality = 100;
        importer.SetPlatformSettings(windows);
        importer.SaveAndReimport();
        var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AtlasPath);
        if (atlas == null) throw new InvalidOperationException("Unity did not import the generated SpriteAtlasV2 asset.");
        SpriteAtlasUtility.PackAtlases(new[] { atlas }, BuildTarget.StandaloneWindows64, false);
        if (atlas.spriteCount != 9 || sprites.Any(sprite => !atlas.CanBindTo(sprite)))
            throw new InvalidOperationException("The generated atlas must bind exactly the nine requested icons.");
        Debug.Log("[ChoiceIcons] Native atlas prepared: " + AtlasPath + "; nine explicit sprites; max 2048; Windows BC7.");
    }
}
#endif
