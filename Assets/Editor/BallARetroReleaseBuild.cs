using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

public static class BallARetroReleaseBuild
{
    public const string DemoProductName = "Ball-A-Retro Demo";
    internal static string ActiveEvidenceDirectory { get; private set; }
    private static readonly string[] ReleaseScenes = {
        "Assets/MainScenes/Main_Scene_Lobby.unity",
        "Assets/MainScenes/Main_Scene_Tutorial.unity",
        "Assets/MainScenes/Main_Scene_Game.unity"
    };

    [MenuItem("BALL-A-RETRO/Build/STOVE Review (Windows)")]
    public static void BuildStove() => BuildInteractive(false);

    [MenuItem("BALL-A-RETRO/Build/Steam (Windows)")]
    public static void BuildSteam() => BuildInteractive(true);

    // Run only on an isolated project copy when the development editor is open.
    public static void BuildStoveBatch() => Build(false, ReadBatchOutput());
    public static void BuildSteamBatch() => Build(true, ReadBatchOutput());
    public static void BuildSteamDemoBatch() => Build(true, ReadBatchOutput(), true);

    private static string ReadBatchOutput()
    {
        var args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-ballARetroOutput");
        if (index < 0 || index + 1 >= args.Length || args[index + 1].StartsWith("-"))
            throw new BuildFailedException("Specify -ballARetroOutput with an absolute .exe path.");
        return args[index + 1];
    }

    private static void BuildInteractive(bool steam)
    {
        var file = EditorUtility.SaveFilePanel(steam ? "Steam build" : "STOVE review build", "", "BALL-A-RETRO.exe", "exe");
        if (!string.IsNullOrEmpty(file)) Build(steam, file);
    }

