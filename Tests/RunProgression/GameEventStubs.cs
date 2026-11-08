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
#if !SCORE_POPUPS
public static class KHS_Script_DumpManager
{
    public static event Action<Collider> OnBallTrigger;
    public static event Action<Collision> OnBallCollision;
    public static event Action<int> OnScore;
    public static event Action<int, Vector3> OnScoreAt;
}
#else
public class YJ_Script_DropTargetController : MonoBehaviour { public void Activate_Object() { } }
public class KHS_Script_BumperEffect : MonoBehaviour { }
#endif
public static class KHS_Script_FliperDumpManager
{
    public static event Action<Collision> OnFliperCollision, OffFliperCollision;
}
public static class KHS_Script_PortalController { public static event Action<int> portalEvt; }
public static class KHS_Script_PlincoFunction { public static event Action ReturnPortalEvt; }
#if !TUTORIAL && !DEMO_GAMEPLAY
public class KHS_Script_PlungerController : MonoBehaviour
{
    public static event Action OnBallLaunched;
    public static void Launch() => OnBallLaunched?.Invoke();
    public static event Action TestTScoreUpdateEvt;
    public bool isBallReady, tutorialForceLock;
}
#endif
public static class KHS_Script_UIImgFunc
{
    public static event Action<bool> RoundUIEvt;
    public static void SetOpen(bool open) => RoundUIEvt?.Invoke(open);
}
namespace PSH { public static class PSH_Script_SceneLoader { public static event Action OnSceneLoadStart; } }
#if !TUTORIAL
namespace PSH { public static class PSH_Script_GameSceneDirector {
    public static event Action NoIntroStartEvt;
    public static void RetrySignal() => NoIntroStartEvt?.Invoke();
} }
#endif
public static class GlitchEffect_RendererFeature { public static bool IsEnabled; }
public class KHS_Script_CameraManager : MonoBehaviour
{
    public static event Action<Camera> CameraChangeEvt;
    public static void SetCamera(Camera camera) => CameraChangeEvt?.Invoke(camera);
    public static event Action<bool> MonitorEvt;
    public static void SetMonitor(bool open) => MonitorEvt?.Invoke(open);
    public static bool IsRoundCameraReady = true;
    public GameObject[] cameraGos;
    public void MonitorOnButton() { }
    public void MonitorOff() { }
}
#if DEMO_GAMEPLAY
public class KHS_Script_RogueLikeManager : MonoBehaviour { public void MatchingFunc(int index) { } }
#endif
public class CJS_Script_CameraShaker : MonoBehaviour { public void OnScored(int score) { } }
// Demo presentation has a separate production-UI fixture; this suite owns scoring.
public class BallARetroDemoPresentation : MonoBehaviour
{
    public static bool CanInstall(Transform parent) => false;
    public static BallARetroDemoPresentation InstallHud(Transform parent, TMPro.TMP_FontAsset font) => null;
    public static BallARetroDemoPresentation InstallResult(CJS_Script_GameOverUI result) => null;
    public void Refresh() { }
    public void SetSubmissionState(bool localOnly) { }
}
// Drawer input, layout and font binding are covered by the production-UI fixture.
public class PSH_Script_QuickChoiceOnOff : MonoBehaviour
{
    public void SetHintFont(TMPro.TMP_FontAsset font) { }
}
public class CJS_Script_LeaderboardList : MonoBehaviour { public void Populate(ScoreRow[] rows) { } }
// Ranking layout/clicks are tested separately with the real UI prefabs.
public class PSH_Script_WorldRankingUI : MonoBehaviour
{
    public event Action Closed;
    public static PSH_Script_WorldRankingUI Install(Transform parent, TMPro.TMP_FontAsset font, bool showEntry) => null;
    public void Show() { }
    public void Close() { }
}
public class CJS_Script_PinballRankingService : MonoBehaviour
{
    public static CJS_Script_PinballRankingService Instance => FindAnyObjectByType<CJS_Script_PinballRankingService>();
    public int Submissions, LastScore;
    public void SubmitScore(int score, string mode, int level, Action<SubmitResp> onDone, Action<string> onFail, int step = 1)
    {
        Submissions++;
        LastScore = score;
        onDone?.Invoke(new SubmitResp { your_score = score, rank = 1, top10 = Array.Empty<ScoreRow>() });
    }
    public void FetchLeaderboard(string mode, int level, Action<LbResp> onDone, Action<string> onFail)
        => onDone?.Invoke(new LbResp { top10 = Array.Empty<ScoreRow>() });
}
