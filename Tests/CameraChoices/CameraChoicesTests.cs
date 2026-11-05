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
    private const float TransitionTime = 0.12f;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        oldTimeScale = Time.timeScale;
        Time.timeScale = 1f;
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
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = objects.Count - 1; i >= 0; --i)
            if (objects[i]) Object.DestroyImmediate(objects[i]);
        objects.Clear();
        Time.timeScale = oldTimeScale;
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
            ball.position += Vector3.forward * 3f;
            yield return new WaitForSecondsRealtime(0.15f);
            yield return OpenChoices();
            skip.onClick.Invoke();
            yield return FinishReturnWhileBallMoves();
        }
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
}