    private static void Build(bool steam, string file, bool demo = false)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new BuildFailedException("Exit Play Mode before building.");
        if (!PlayerSettings.GetStaticBatchingForPlatform(BuildTarget.StandaloneWindows64))
            throw new BuildFailedException("Enable Windows Static Batching before building; the fixed board pipes rely on it.");
        uint configuredAppId = 0;
        if (steam && !BallARetroSteamSettings.TryGetAppId(demo, out configuredAppId))
            throw new BuildFailedException("Configure real, separate full-game/demo App IDs in SteamSettings. IDs 0 and 480 are not accepted.");
        if (demo && (!steam || PlayerSettings.productName != DemoProductName))
            throw new BuildFailedException("Build Steam demos through Build-SteamDemoReview.ps1 in an isolated snapshot with product name 'Ball-A-Retro Demo'.");
        if (!demo && PlayerSettings.productName == DemoProductName)
            throw new BuildFailedException("A full-game build cannot use the demo save-data namespace.");
        if (!Path.IsPathRooted(file) || !file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            throw new BuildFailedException("Choose an absolute Windows .exe output path.");
        file = Path.GetFullPath(file);
        string output = Path.GetDirectoryName(file);
        string assets = Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar;
        if ((output + Path.DirectorySeparatorChar).StartsWith(assets, StringComparison.OrdinalIgnoreCase))
            throw new BuildFailedException("Do not build inside Assets.");
        // Reusing a Steam folder can leak its appid/native libraries into a STOVE upload.
        if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
            throw new BuildFailedException("Choose a new, empty output folder for each release build.");
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (!scenes.SequenceEqual(ReleaseScenes) || scenes.Any(s => !File.Exists(s)))
            throw new BuildFailedException("Enable only Lobby, Tutorial and Game, in that order, for a release build.");
        var defines = PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Standalone)
            .Split(';');
        if (steam && defines.Contains("BALLARETRO_STOVE"))
            throw new BuildFailedException("Remove the global BALLARETRO_STOVE define before a Steam build. These menu commands choose the platform per build.");
        if (defines.Contains("BALLARETRO_DEMO"))
            throw new BuildFailedException("Remove the global BALLARETRO_DEMO define. Demo selection is per build only.");
        // This is a per-build filter: no plugin .meta or project defines are changed.
        var steamPlugins = PluginImporter.GetAllImporters().Where(p =>
            p.isNativePlugin && (Path.GetFileName(p.assetPath).StartsWith("steam_api", StringComparison.OrdinalIgnoreCase)
                || Path.GetFileName(p.assetPath).StartsWith("libsteam_api", StringComparison.OrdinalIgnoreCase))).ToArray();
        // Match the Windows native library to the vendored Steamworks.NET wrapper.
        // A second, different DLL exists under Assets/Plugins; never ship both or
        // depend on Unity's overridable-plugin priority to choose the API version.
        const string windowsSteamPlugin = "Assets/com.rlabrecque.steamworks.net/Plugins/steam_api64.dll";
        if (steam && !steamPlugins.Any(p => p.assetPath == windowsSteamPlugin))
            throw new BuildFailedException("The Steamworks.NET Windows x64 native library is missing.");
        foreach (var plugin in steamPlugins)
        {
            bool includeNative = steam && plugin.assetPath == windowsSteamPlugin;
            plugin.SetIncludeInBuildDelegate(_ => includeNative);
        }
        BuildReport report;
        try
        {
            ActiveEvidenceDirectory = Path.Combine(Path.GetDirectoryName(output), "evidence");
            if (demo) VerifyDemoAtlas(ActiveEvidenceDirectory);
            report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = file, target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.DetailedBuildReport,
                extraScriptingDefines = demo ? new[] { "BALLARETRO_STEAM", "BALLARETRO_DEMO" }
                    : new[] { steam ? "BALLARETRO_STEAM" : "BALLARETRO_STOVE" }
            });
        }
        finally
        {
            ActiveEvidenceDirectory = null;
            foreach (var plugin in steamPlugins)
                if (plugin != null) plugin.SetIncludeInBuildDelegate(null);
        }
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException("Build failed. See the Unity Console and build report.");
        WriteAssetReport(report, file, scenes, steam, demo, configuredAppId);
        if (!steam && Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories).Any(p =>
            Path.GetFileName(p).StartsWith("steam_api", StringComparison.OrdinalIgnoreCase)
            || Path.GetFileName(p).StartsWith("libsteam_api", StringComparison.OrdinalIgnoreCase)))
            throw new BuildFailedException("STOVE output contains Steam runtime files. Do not submit this build.");
        Debug.Log($"[Release] {(demo ? "SteamDemo" : steam ? "Steam" : "STOVE")} Windows build succeeded: {file}; {report.summary.totalSize} bytes.");
    }

    [Serializable] private class AssetEntry { public string path; public long packedBytes; }
    [Serializable] private class DemoAtlasReport
    {
        public bool success;
        public string atlas;
        public int sprites;
        public int textures;
        public int width;
        public int height;
        public string format;
    }

    private static void VerifyDemoAtlas(string evidence)
    {
        const string atlasPath = "Assets/Resources/Demo/DemoChoiceIcons.spriteatlasv2";
        var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
        var importer = AssetImporter.GetAtPath(atlasPath) as SpriteAtlasImporter;
        var originals = new[] { "Choice_Capacitor", "Choice_LastBall" }.Select(name =>
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Demo/" + name + ".png")).ToArray();
        if (atlas == null || importer == null || !importer.includeInBuild || originals.Any(sprite => sprite == null))
            throw new BuildFailedException("Demo choice icons and included atlas must be present.");
        SpriteAtlasUtility.PackAtlases(new[] { atlas }, BuildTarget.StandaloneWindows64, false);
        // In edit mode GetSprites returns sprites whose texture pointers still
        // reference their source PNGs. Use the same packed-page accessor as the
        // Unity 6000.0 SpriteAtlas inspector, and fail closed if that API changes.
        var preview = typeof(SpriteAtlasExtensions).GetMethod("GetPreviewTextures", BindingFlags.Static | BindingFlags.NonPublic,
            null, new[] { typeof(SpriteAtlas) }, null);
        if (preview == null) throw new BuildFailedException("This Unity version cannot validate packed demo atlas pages.");
        var textures = preview.Invoke(null, new object[] { atlas }) as Texture2D[] ?? Array.Empty<Texture2D>();
        bool valid = atlas.spriteCount == 2 && textures.Length == 1 && textures[0] != null
            && originals.All(sprite => atlas.CanBindTo(sprite));
        Directory.CreateDirectory(evidence);
        File.WriteAllText(Path.Combine(evidence, "demo-atlas.json"), JsonUtility.ToJson(new DemoAtlasReport {
            success = valid, atlas = atlasPath, sprites = atlas.spriteCount, textures = textures.Length,
            width = textures.FirstOrDefault() != null ? textures[0].width : 0,
            height = textures.FirstOrDefault() != null ? textures[0].height : 0,
            format = textures.FirstOrDefault() != null ? textures[0].format.ToString() : "missing"
        }, true));
        if (!valid) throw new BuildFailedException("Both demo choice icons must bind to one atlas texture. Increase atlas max size if padding creates another page.");
    }

    [Serializable] private class AssetReport
    {
        public string platform;
        public uint appId;
        public string productName;
        public string[] scenes;
        public AssetEntry[] assets;
        public string[] resources;
    }

    private static void WriteAssetReport(BuildReport report, string file, string[] scenes, bool steam, bool demo, uint appId)
    {
        var entries = report.packedAssets.SelectMany(p => p.contents)
            .GroupBy(p => p.sourceAssetPath ?? "")
            .Select(g => new AssetEntry { path = g.Key, packedBytes = g.Sum(p => (long)p.packedSize) })
            .OrderByDescending(p => p.packedBytes).ToArray();
        if (entries.Any(e => e.path.EndsWith("/PerformanceTestRunInfo.json") ||
            e.path.EndsWith("/PerformanceTestRunSettings.json")))
            throw new BuildFailedException("Performance test metadata must not be included in a release.");
        // Keep provenance alongside, never inside, the distributable player folder.
        string evidence = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(file)), "evidence");
        Directory.CreateDirectory(evidence);
        File.WriteAllText(Path.Combine(evidence, "packed-assets.json"), JsonUtility.ToJson(new AssetReport {
            platform = demo ? "SteamDemo" : steam ? "Steam" : "STOVE", appId = appId, productName = PlayerSettings.productName,
            scenes = scenes, assets = entries,
            resources = entries.Where(e => e.path.Contains("/Resources/")).Select(e => e.path).ToArray()
        }, true));
    }
}

