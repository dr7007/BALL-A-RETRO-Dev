// This isolated fixture replaces unrelated scene/UI services. ScoreManager,
// RogueLikeManager, BumperComboManager, BumperEffect and DemoRun are production.
using System;
using UnityEngine;

namespace Unity.Collections { }
namespace Unity.VisualScripting { }
namespace PSH { public static class PSH_Script_SceneLoader { public static event Action OnSceneLoadStart; } }

public class KHS_Script_ResetController : MonoBehaviour
{
    public static event Action OnReset;
    public static void ResetRun() => OnReset?.Invoke();
}
public static class KHS_Script_BallOutController
{
    public static event Action BallOutEvt;
    public static void Drain() => BallOutEvt?.Invoke();
}
public class KHS_Script_DumpManager : MonoBehaviour
{
    public static event Action<Collider> OnBallTrigger;
    public static event Action<Collision> OnBallCollision;
    public static event Action<int, Vector3> OnScoreAt;
    public int bumpScore;
    public float bounceForce;
    public bool isSpecial;
}
public static class KHS_Script_FliperDumpManager { public static event Action<Collision> OnFliperCollision; }
public static class KHS_Script_PortalController { public static event Action<int> portalEvt; }
public class KHS_Script_PlincoFunction : MonoBehaviour
{
    public static event Action ReturnPortalEvt;
    public void AddScoreMulti(int amount) { }
}
public class KHS_Script_UIToPlinco : MonoBehaviour { public void UpdatePlincoUI() { } }
public class KHS_Script_UIToBall : MonoBehaviour { public void BallInfoUpdate() { } }
public class KHS_Script_BatteryLedManager : MonoBehaviour { public void ForcedOn() { } }
public class YJ_Script_BlackholeController : MonoBehaviour { public bool isForceActive, isActivated; }
public class CJS_Script_ChoiceRoller : MonoBehaviour { public void UnActiveChoices(int id) { } }
public class KHS_Script_FliperController : MonoBehaviour
{
    public int fliper_Count = 10;
    public void FlipperCountUp(int amount) { }
    public void FlipperCountUpdate() { }
}
public class YJ_Script_BallController : MonoBehaviour
{
    private int count = 2;
    public int GetBallCount() => count;
    public void SetBallCount(int value) => count = value;
    public void ConfigureRunBallCredits(int value) => count = value;
    public void AddBallFunc(int value) => count += value;
}
public class KHS_Script_CameraManager : MonoBehaviour
{
    public static event Action<bool> MonitorEvt;
    public static bool IsRoundCameraReady = true;
    public GameObject[] cameraGos;
}
public class CJS_Script_CameraShaker : MonoBehaviour { public void OnScored(int score) { } }
public class CJS_Script_GameOverUI : MonoBehaviour
{
    public bool autoSubmitOnShow;
    public void SetFinalScore(int score) { }
    public void Show(int score) { }
}
public class ScoreUpgradeSubmitResponse { public int rank; }
public class CJS_Script_PinballRankingService : MonoBehaviour
{
    public void SubmitScore(int score, string mode, int level, Action<ScoreUpgradeSubmitResponse> onDone,
        Action<string> onFail, int step) { }
}
public class ChangeSpriteRenderer : MonoBehaviour
{
    public bool On;
    public Light obj_Light;
    public Color Emission_On, Emission_Off_;
    public void F_ChangeSprite_Off() { }
    public void F_ChangeSprite_On() { }
    public void Led_On_With_Timer(float seconds) { }
}
