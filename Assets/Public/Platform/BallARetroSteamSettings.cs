using UnityEngine;

[CreateAssetMenu(menuName = "BALL-A-RETRO/Steam Settings")]
public class BallARetroSteamSettings : ScriptableObject
{
    public const string ResourcePath = "Release/SteamSettings";
    [Tooltip("The App ID assigned to this game. 0 and the Spacewar test ID 480 are not release IDs.")]
    [SerializeField] private uint steamAppId;
    public uint AppId => steamAppId;
    public bool HasReleaseAppId => steamAppId != 0 && steamAppId != 480;

    public static bool TryGetAppId(out uint appId)
    {
        var settings = Resources.Load<BallARetroSteamSettings>(ResourcePath);
        appId = settings != null ? settings.AppId : 0;
        return settings != null && settings.HasReleaseAppId;
    }
}
