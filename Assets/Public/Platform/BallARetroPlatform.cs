/// <summary>STOVE is the default. Steam integration is an explicit build choice.</summary>
public static class BallARetroPlatform
{
    public static bool IsSteamBuild
    {
        get
        {
#if BALLARETRO_STEAM && !BALLARETRO_STOVE
            return true;
#else
            return false;
#endif
        }
    }
    public static bool UsesPersonalRecords => !IsSteamBuild;
}
