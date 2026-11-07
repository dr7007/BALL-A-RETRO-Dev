using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BallARetroReleaseBuild
{
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
        if (scenes.Length == 0 || scenes[0] != "Assets/MainScenes/Main_Scene_Lobby.unity" || scenes.Any(s => !File.Exists(s)))
            throw new BuildFailedException("Enable the release scenes with Main_Scene_Lobby first, and verify all scene paths.");
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
            report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = file, target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
                extraScriptingDefines = new[] { steam ? "BALLARETRO_STEAM" : "BALLARETRO_STOVE" }
            });
        }
        finally
        {
            foreach (var plugin in steamPlugins)
                if (plugin != null) plugin.SetIncludeInBuildDelegate(null);
        }
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException("Build failed. See the Unity Console and build report.");
        if (!steam && Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories).Any(p =>
            Path.GetFileName(p).StartsWith("steam_api", StringComparison.OrdinalIgnoreCase)
            || Path.GetFileName(p).StartsWith("libsteam_api", StringComparison.OrdinalIgnoreCase)))
            throw new BuildFailedException("STOVE output contains Steam runtime files. Do not submit this build.");
        Debug.Log($"[Release] {(steam ? "Steam" : "STOVE")} Windows build succeeded: {file}; {report.summary.totalSize} bytes.");
    }
}
