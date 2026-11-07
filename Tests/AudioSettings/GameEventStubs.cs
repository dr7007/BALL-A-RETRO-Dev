using System;
using UnityEngine;
// Only event sources/VFX are substituted; both audio controllers and UI bridge are production code.
public static class KHS_Script_PlungerController { public static event Action OnBallLaunched; }
public static class KHS_Script_DumpManager { public static event Action<Collision> OnBallCollision; }
public static class KHS_Script_ScoreManager { public static event Action OnGameClear, OnGameOver; }
public static class KHS_Script_BallOutController { public static event Action BallOutEvt; }
public static class KHS_Script_FliperController { public static event Action OnAnyFlipperPress, OnAnyFlipperRelease; }
public static class KHS_Script_FliperDumpManager { public static event Action<Collision> OnFliperCollision; }
public static class KHS_Script_PortalController { public static event Action<int> portalEvt; public static event Action portalEndEvt; }
public static class PSH_Script_EffectSettings { public static bool IsEnabled => true; public static void SetEnabled(bool value) {} }
