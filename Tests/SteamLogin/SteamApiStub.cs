// Test double for the Steam client boundary. Never copied into the game project.
using System.Text;
namespace Steamworks
{
    public static class Fixture
    {
        // Synthetic test IDs, never release settings.
#if BALLARETRO_DEMO
        public const uint AppId = 654321;
#else
        public const uint AppId = 123456;
#endif
        public static bool LoggedOn, InitResult;
        public static uint RuntimeAppId, RestartAppId;
        public static ulong UserId;
        public static string Name;
        public static int InitCalls, ShutdownCalls;
        public static void Reset()
        {
            LoggedOn = InitResult = true; RuntimeAppId = AppId; RestartAppId = 0;
            UserId = 76561198000000001; Name = "Steam Player"; InitCalls = ShutdownCalls = 0;
        }
    }
    public struct AppId_t { public uint m_AppId; public AppId_t(uint id) { m_AppId = id; } }
    public struct CSteamID
    {
        public ulong Value;
        public bool IsValid() => Value != 0;
        public override string ToString() => Value.ToString();
    }
    public delegate void SteamAPIWarningMessageHook_t(int severity, StringBuilder text);
    public static class Packsize { public static bool Test() => true; }
    public static class DllCheck { public static bool Test() => true; }
    public static class SteamAPI
    {
        public static bool RestartAppIfNecessary(AppId_t id) { Fixture.RestartAppId = id.m_AppId; return false; }
        public static bool Init() { Fixture.InitCalls++; return Fixture.InitResult; }
        public static void Shutdown() { Fixture.ShutdownCalls++; }
        public static void RunCallbacks() { LeaderboardFixture.Dispatch(); }
    }
    public static class SteamClient { public static void SetWarningMessageHook(SteamAPIWarningMessageHook_t hook) { } }
    public static class SteamUtils { public static AppId_t GetAppID() => new AppId_t(Fixture.RuntimeAppId); }
    public static class SteamUser
    {
        public static bool BLoggedOn() => Fixture.LoggedOn;
        public static CSteamID GetSteamID() => new CSteamID { Value = Fixture.UserId };
    }
    public static class SteamFriends { public static string GetPersonaName() => Fixture.Name; public static string GetFriendPersonaName(CSteamID id) => "World Player " + id.Value; }
}
