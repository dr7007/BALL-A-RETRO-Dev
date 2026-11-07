using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BallARetroReleaseBuild
{
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

    private static void Build(bool steam, string file)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new BuildFailedException("Exit Play Mode before building.");
        if (steam && !BallARetroSteamSettings.TryGetAppId(out _))
            throw new BuildFailedException("Set this game's real App ID in Assets/Resources/Release/SteamSettings.asset. Test App ID 480 is not accepted.");
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
        // This is a per-build filter: no plugin .meta or project defines are changed.
        var steamPlugins = PluginImporter.GetAllImporters().Where(p =>
            p.isNativePlugin && (Path.GetFileName(p.assetPath).StartsWith("steam_api", StringComparison.OrdinalIgnoreCase)
                || Path.GetFileName(p.assetPath).StartsWith("libsteam_api", StringComparison.OrdinalIgnoreCase))).ToArray();
        foreach (var plugin in steamPlugins) plugin.SetIncludeInBuildDelegate(_ => steam);
        BuildReport report;
        try
        {
            ActiveEvidenceDirectory = Path.Combine(Path.GetDirectoryName(output), "evidence");
            report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = file, target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.DetailedBuildReport,
                extraScriptingDefines = new[] { steam ? "BALLARETRO_STEAM" : "BALLARETRO_STOVE" }
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
        WriteAssetReport(report, file, scenes);
        if (!steam && Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories).Any(p =>
            Path.GetFileName(p).StartsWith("steam_api", StringComparison.OrdinalIgnoreCase)
            || Path.GetFileName(p).StartsWith("libsteam_api", StringComparison.OrdinalIgnoreCase)))
            throw new BuildFailedException("STOVE output contains Steam runtime files. Do not submit this build.");
        Debug.Log($"[Release] {(steam ? "Steam" : "STOVE")} Windows build succeeded: {file}; {report.summary.totalSize} bytes.");
    }

    [Serializable] private class AssetEntry { public string path; public long packedBytes; }
    [Serializable] private class AssetReport
    {
        public string[] scenes;
        public AssetEntry[] assets;
        public string[] resources;
    }

    private static void WriteAssetReport(BuildReport report, string file, string[] scenes)
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
