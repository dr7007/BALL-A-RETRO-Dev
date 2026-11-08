// Compiled only in the isolated test project. Camera, choice UI and card
// behavior use production scripts; unrelated game systems supply event seams.
using System;
using UnityEngine;

public class YJ_Script_BallController : MonoBehaviour
{
    public int Credits = 1;
    public int GetBallCount() => Credits;
}
public static class KHS_Script_PlungerController
{
    public static event Action OnBallLaunched;
    public static void RaiseLaunched() => OnBallLaunched?.Invoke();
}
public static class KHS_Script_BallOutController
{
    public static event Action BallOutEvt;
    public static void RaiseBallOut() => BallOutEvt?.Invoke();
}
public class KHS_Script_ScoreManager : MonoBehaviour
{
    public static KHS_Script_ScoreManager Instance => null;
    public static event Action OnGameClear, OnGameOver, Round_Clear;
    public static void RaiseGameOver() => OnGameOver?.Invoke();
    public static void RaiseRoundClear() => Round_Clear?.Invoke();
    public int RoundRespone() => 1;
}
public class KHS_Script_UIImgFunc : MonoBehaviour
{
    public static event Action<bool> RoundUIEvt;
    public static void RaiseRoundUI(bool active) => RoundUIEvt?.Invoke(active);
    public void StartRoundFunc(int round) { }
}
public static class KHS_Script_PortalController
{
    public static event Action<int> portalEvt;
}
public static class KHS_Script_PlincoFunction
{
    public static event Action ReturnPortalEvt;
}
public class KHS_Script_RogueLikeManager : MonoBehaviour { }
public static class PSH_Script_EffectSettings
{
    public static event Action<bool> Changed;
    public static bool CameraShakeEnabled => true;
}
public class CJS_Script_ChoiceState
{
    public static CJS_Script_ChoiceState I => null;
    public void Add(CJS_ChoiceData choice) { }
}
namespace PSH
{
    public static class PSH_Script_GameSceneDirector
    {
        public static event Action NoIntroStartEvt;
        public static event Action<PSH_Script_DialogueUI> OpeningEyeEvt;
    }
    public class PSH_Script_DialogueUI : MonoBehaviour
    {
        public static event Action<string> DialogueEvt;
        public void Play(string id) { }
    }
}
