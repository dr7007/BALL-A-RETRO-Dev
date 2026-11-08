// Only the isolated test project compiles these stand-ins. Ball motion, rolling
// audio, choice UI, and card selection all use the unmodified production files.
using System;
using UnityEngine;

public static class KHS_Script_ResetController
{
    public static event Action OnReset;
}

public static class KHS_Script_BallOutController
{
    public static event Action BallOutEvt;
}

public class KHS_Script_ScoreManager
{
    public static KHS_Script_ScoreManager Instance => null;
    public static event Action OnGameClear, OnGameOver, Round_Clear;
    public void HandleBallOut(YJ_Script_BallController ball) { }
    public int RoundRespone() => 1;
}

public class KHS_Script_CameraManager : MonoBehaviour
{
    public void MonitorOff() { }
    public void MonitorOnButton() { }
}

public static class KHS_Script_UIImgFunc { public static event Action<bool> RoundUIEvt; }
namespace PSH { public static class PSH_Script_GameSceneDirector { public static event Action NoIntroStartEvt; } }

public class KHS_Script_RogueLikeManager : MonoBehaviour { }

public class CJS_Script_ChoiceState
{
    public static CJS_Script_ChoiceState I => null;
    public void Add(CJS_ChoiceData choice) { }
}
