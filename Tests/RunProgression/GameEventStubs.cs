// Isolated project only. Scoring, HUD, ball, flippers, dialogue and result UI
// are production code; network requests and unrelated scene actors are replaced.
using System;
using UnityEngine;

public class KHS_Script_ResetController : MonoBehaviour
{
    public static event Action OnReset;
    public static void ResetRun() => OnReset?.Invoke();
    public void GameGoToLobbyFunc() { }
}
public static class KHS_Script_BallOutController
{
    public static event Action BallOutEvt;
    public static void Drain() => BallOutEvt?.Invoke();
}
public static class KHS_Script_DumpManager
{
    public static event Action<Collider> OnBallTrigger;
    public static event Action<Collision> OnBallCollision;
    public static event Action<int> OnScore;
}
public static class KHS_Script_FliperDumpManager
{
    public static event Action<Collision> OnFliperCollision, OffFliperCollision;
}
public static class KHS_Script_PortalController { public static event Action<int> portalEvt; }
public static class KHS_Script_PlincoFunction { public static event Action ReturnPortalEvt; }
public static class KHS_Script_PlungerController { public static event Action TestTScoreUpdateEvt; }
namespace PSH { public static class PSH_Script_SceneLoader { public static event Action OnSceneLoadStart; } }
namespace PSH { public static class PSH_Script_GameSceneDirector { public static event Action NoIntroStartEvt; } }
public static class GlitchEffect_RendererFeature { public static bool IsEnabled; }
public class KHS_Script_CameraManager : MonoBehaviour
{
    public static bool IsRoundCameraReady = true;
    public GameObject[] cameraGos;
}
public class CJS_Script_CameraShaker : MonoBehaviour { public void OnScored(int score) { } }
public class CJS_Script_LeaderboardList : MonoBehaviour { public void Populate(ScoreRow[] rows) { } }
public class CJS_Script_PinballRankingService : MonoBehaviour
{
    public static CJS_Script_PinballRankingService Instance => FindAnyObjectByType<CJS_Script_PinballRankingService>();
    public int Submissions, LastScore;
    public void SubmitScore(int score, string mode, int level, Action<SubmitResp> onDone, Action<string> onFail)
    {
        Submissions++;
        LastScore = score;
        onDone?.Invoke(new SubmitResp { your_score = score, rank = 1, top10 = Array.Empty<ScoreRow>() });
    }
    public void FetchLeaderboard(string mode, int level, Action<LbResp> onDone, Action<string> onFail)
        => onDone?.Invoke(new LbResp { top10 = Array.Empty<ScoreRow>() });
}
