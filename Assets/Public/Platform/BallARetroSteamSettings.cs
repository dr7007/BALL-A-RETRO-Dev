using UnityEngine;

[CreateAssetMenu(menuName = "BALL-A-RETRO/Steam Settings")]
public class BallARetroSteamSettings : ScriptableObject
{
    public const string ResourcePath = "Release/SteamSettings";
    [Tooltip("The App ID assigned to this game. 0 and the Spacewar test ID 480 are not release IDs.")]
    [SerializeField] private uint steamAppId;
    [Tooltip("The separate demo App ID, linked to this full game in Steamworks.")]
    [SerializeField] private uint demoAppId;
    public uint FullAppId => steamAppId;
    public uint DemoAppId => demoAppId;
    public uint AppId => BallARetroPlatform.IsDemoBuild ? demoAppId : steamAppId;
    public bool HasReleaseAppId => IsValidFor(BallARetroPlatform.IsDemoBuild);

    public bool IsValidFor(bool demo) => IsReleaseId(steamAppId) &&
        (!demo || (IsReleaseId(demoAppId) && demoAppId != steamAppId));

    private static bool IsReleaseId(uint id) => id != 0 && id != 480;

    public static string FullGameStoreUrl => TryGetFullGameAppId(out uint id)
        ? "https://store.steampowered.com/app/" + id + "/" : null;

    public static bool TryGetFullGameAppId(out uint appId) => TryGetAppId(false, out appId);

    public static bool TryGetAppId(out uint appId) => TryGetAppId(BallARetroPlatform.IsDemoBuild, out appId);

    // Explicit edition selection is required by the editor: extraScriptingDefines
    // affect the player compilation, not this running editor assembly.
    public static bool TryGetAppId(bool demo, out uint appId)
    {
        var settings = Resources.Load<BallARetroSteamSettings>(ResourcePath);
        appId = settings != null ? (demo ? settings.DemoAppId : settings.FullAppId) : 0;
        return settings != null && settings.IsValidFor(demo);
    }
}
