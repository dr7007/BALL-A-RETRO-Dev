using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class ScorePopupTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private KHS_Script_ScoreManager score;
    private CJS_Script_ScorePopupSpawner spawner;
    private Canvas canvas;
    private Camera camera;
    private Rigidbody ball;
    private SimulationMode previousSimulation;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.timeScale = 1f;
        previousSimulation = Physics.simulationMode;
        Physics.simulationMode = SimulationMode.Script;
        camera = New("PlayCamera", typeof(Camera)).GetComponent<Camera>();
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(0, 6, -8);
        camera.transform.LookAt(Vector3.zero);
        camera.orthographic = true;
        camera.orthographicSize = 4;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.035f, 0.06f);
        canvas = New("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        ball = New("Ball", typeof(SphereCollider), typeof(Rigidbody)).GetComponent<Rigidbody>();
        ball.tag = "Ball";
        ball.useGravity = false;
        ball.GetComponent<SphereCollider>().radius = 0.125f;
        ball.transform.position = new Vector3(-2, 0, -1);
        Physics.SyncTransforms();
        var controller = ball.gameObject.AddComponent<YJ_Script_BallController>();
        Set(controller, "Gravity", 0f);

        var holder = New("CameraHolder").AddComponent<KHS_Script_CameraManager>();
        holder.cameraGos = new[] { camera.gameObject };
        var managerGO = New("Score"); managerGO.SetActive(false);
        score = managerGO.AddComponent<KHS_Script_ScoreManager>();
        score.targetScores = new[] { 100000, 200000, 300000 };
        Set(score, "goalRound", 3);
        Set(score, "currentgameScores", new int[3]);
        Set(score, "camHolder", holder);
        Set(score, "ballTransformFallback", ball.transform);
        managerGO.SetActive(true);

        spawner = New("Popups").AddComponent<CJS_Script_ScorePopupSpawner>();
        spawner.canvasRoot = (RectTransform)canvas.transform;
        spawner.worldCamera = camera;
        spawner.popupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ScorePopup.prefab").GetComponent<TextMeshProUGUI>();
        spawner.duration = 10f;
        spawner.riseDistance = 0f;
        yield return null;
        Canvas.ForceUpdateCanvases();
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = objects.Count - 1; i >= 0; i--) if (objects[i]) Object.DestroyImmediate(objects[i]);
        objects.Clear();
        Physics.simulationMode = previousSimulation;
        Time.timeScale = 1f;
    }

    [Test]
    public void TwoHitsAppearAtTheirOwnPositionsAndApplyMultiplierOnlyOnce()
    {
        score.multiplier = 2;
        Vector3 left = new Vector3(-2, 0, 1), right = new Vector3(2, 0, 1);
        score.AddScoreAt(25, left); score.AddScoreAt(60, right);
        var popups = Popups();
        Assert.That(popups.Length, Is.EqualTo(2));
        Assert.That(score.curScore, Is.EqualTo(170));
        Assert.That(popups[0].text, Is.EqualTo("+50"));
        Assert.That(popups[1].text, Is.EqualTo("+120"));
        At(popups[0], left); At(popups[1], right);
        Assert.That(popups[0].raycastTarget, Is.False);
        Assert.That(popups[0].GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
    }

    [Test]
    public void TriggerKeepsOriginalHitPointWhenAnotherListenerTeleportsTheBall()
    {
        var obstacle = New("Sensor").AddComponent<KHS_Script_DumpManager>();
        obstacle.transform.position = new Vector3(-2.3f, 0, -1);
        obstacle.bumpScore = 50;
        var collider = ball.GetComponent<Collider>();
        Vector3 expected = collider.ClosestPoint(obstacle.transform.position);
        System.Action<Collider> teleport = c => { ball.position = Vector3.one * 20; ball.gameObject.SetActive(false); };
        KHS_Script_DumpManager.OnBallTrigger += teleport;
        try { Call(obstacle, "OnTriggerEnter", collider); }
        finally { KHS_Script_DumpManager.OnBallTrigger -= teleport; }
        Assert.That(score.curScore, Is.EqualTo(50));
        Assert.That(Popups().Length, Is.EqualTo(1));
        At(Popups()[0], expected);
        Call(obstacle, "OnTriggerEnter", collider);
        Assert.That(score.curScore, Is.EqualTo(50), "Trigger cooldown must not award twice.");
    }

    [Test]
    public void PhysicsCollisionAwardsOnceAtItsActualContactPoint()
    {
        ball.position = new Vector3(-0.55f, 0, 0);
        var obstacle = New("Bumper", typeof(BoxCollider)).AddComponent<KHS_Script_DumpManager>();
        obstacle.bumpScore = 35;
        obstacle.bounceForce = 0;
        obstacle.GetComponent<BoxCollider>().size = Vector3.one;
        Vector3 hit = Vector3.positiveInfinity;
        System.Action<Collision> capture = c => hit = c.GetContact(0).point;
        KHS_Script_DumpManager.OnBallCollision += capture;
        try { Physics.SyncTransforms(); Physics.Simulate(0.02f); }
        finally { KHS_Script_DumpManager.OnBallCollision -= capture; }
        Assert.That(score.curScore, Is.EqualTo(35));
        Assert.That(Popups().Length, Is.EqualTo(1));
        At(Popups()[0], hit);
    }

    [Test]
    public void CollidingNonBallDoesNotCreateScoreOrPopup()
    {
        ball.tag = "Untagged";
        ball.position = new Vector3(-0.55f, 0, 0);
        var obstacle = New("Bumper", typeof(BoxCollider)).AddComponent<KHS_Script_DumpManager>();
        obstacle.bumpScore = 35;
        Physics.SyncTransforms(); Physics.Simulate(0.02f);
        Assert.That(score.curScore, Is.Zero);
        Assert.That(Popups(), Is.Empty);
    }

    [Test]
    public void CoinPopupRemainsAtTheCollectedCoinAfterItHides()
    {
        var coin = New("Coin").AddComponent<YJ_Script_PacManCoin>();
        coin.transform.position = new Vector3(1, 0, 1);
        Call(coin, "Start");
        Call(coin, "OnTriggerEnter", ball.GetComponent<Collider>());
        Assert.That(coin.gameObject.activeSelf, Is.False);
        At(Popups().Single(), coin.transform.position);
    }

    [Test]
    public void BonusWithoutExplicitPositionUsesTheBoundBallPosition()
    {
        score.AddScore(200);
        At(Popups().Single(), ball.transform.position);
    }

    [Test]
    public void CameraMovementKeepsThePopupOnTheHitInsteadOfFollowingTheBall()
    {
        var hit = new Vector3(1, 0, 0);
        score.AddScoreAt(20, hit);
        camera.transform.position += Vector3.right;
        ball.position = new Vector3(-4, 0, 0);
        Call(spawner, "LateUpdate");
        At(Popups().Single(), hit);
    }

    [UnityTest]
    public IEnumerator CameraSwitchUsesTheNewViewAndRemovesOldViewPopups()
    {
        score.AddScoreAt(20, Vector3.zero);
        var sub = New("BonusCamera", typeof(Camera)).GetComponent<Camera>();
        sub.transform.position = new Vector3(1, 8, -5); sub.transform.LookAt(Vector3.zero);
        KHS_Script_CameraManager.SetCamera(sub);
        camera.gameObject.SetActive(false);
        var point = new Vector3(1, 0, 1);
        score.AddScoreAt(30, point);
        yield return null;
        Assert.That(Popups().Length, Is.EqualTo(1));
        At(Popups()[0], point, sub);
    }

    [TestCase(RenderMode.ScreenSpaceOverlay)]
    [TestCase(RenderMode.ScreenSpaceCamera)]
    public void ScaledOffsetParentUsesItsOwnCoordinateSystem(RenderMode mode)
    {
        canvas.renderMode = mode;
        canvas.worldCamera = camera;
        canvas.planeDistance = 2f;
        var parent = New("Offset parent", typeof(RectTransform)).GetComponent<RectTransform>();
        parent.SetParent(canvas.transform, false);
        parent.anchoredPosition = new Vector2(170, -90);
        parent.localScale = Vector3.one * 0.7f;
        parent.pivot = new Vector2(0.2f, 0.8f);
        spawner.spawnParent = parent;
        Canvas.ForceUpdateCanvases();
        var point = new Vector3(1, 0, 1);
        score.AddScoreAt(50, point);
        At(Popups().Single(), point);
    }

    [Test]
    public void BehindCameraAndOffscreenHitsDoNotCreateMisleadingPopups()
    {
        score.AddScoreAt(10, camera.transform.position - camera.transform.forward);
        score.AddScoreAt(10, Vector3.right * 1000);
        score.AddScoreAt(0, Vector3.zero);
        Assert.That(score.curScore, Is.EqualTo(20));
        Assert.That(Popups(), Is.Empty);
    }

    [UnityTest]
    public IEnumerator ExpiredAndDisabledPopupsAreRemoved()
    {
        spawner.duration = 0.05f;
        score.AddScoreAt(10, Vector3.zero);
        yield return new WaitForSecondsRealtime(0.12f);
        yield return null;
        Assert.That(Popups(), Is.Empty);
        spawner.duration = 10;
        score.AddScoreAt(10, Vector3.zero);
        spawner.enabled = false;
        yield return null;
        Assert.That(Popups(), Is.Empty);
        score.AddScoreAt(10, Vector3.zero);
        Assert.That(Popups(), Is.Empty);
    }

    [UnityTest]
    public IEnumerator RenderActualPopupPrefabAtThreeHitLocations()
    {
        var target = new RenderTexture(1920, 1080, 24);
        camera.targetTexture = target;
        camera.aspect = 1920f / 1080f;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera; canvas.planeDistance = 1f;
        Canvas.ForceUpdateCanvases();
        Vector3[] positions = { new Vector3(-2, 0, 1), Vector3.zero, new Vector3(2, 0, -1) };
        for (int i = 0; i < positions.Length; i++)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder); objects.Add(marker);
            marker.transform.position = positions[i] - Vector3.up * 0.15f;
            marker.transform.localScale = new Vector3(0.6f, 0.1f, 0.6f);
            score.AddScoreAt((i + 1) * 50, positions[i]);
        }
        yield return null;
        Canvas.ForceUpdateCanvases();
        Call(spawner, "LateUpdate");
        var popups = Popups();
        for (int i = 0; i < positions.Length; i++) At(popups[i], positions[i]);
        var pixels = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        camera.Render(); RenderTexture.active = target;
        pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); pixels.Apply();
        File.WriteAllBytes(Path.Combine(Application.dataPath, "../score-hit-popups.png"), pixels.EncodeToPNG());
        camera.targetTexture = null; RenderTexture.active = previous;
        Object.DestroyImmediate(pixels); target.Release(); Object.DestroyImmediate(target);
        Assert.That(Popups().Length, Is.EqualTo(3));
    }

    private TextMeshProUGUI[] Popups() => canvas.GetComponentsInChildren<TextMeshProUGUI>().Where(t => t.name == spawner.popupPrefab.name + "(Clone)").ToArray();
    private void At(TextMeshProUGUI text, Vector3 world, Camera view = null)
    {
        var actual = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, text.rectTransform.position);
        var expected = (Vector2)(view != null ? view : camera).WorldToScreenPoint(world);
        Assert.That(Vector2.Distance(actual, expected), Is.LessThan(1.1f), "Popup must be centered on the hit's projected position.");
    }
    private GameObject New(string name, params System.Type[] components) { var go = new GameObject(name, components); objects.Add(go); return go; }
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static void Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
}
