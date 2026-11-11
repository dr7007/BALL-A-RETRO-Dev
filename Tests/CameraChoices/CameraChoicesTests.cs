using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class CameraChoicesTestRoller : MonoBehaviour, CJS_IChoiceRoller
{
    public int Picks;
    public List<CJS_ChoiceData> Roll3(out Dictionary<CJS_ChoiceData, float> chances)
    {
        chances = null;
        return new List<CJS_ChoiceData> { new CJS_ChoiceData() };
    }
    public void PushPicked(CJS_ChoiceData picked) { Picks++; }
}

public class CameraChoicesTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private Transform rig, main, monitor, ball;
    private KHS_Script_CameraController follow;
    private KHS_Script_CameraManager manager;
    private CJS_Script_ChoiceUIController choices;
    private CameraChoicesTestRoller roller;
    private Transform content;
    private Button skip;
    private Vector3 mainLocalPosition;
    private Quaternion mainLocalRotation;
    private float oldTimeScale;
    private bool oldCursorVisible;
    private CursorLockMode oldCursorLock;
    private const float TransitionTime = 0.12f;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        oldTimeScale = Time.timeScale;
        oldCursorVisible = Cursor.visible;
        oldCursorLock = Cursor.lockState;
        Time.timeScale = 1f;
        Make("Score state").AddComponent<KHS_Script_ScoreManager>();
        ball = Make("Ball").transform;
        ball.position = new Vector3(0f, 0f, 6f);
        var ballController = ball.gameObject.AddComponent<YJ_Script_BallController>();

        // Match Main_Scene_Game: controller AND manager on CameraHolder;
        // the actual camera is a child with a nonzero local offset/rotation.
        rig = Make("CameraHolder").transform;
        rig.gameObject.SetActive(false);
        main = MakeCamera("Main Camera", new Vector3(0.14f, 4f, -6f));
        var sub = MakeCamera("Sub Camera", new Vector3(20f, 4f, -6f));
        monitor = MakeCamera("Monitor Camera", new Vector3(0f, 3f, 8f));
        var roulette = MakeCamera("RouletteCamera", new Vector3(20f, 5f, 8f));
        mainLocalPosition = main.localPosition;
        mainLocalRotation = main.localRotation;
        monitor.gameObject.SetActive(false);

        follow = rig.gameObject.AddComponent<KHS_Script_CameraController>();
        follow.ball = ball;
        follow.cam = main.GetComponent<Camera>();
        follow.camAxesOffset = new Vector3(0f, 1.5f, -1.8f);
        follow.minWorldZ = -0.39f;
        // This fixture deliberately travels beyond the real board (Z 7.3).
        // Separate escape tests below exercise the shipping Z 6 rig limit.
        SetOptionalField(follow, "maxWorldZ", 20f);
        SetOptionalField(follow, "maxLocalZ", 20f);
        follow.positionSmoothTimeZ = 0.03f;
        follow.zoomInTime = 0f;
        follow.zoomOutTime = 0.02f;
        manager = rig.gameObject.AddComponent<KHS_Script_CameraManager>();
        manager.cameraGos = new[] { main.gameObject, sub.gameObject, monitor.gameObject, roulette.gameObject };
        SetField(manager, "moveDuration", TransitionTime);
        // Intentionally leave cameraController unassigned, matching the stale
        // cameraFollow override saved in the scene before this fix.
        rig.gameObject.SetActive(true);

        var ui = Make("Choice UI");
        ui.SetActive(false);
        roller = ui.AddComponent<CameraChoicesTestRoller>();
        var panel = Make("Choice panel", typeof(RectTransform));
        content = new GameObject("Cards", typeof(RectTransform)).transform;
        content.SetParent(panel.transform, false);
        skip = new GameObject("Skip", typeof(RectTransform), typeof(Button)).GetComponent<Button>();
        skip.transform.SetParent(panel.transform, false);
        // The scene's persistent Skip -> MonitorOff runs before runtime Hide.
        skip.onClick.AddListener(manager.MonitorOff);
        var template = Make("Card template", typeof(RectTransform), typeof(Button));
        template.SetActive(false);
        var card = template.AddComponent<CJS_Script_ChoiceCard>();
        SetField(card, "btn", template.GetComponent<Button>());
        template.SetActive(true);
        choices = ui.AddComponent<CJS_Script_ChoiceUIController>();
        SetField(choices, "rollerBehaviour", roller);
        SetField(choices, "panelRoot", panel);
        SetField(choices, "content", content);
        SetField(choices, "cardPrefab", card);
        SetField(choices, "ballCon", ballController);
        SetField(choices, "btnSkip", skip);
        SetField(choices, "camManager", manager);
        ui.SetActive(true);
        yield return new WaitForSecondsRealtime(0.2f);
        KHS_Script_PlungerController.RaiseLaunched();
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = objects.Count - 1; i >= 0; --i)
            if (objects[i]) Object.DestroyImmediate(objects[i]);
        objects.Clear();
        Time.timeScale = oldTimeScale;
        Cursor.lockState = oldCursorLock;
        Cursor.visible = oldCursorVisible;
    }

    [UnityTest]
    public IEnumerator PickingWhileTravellingUpstairsPreservesCameraOffset()
    {
        yield return TravelToUpperRail();
        yield return OpenChoices();
        content.GetComponentInChildren<Button>().onClick.Invoke();
        Assert.That(roller.Picks, Is.EqualTo(1));
        yield return FinishReturnWhileBallMoves();
    }

    [UnityTest]
    public IEnumerator FullGameMultiplePicksKeepMonitorOpenAndIgnoreStaleCardDoubleClick()
    {
        ball.GetComponent<YJ_Script_BallController>().Credits = 3;
        yield return OpenChoices();
        var first = content.GetComponentInChildren<Button>();
        first.onClick.Invoke();
        first.onClick.Invoke(); // Destroy is deferred; this is a stale card.
        Assert.That(roller.Picks, Is.EqualTo(1));
        Assert.That(Time.timeScale, Is.Zero);
        AssertOnlyCamera(2);
        yield return null;
        content.GetComponentInChildren<Button>().onClick.Invoke();
        Assert.That(roller.Picks, Is.EqualTo(2));
        Assert.That(Time.timeScale, Is.Zero);
        AssertOnlyCamera(2);
        yield return null;
        var last = content.GetComponentInChildren<Button>();
        last.onClick.Invoke();
        last.onClick.Invoke();
        Assert.That(roller.Picks, Is.EqualTo(3));
        yield return FinishReturnWhileBallMoves();
        AssertOnlyCamera(0);
        AssertGameplayCursor();
    }

    [UnityTest]
    public IEnumerator SkippingWhileTravellingUpstairsPreservesCameraOffset()
    {
        yield return TravelToUpperRail();
        yield return OpenChoices();
        skip.onClick.Invoke();
        Assert.That(roller.Picks, Is.Zero);
        yield return FinishReturnWhileBallMoves();
    }

    [UnityTest]
    public IEnumerator ExplicitControllerReferenceKeepsRigAndChildPosesSeparate()
    {
        SetField(manager, "cameraController", follow);
        yield return TravelToUpperRail();
        yield return OpenChoices();
        skip.onClick.Invoke();
        yield return FinishReturnWhileBallMoves();
    }

    [UnityTest]
    public IEnumerator RepeatedChoicesNeverAccumulateCameraOffset()
    {
        for (int i = 0; i < 3; ++i)
        {
            KHS_Script_ScoreManager.Instance.Round = i + 1;
            ball.position += Vector3.forward * 3f;
            yield return new WaitForSecondsRealtime(0.15f);
            yield return OpenChoices();
            skip.onClick.Invoke();
            yield return FinishReturnWhileBallMoves();
        }
    }

    [UnityTest]
    public IEnumerator CompletedChoiceScreenCannotMoveCameraOrPauseAgain()
    {
        yield return OpenChoices();
        content.GetComponentInChildren<Button>().onClick.Invoke();
        yield return FinishReturnWhileBallMoves();
        Assert.That(choices.CanOpenFullScreen, Is.False);
        manager.MonitorOnButton();
        choices.ShowChoices();
        yield return new WaitForSecondsRealtime(.25f);
        AssertOnlyCamera(0);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        AssertGameplayCursor();
        Assert.That(roller.Picks, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator ChoicesDoNotReplaceBallOutDefaultPose()
    {
        SetField(manager, "cameraController", follow);
        KHS_Script_BallOutController.RaiseBallOut();
        yield return new WaitForSecondsRealtime(0.08f);
        Vector3 originalDefault = rig.position;
        follow.StartFollow();
        yield return TravelToUpperRail();
        yield return OpenChoices();
        skip.onClick.Invoke();
        yield return new WaitForSecondsRealtime(0.2f);
        KHS_Script_BallOutController.RaiseBallOut();
        yield return new WaitForSecondsRealtime(0.08f);
        AssertPosition(rig.position, originalDefault, "Ball-out must retain the original gameplay baseline.");
        AssertMainLocalPose();
    }

    [UnityTest]
    public IEnumerator IdleCameraStaysIdleAfterMonitorCloses()
    {
        SetField(manager, "cameraController", follow);
        KHS_Script_BallOutController.RaiseBallOut();
        yield return new WaitForSecondsRealtime(0.08f);
        Vector3 idlePosition = rig.position;
        yield return OpenChoices();
        ball.position += Vector3.forward * 10f;
        skip.onClick.Invoke();
        yield return new WaitForSecondsRealtime(0.25f);
        AssertPosition(rig.position, idlePosition, "Closing a monitor must not start previously idle tracking.");
    }

    [UnityTest]
    public IEnumerator IntroCanReturnWhileGameTimeIsPaused()
    {
        Time.timeScale = 0f;
        manager.MonitorOn();
        yield return new WaitForSecondsRealtime(0.2f);
        manager.MonitorOffAfterDialogue("Intro");
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(Time.timeScale, Is.Zero);
        Assert.That(main.gameObject.activeInHierarchy, Is.True);
        Assert.That(monitor.gameObject.activeInHierarchy, Is.False);
        AssertMainLocalPose();
    }

    [UnityTest]
    public IEnumerator RepeatedOpenAndCloseCallbacksDoNotOverwriteReturnPose()
    {
        yield return TravelToUpperRail();
        yield return OpenChoices();
        manager.MonitorOn();
        skip.onClick.Invoke();
        manager.MonitorOff();
        yield return FinishReturnWhileBallMoves();
        manager.MonitorOff(); // Late callback after the transition is already finished.
        yield return new WaitForSecondsRealtime(0.2f);
        AssertMainLocalPose();
    }

    [UnityTest]
    public IEnumerator EscapedBallCannotPullRigOutsideCalibratedBounds()
    {
        SetOptionalField(follow, "maxWorldZ", 6f);
        SetOptionalField(follow, "maxLocalZ", 6f);
        follow.minLocalZ = -0.39f;
        foreach (var mode in new[] { KHS_Script_CameraController.ZAxisMode.WorldZ, KHS_Script_CameraController.ZAxisMode.LocalZ })
        {
            follow.zMode = mode;
            foreach (float z in new[] { 1000f, -1000f })
            {
                ball.position = new Vector3(20, 100, z);
                follow.StartFollow();
                for (int frame = 0; frame < 8; frame++)
                {
                    Assert.That(rig.position.z, Is.InRange(-0.3901f, 6.0001f), "Both launch snap and continuous follow must obey bounds.");
                    yield return null;
                }
                Assert.That(rig.position.x, Is.Zero.Within(0.001f));
                Assert.That(rig.position.y, Is.Zero.Within(0.001f));
                AssertMainLocalPose();
            }
        }
    }

    [UnityTest]
    public IEnumerator RouletteRoundClearSelectsMainAndRejectsStalePortalCallbacks()
    {
        manager.SubCamOn(3);
        AssertOnlyCamera(3);
        KHS_Script_ScoreManager.RaiseRoundClear();
        AssertOnlyCamera(0);
        manager.SubCamOn(3); // A queued portal callback during the round banner.
        AssertOnlyCamera(0);
        KHS_Script_UIImgFunc.RaiseRoundUI(false);
        manager.SubCamOn(3); // The banner-to-choices gap is still owned by the round flow.
        AssertOnlyCamera(0);
        yield return OpenChoices();
        AssertOnlyCamera(2);
        manager.SubCamOn(3);
        manager.MainCamOn();
        AssertOnlyCamera(2);
        skip.onClick.Invoke();
        yield return new WaitForSecondsRealtime(0.25f);
        AssertOnlyCamera(0);
        AssertGameplayCursor();
    }

    [UnityTest]
    public IEnumerator BallOutDuringMonitorRestoresDefaultPoseAndFovWhilePaused()
    {
        yield return TravelToUpperRail();
        yield return OpenChoices();
        KHS_Script_BallOutController.RaiseBallOut();
        manager.MonitorOff();
        yield return new WaitForSecondsRealtime(0.3f);
        Assert.That(Time.timeScale, Is.Zero);
        AssertPosition(rig.position, Vector3.zero, "A queued ball-out reset must survive main-camera reactivation.");
        Assert.That(main.GetComponent<Camera>().fieldOfView, Is.EqualTo(60f).Within(0.01f));
        AssertMainLocalPose();
    }

    [UnityTest]
    public IEnumerator RouletteReturnDoesNotReplaceDefaultOrWriteRouletteFov()
    {
        yield return TravelToUpperRail();
        var roulette = manager.cameraGos[3].GetComponent<Camera>();
        roulette.fieldOfView = 33f;
        manager.SubCamOn(3);
        ball.position += Vector3.forward * 200;
        yield return null;
        manager.MainCamOn();
        Time.timeScale = 0;
        KHS_Script_BallOutController.RaiseBallOut();
        yield return new WaitForSecondsRealtime(0.1f);
        AssertPosition(rig.position, Vector3.zero, "Roulette must not rebase the gameplay rig.");
        Assert.That(main.GetComponent<Camera>().fieldOfView, Is.EqualTo(60f).Within(0.01f));
        Assert.That(roulette.fieldOfView, Is.EqualTo(33f).Within(0.01f));
        AssertMainLocalPose();
    }

    [UnityTest]
    public IEnumerator TutorialFocusHoldsRigAndRestoresLocalCameraBeforeFollowing()
    {
        KHS_Script_BallOutController.RaiseBallOut();
        yield return new WaitForSecondsRealtime(0.08f);
        var focus = MakeFocus();
        roller.enabled = false;
        focus.disableWhileFocusing = new MonoBehaviour[] { roller };
        focus.CaptureOriginal();
        var marker = Make("Focus Marker").transform;
        marker.position = new Vector3(2, 6, 3);
        marker.rotation = Quaternion.Euler(70, 10, 0);
        focus.FocusTo(marker);
        KHS_Script_PlungerController.RaiseLaunched();
        ball.position = new Vector3(0, 2, 16);
        yield return new WaitForSecondsRealtime(0.12f);
        AssertPosition(rig.position, Vector3.zero, "The parent rig must not follow underneath a child focus tween.");
        focus.Restore();
        focus.ReEnableDisabled();
        yield return null;
        AssertPosition(rig.position, Vector3.zero, "Tracking resumes only after the child restore tween completes.");
        yield return new WaitForSecondsRealtime(0.25f);
        AssertMainLocalPose();
        Assert.That(rig.position.z, Is.GreaterThan(10f), "A launch received during focus must resume tracking.");
        Assert.That(roller.enabled, Is.False, "Focus may not enable a component that was already disabled.");
    }

    [UnityTest]
    public IEnumerator TutorialFailureDuringFocusRestoresRigChildAndFovAtTimeScaleZero()
    {
        KHS_Script_BallOutController.RaiseBallOut();
        yield return new WaitForSecondsRealtime(0.08f);
        var focus = MakeFocus();
        focus.CaptureOriginal();
        focus.Restore();
        focus.ReEnableDisabled();
        yield return new WaitForSecondsRealtime(0.12f);
        follow.StartFollow();
        yield return TravelToUpperRail();
        var marker = Make("Retry Focus Marker").transform;
        marker.position = new Vector3(-3, 5, 8);
        focus.FocusTo(marker);
        yield return null;
        Time.timeScale = 0;
        // Reflection keeps the same regression fixture compilable against HEAD.
        var restore = typeof(CJS_TutorialCameraFocus).GetMethod("RestoreGameplayView");
        Assert.That(restore, Is.Not.Null, "Tutorial result handling needs an explicit gameplay-view restore.");
        restore.Invoke(focus, null);
        yield return new WaitForSecondsRealtime(0.3f);
        AssertPosition(rig.position, Vector3.zero, "Tutorial retry must restore the rig even without generic GameOver.");
        AssertMainLocalPose();
        Assert.That(main.GetComponent<Camera>().fieldOfView, Is.EqualTo(60f).Within(0.01f));
        Assert.That(Time.timeScale, Is.Zero);
    }

    [UnityTest]
    public IEnumerator StackedShakeStaysWithinConfiguredAmplitudeAndDoesNotPolluteMonitorPose()
    {
        var shaker = main.gameObject.AddComponent<CJS_Script_CameraShaker>();
        shaker.maxAmplitude = 0.04f;
        shaker.useUnscaledTime = true;
        shaker.falloff = AnimationCurve.Constant(0, 1, 1);
        for (int i = 0; i < 100; i++) shaker.Shake(0.04f, 1f);
        for (int frame = 0; frame < 15; frame++)
        {
            yield return null;
            Vector3 offset = main.localPosition - mainLocalPosition;
            Assert.That(Mathf.Abs(offset.x), Is.LessThanOrEqualTo(0.0401f));
            Assert.That(Mathf.Abs(offset.y), Is.LessThanOrEqualTo(0.0401f));
        }
        yield return OpenChoices();
        skip.onClick.Invoke();
        yield return new WaitForSecondsRealtime(0.25f);
        AssertMainLocalPose();
    }

    [UnityTest]
    public IEnumerator RepeatedChoiceHideAfterGameOverPreservesEndingInputAndPause()
    {
        yield return OpenChoices();
        KHS_Script_ScoreManager.RaiseGameOver();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 0;
        choices.Hide();
        choices.Hide();
        choices.ShowChoices();
        manager.MonitorOff();
        yield return new WaitForSecondsRealtime(0.3f);
        Assert.That(Time.timeScale, Is.Zero);
        Assert.That(Cursor.visible, Is.True);
        Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
        Assert.That(content.childCount, Is.Zero, "Ending callbacks must not recreate choice cards.");
        Assert.That(skip.gameObject.activeInHierarchy, Is.False);
        Assert.That(typeof(CJS_Script_ChoiceUIController).GetField("isOpen", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(choices), Is.False);
        Assert.That(roller.Picks, Is.Zero);
    }

    [UnityTest]
    public IEnumerator RoundBannerOpeningDoesNotUnpauseAnExistingModal()
    {
        Time.timeScale = 0;
        KHS_Script_UIImgFunc.RaiseRoundUI(true);
        yield return null;
        Assert.That(Time.timeScale, Is.Zero, "Only the banner completion may resume gameplay.");
    }

    private CJS_TutorialCameraFocus MakeFocus()
    {
        var focus = Make("Tutorial Focus").AddComponent<CJS_TutorialCameraFocus>();
        focus.cam = main.GetComponent<Camera>();
        focus.moveDuration = 0.06f;
        return focus;
    }

    [UnityTest]
    public IEnumerator InterruptedDefaultReturnResumesAfterMonitorAndTutorialFocus()
    {
        follow.zoomOutTime = 0.18f;
        yield return TravelToUpperRail();
        KHS_Script_BallOutController.RaiseBallOut();
        yield return null;
        manager.MonitorOn();
        yield return new WaitForSecondsRealtime(0.2f);
        manager.MonitorOff();
        yield return new WaitForSecondsRealtime(0.4f);
        AssertPosition(rig.position, Vector3.zero, "A monitor may temporarily interrupt, but not discard, an ongoing reset.");
        follow.StartFollow();
        yield return TravelToUpperRail();
        KHS_Script_BallOutController.RaiseBallOut();
        yield return null;
        var focus = MakeFocus();
        focus.CaptureOriginal();
        var marker = Make("Interrupted Return Focus").transform;
        marker.position = new Vector3(2, 6, 3);
        focus.FocusTo(marker);
        yield return new WaitForSecondsRealtime(0.12f);
        focus.Restore();
        focus.ReEnableDisabled();
        yield return new WaitForSecondsRealtime(0.4f);
        AssertPosition(rig.position, Vector3.zero, "Tutorial focus may not discard an ongoing reset.");
        AssertMainLocalPose();
        Assert.That(main.GetComponent<Camera>().fieldOfView, Is.EqualTo(60f).Within(0.01f));
    }
    private void AssertOnlyCamera(int active)
    {
        for (int i = 0; i < manager.cameraGos.Length; i++)
            Assert.That(manager.cameraGos[i].activeInHierarchy, Is.EqualTo(i == active), $"Camera {i}, expected active {active}");
    }
    private static void AssertGameplayCursor()
    {
        Assert.That(Cursor.visible, Is.False);
        // Batch Editor has no focused Game View and releases native pointer capture.
        if (Application.isFocused && !Application.isBatchMode)
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.Locked));
    }

    private IEnumerator TravelToUpperRail()
    {
        ball.position = new Vector3(0f, 2f, 16f);
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(rig.position.z, Is.GreaterThan(10f), "The fixture must reach the upper rail before opening choices.");
    }

    private IEnumerator OpenChoices()
    {
        choices.ShowChoices();
        manager.MonitorOnButton();
        yield return new WaitForSecondsRealtime(0.2f);
        AssertMainLocalPose();
        Assert.That(Cursor.visible, Is.True);
        Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
    }

    private IEnumerator FinishReturnWhileBallMoves()
    {
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        float until = Time.realtimeSinceStartup + 0.22f;
        while (Time.realtimeSinceStartup < until)
        {
            ball.position += Vector3.forward * Time.deltaTime * 2f;
            AssertMainLocalPose(); // Also check during, not only after the return.
            yield return null;
        }
        yield return new WaitForSecondsRealtime(0.15f);
        AssertMainLocalPose();
        Assert.That(rig.position.z, Is.EqualTo(ball.position.z - 1.8f).Within(0.03f));
        Assert.That(main.gameObject.activeInHierarchy, Is.True);
        Assert.That(monitor.gameObject.activeInHierarchy, Is.False);
        AssertGameplayCursor();
    }

    private void AssertMainLocalPose()
    {
        AssertPosition(main.localPosition, mainLocalPosition, "Monitor transitions must preserve the gameplay camera's rig offset.");
        Assert.That(Quaternion.Angle(main.localRotation, mainLocalRotation), Is.LessThan(0.01f));
    }
    private static void AssertPosition(Vector3 actual, Vector3 expected, string message)
    {
        Assert.That(Vector3.Distance(actual, expected), Is.LessThan(0.002f), message + $" Actual: {actual}; expected: {expected}");
    }
    private GameObject Make(string name, params System.Type[] types)
    {
        var go = new GameObject(name, types);
        objects.Add(go);
        return go;
    }
    private Transform MakeCamera(string name, Vector3 position)
    {
        var go = new GameObject(name, typeof(Camera));
        go.transform.SetParent(rig, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.Euler(43.5f, 0f, 0f);
        return go.transform;
    }
    private static void SetField(object target, string name, object value)
    {
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
    private static void SetOptionalField(object target, string name, object value)
    {
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public)?.SetValue(target, value);
    }
}