// Performance Testing 3.0.3 (a transitive Collections dependency) writes these
// Resources in its order-0 callback even for ordinary non-test player builds.
// Move only that generated metadata out after preprocessors have run. Keep the
// originals in local evidence and leave unrelated resources and test builds alone.
public sealed class BallARetroReleaseTestMetadataFilter : IPreprocessBuildWithReport
{
    public int callbackOrder => int.MaxValue;
    public void OnPreprocessBuild(BuildReport report)
    {
        if (BallARetroReleaseBuild.ActiveEvidenceDirectory == null ||
            (report.summary.options & BuildOptions.IncludeTestAssemblies) != 0) return;
        string excluded = Path.Combine(BallARetroReleaseBuild.ActiveEvidenceDirectory, "excluded-generated");
        Directory.CreateDirectory(excluded);
        bool moved = false;
        foreach (string name in new[] { "PerformanceTestRunInfo.json", "PerformanceTestRunSettings.json" })
        foreach (string suffix in new[] { "", ".meta" })
        {
            string source = Path.Combine(Application.dataPath, "Resources", name + suffix);
            if (!File.Exists(source)) continue;
            File.Move(source, Path.Combine(excluded, name + suffix));
            moved = true;
        }
        if (moved) AssetDatabase.Refresh();
    }
}
