using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public static class BallARetroReleaseBuild
{
    [MenuItem("BALL-A-RETRO/Build/STOVE Review (Windows)")]
    public static void BuildStove() => Build(false);

    [MenuItem("BALL-A-RETRO/Build/Steam (Windows)")]
    public static void BuildSteam() => Build(true);

    private static void Build(bool steam)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new BuildFailedException("Exit Play Mode before building.");
        if (steam && !BallARetroSteamSettings.TryGetAppId(out _))
            throw new BuildFailedException("Set this game's real App ID in Assets/Resources/Release/SteamSettings.asset. Test App ID 480 is not accepted.");
        var file = EditorUtility.SaveFilePanel(steam ? "Steam build" : "STOVE review build", "", "BALL-A-RETRO.exe", "exe");
        if (string.IsNullOrEmpty(file)) return;
        // A local development appid file silently overrides Steam's launch context.
        if (steam && File.Exists(Path.Combine(Path.GetDirectoryName(file), "steam_appid.txt")))
            throw new BuildFailedException("Choose a clean Steam output folder without steam_appid.txt.");
        var defines = PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Standalone)
            .Split(';');
        if (steam && defines.Contains("BALLARETRO_STOVE"))
            throw new BuildFailedException("Remove the global BALLARETRO_STOVE define before a Steam build. These menu commands choose the platform per build.");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = file, target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
            extraScriptingDefines = new[] { steam ? "BALLARETRO_STEAM" : "BALLARETRO_STOVE" }
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException("Build failed. See the Unity Console and build report.");
    }
}
