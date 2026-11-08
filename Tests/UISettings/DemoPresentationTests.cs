using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class DemoPresentationTests
{
    private GameObject root, scoreObject;
    private string bestKey, oldBest;
    private bool hadBest;
    private Camera captureCamera;
    private Canvas captureCanvas;
    private RenderTexture captureTarget;
    private RenderMode originalMode;
    private Camera originalCamera;
    private float originalPlane;
    private RenderPipelineAsset originalPipeline;
    private UniversalRendererData captureRenderer;
    private UniversalRenderPipelineAsset capturePipeline;

    [UnitySetUp] public IEnumerator Setup()
    {
#if BALLARETRO_STEAM
        Steamworks.Fixture.Reset(); Steamworks.LeaderboardFixture.Reset();
        typeof(SteamManager).GetMethod("InitOnPlayMode", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
#endif
        LocaleTestContext.Select("en"); yield return LocalizationSettings.InitializationOperation;
        bestKey = BallARetroPersonalRecordStore.StorageKey("Classic", 1);
        hadBest = PlayerPrefs.HasKey(bestKey); oldBest = PlayerPrefs.GetString(bestKey);
        PlayerPrefs.DeleteKey(bestKey);
        BallARetroDemoRun.Current = null;
        Time.timeScale = 1;
        originalPipeline = GraphicsSettings.defaultRenderPipeline;
        captureRenderer = ScriptableObject.CreateInstance<UniversalRendererData>();
        capturePipeline = UniversalRenderPipelineAsset.Create(captureRenderer);
        GraphicsSettings.defaultRenderPipeline = capturePipeline;
    }
    [TearDown] public void Cleanup()
    {
        if (captureCanvas != null)
        {
            captureCanvas.renderMode = originalMode; captureCanvas.worldCamera = originalCamera;
            captureCanvas.planeDistance = originalPlane;
        }
        if (captureCamera != null) { captureCamera.targetTexture = null; Object.DestroyImmediate(captureCamera.gameObject); }
        if (captureTarget != null) RenderTexture.ReleaseTemporary(captureTarget);
        captureCamera = null; captureCanvas = null; captureTarget = null;
        GraphicsSettings.defaultRenderPipeline = originalPipeline;
        if (capturePipeline != null) Object.DestroyImmediate(capturePipeline);
        if (captureRenderer != null) Object.DestroyImmediate(captureRenderer);
        if (root) Object.DestroyImmediate(root);
        if (scoreObject) Object.DestroyImmediate(scoreObject);
        BallARetroDemoRun.Current = null;
        foreach (var service in Object.FindObjectsByType<CJS_Script_PinballRankingService>(FindObjectsSortMode.None)) Object.DestroyImmediate(service.gameObject);
        foreach (var cursor in Object.FindObjectsByType<PSH_Script_CursorManager>(FindObjectsSortMode.None)) Object.DestroyImmediate(cursor.gameObject);
        foreach (var steam in Object.FindObjectsByType<SteamManager>(FindObjectsSortMode.None)) Object.DestroyImmediate(steam.gameObject);
        if (hadBest) PlayerPrefs.SetString(bestKey, oldBest); else PlayerPrefs.DeleteKey(bestKey);
        PlayerPrefs.Save(); Time.timeScale = 1; LocaleTestContext.Select("ko");
    }
    private static TMP_Text Text(Transform parent, string name) => parent.GetComponentsInChildren<TMP_Text>(true).Single(t => t.name == name);
    private void RequireDemo() { if (!BallARetroPlatform.IsDemoBuild) Assert.Ignore("Demo-specific presentation; retail exclusion is tested separately."); }

#if !BALLARETRO_STEAM || !BALLARETRO_DEMO
    [Test] public void RetailBuildDoesNotCreateAnyDemoObjects()
    {
        if (BallARetroPlatform.IsDemoBuild) Assert.Ignore("Run in STOVE and full Steam configurations.");
        root = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        Assert.That(BallARetroDemoPresentation.InstallLobby(root.transform, null), Is.Null);
        Assert.That(BallARetroDemoPresentation.InstallHud(root.transform, null), Is.Null);
        Assert.That(root.transform.childCount, Is.Zero);
    }
#endif
#if BALLARETRO_STEAM && BALLARETRO_DEMO
    [UnityTest] public IEnumerator TutorialSceneNeverCreatesDemoUI()
    {
        RequireDemo();
        var tutorial = SceneManager.CreateScene("Main_Scene_Tutorial_PresentationFixture");
        root = new GameObject("TutorialCanvas", typeof(RectTransform), typeof(Canvas));
        SceneManager.MoveGameObjectToScene(root, tutorial);
        Assert.That(BallARetroDemoPresentation.InstallLobby(root.transform, null), Is.Null);
        Assert.That(BallARetroDemoPresentation.InstallHud(root.transform, null), Is.Null);
        Object.DestroyImmediate(root); root = null;
        yield return SceneManager.UnloadSceneAsync(tutorial);
    }
    [UnityTest] public IEnumerator LobbyUsesActualPrefabAndKeepsPersonalBestWhenOffline()
    {
        RequireDemo();
        root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Public/Main_Lobby/UI_Lobby/Prefabs/Canvas_Lobby.prefab"));
        var loaderObject = new GameObject("Loader", typeof(PSH.PSH_Script_SceneLoader));
        loaderObject.transform.SetParent(root.transform);
        var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        yield return null;
        var lobby = root.GetComponentsInChildren<BallARetroDemoPresentation>(true).Single(t => t.name == "DemoLobby");
        Assert.That(BallARetroDemoPresentation.InstallLobby(root.transform, null), Is.SameAs(lobby));
        var wishlist = lobby.GetComponentInChildren<Button>();
        Assert.That(wishlist.name, Is.EqualTo("Wishlist")); Assert.That(wishlist.interactable, Is.True);
        var ranking = root.GetComponentInChildren<PSH_Script_WorldRankingUI>();
        ranking.Show();
        Assert.That(Text(ranking.transform, "PersonalBest").text, Is.EqualTo(PSH_Script_Localization.Text("Demo_NoPersonalBest")));
        BallARetroPersonalRecordStore.Submit("Fixture", 8765, "Classic", 1);
        ranking.Close(); ranking.Show();
        Assert.That(Text(ranking.transform, "PersonalBest").text, Is.EqualTo(PSH_Script_Localization.Text("Demo_PersonalBest", 8765)));
        Assert.That(ranking.GetComponentsInChildren<Transform>().Any(t => t.name == "RankingRow"), Is.False);
        Assert.That(Text(ranking.transform, "Status").text, Is.EqualTo(PSH_Script_Localization.Text("Demo_RankingOffline")));
        ranking.Close();
        foreach (var code in new[] { "ko", "en", "ja" })
        {
            LocaleTestContext.Select(code); yield return null;
            Assert.That(Text(lobby.transform, "Scope").text, Is.EqualTo(PSH_Script_Localization.Text("Demo_Scope")));
            yield return Capture(canvas, "demo-lobby-" + code, lobby.transform);
        }
    }
    [UnityTest] public IEnumerator DemoRankingRendersOnlineAndKeepsBestWhenRefreshFails()
    {
        RequireDemo();
        root = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        BallARetroPersonalRecordStore.Submit("Fixture", 4321, "Classic", 1);
        Assert.That(SteamManager.Initialized, Is.True);
        Steamworks.LeaderboardFixture.Rows = Enumerable.Range(1, 10).Select(i => new Steamworks.LeaderboardEntry_t
        { m_steamIDUser = new Steamworks.CSteamID { Value = (ulong)i }, m_nGlobalRank = i, m_nScore = 11000 - i * 1000 }).ToArray();
        Steamworks.LeaderboardFixture.Steps = Enumerable.Repeat(3, 10).ToArray();
        var ui = PSH_Script_WorldRankingUI.Install(root.transform,
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Galmuri11 SDF.asset"), false);
        ui.Show();
        float timeout = Time.realtimeSinceStartup + 3;
        while (ui.GetComponentsInChildren<TMP_Text>().Count(t => t.name == "Player") < 10 && Time.realtimeSinceStartup < timeout) yield return null;
        Assert.That(ui.GetComponentsInChildren<TMP_Text>().Count(t => t.name == "Player"), Is.EqualTo(10));
        Assert.That(Text(ui.transform, "PersonalBest").text, Is.EqualTo(PSH_Script_Localization.Text("Demo_PersonalBest", 4321)));
        foreach (var code in new[] { "ko", "en", "ja" })
        {
            LocaleTestContext.Select(code); yield return null;
            Assert.That(Text(ui.transform, "Title").text, Is.EqualTo(PSH_Script_Localization.Text("Demo_RankingTitle")));
            yield return Capture(canvas, "demo-ranking-" + code, ui.transform);
        }
        Steamworks.LeaderboardFixture.DownloadFails = true;
        ui.GetComponentsInChildren<Button>().Single(b => b.name == "Refresh").onClick.Invoke();
        timeout = Time.realtimeSinceStartup + 3;
        while (!ui.GetComponentsInChildren<Button>().Single(b => b.name == "Refresh").interactable && Time.realtimeSinceStartup < timeout) yield return null;
        Assert.That(Text(ui.transform, "Status").text, Is.EqualTo(PSH_Script_Localization.Text("Rank_LoadFailed")));
        Assert.That(ui.GetComponentsInChildren<TMP_Text>().Count(t => t.name == "Player"), Is.Zero);
        Assert.That(Text(ui.transform, "PersonalBest").text, Is.EqualTo(PSH_Script_Localization.Text("Demo_PersonalBest", 4321)));
    }
    [UnityTest] public IEnumerator HudReflectsEventsWithoutBlockingInputAndDisappearsAtEnd()
    {
        RequireDemo();
        root = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scoreObject = new GameObject("Score", typeof(KHS_Script_ScoreManager));
        var gamePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Public/Main_Game/UI_Game/Prefabs/Canvas_GameScene.prefab");
        var drawer = Object.Instantiate(gamePrefab.GetComponentInChildren<PSH_Script_QuickChoiceOnOff>(true), root.transform);
        drawer.duration = 0;
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Galmuri11 SDF.asset");
        var hud = BallARetroDemoPresentation.InstallHud(root.transform, font);
        Assert.That(BallARetroDemoPresentation.InstallHud(root.transform, font), Is.SameAs(hud));
        var panel = hud.GetComponentsInChildren<Transform>(true).Single(t => t.name == "BonusStatus");
        Assert.That(panel.gameObject.activeSelf, Is.False);
        var run = new BallARetroDemoRun { IsEnabled = true, CapacitorLevel = 1, Charge = 300, MaxCharge = 600, LastBallLevel = 2, LastBallBonusPercent = 75, LastBallActive = true };
        BallARetroDemoRun.Current = run; BallARetroDemoRun.Notify();
        Assert.That(panel.gameObject.activeSelf, Is.False, "Run updates must not reveal a closed drawer.");
        Assert.That(drawer.TryTogglePanel(), Is.True);
        Assert.That(panel.gameObject.activeSelf, Is.True);
        Assert.That(panel.IsChildOf(drawer.transform), Is.True);
        Assert.That(root.transform.Find("DemoHud"), Is.Null, "No permanent top-of-screen demo banner.");
        Assert.That(Text(hud.transform, "Charge").text, Is.EqualTo(PSH_Script_Localization.Text("Demo_Charge", 300, 600)));
        Assert.That(Text(hud.transform, "LastBall").text, Is.EqualTo(PSH_Script_Localization.Text("Demo_LastBall", 75)));
        Assert.That(hud.GetComponentsInChildren<Graphic>(true).All(g => !g.raycastTarget), Is.True);
        foreach (var code in new[] { "ko", "en", "ja" })
        {
            LocaleTestContext.Select(code); yield return null;
            yield return Capture(canvas, "demo-hud-" + code, hud.transform);
        }
        run.LastBallActive = false; BallARetroDemoRun.Notify();
        Assert.That(Text(hud.transform, "LastBall").text, Is.EqualTo(PSH_Script_Localization.Text("Demo_LastBallReady", 75)));
        Assert.That(drawer.TryTogglePanel(), Is.True);
        Assert.That(panel.gameObject.activeInHierarchy, Is.False);
        Assert.That(drawer.TryTogglePanel(), Is.True);
        scoreObject.GetComponent<KHS_Script_ScoreManager>().HasEnded = true;
        KHS_Script_ScoreManager.EndGame("Ending1");
        Assert.That(panel.gameObject.activeSelf, Is.False);
    }
    [UnityTest] public IEnumerator InactiveDrawerInstallationDoesNotDuplicateVisibilitySubscriptions()
    {
        root = new GameObject("Inactive game UI", typeof(RectTransform), typeof(Canvas));
        root.SetActive(false);
        scoreObject = new GameObject("Score", typeof(KHS_Script_ScoreManager));
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Public/Main_Game/UI_Game/Prefabs/Canvas_GameScene.prefab");
        var drawer = Object.Instantiate(prefab.GetComponentInChildren<PSH_Script_QuickChoiceOnOff>(true), root.transform);
        var hud = BallARetroDemoPresentation.InstallHud(root.transform, null);
        var field = typeof(PSH_Script_QuickChoiceOnOff).GetField("OpenStateChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.That(field.GetValue(drawer), Is.Null);
        for (int i = 0; i < 3; i++)
        {
            root.SetActive(true); yield return null;
            Assert.That(((System.Action<bool>)field.GetValue(drawer)).GetInvocationList().Length, Is.EqualTo(1));
            Assert.That(BallARetroDemoPresentation.InstallHud(root.transform, null), Is.SameAs(hud));
            root.SetActive(false);
            Assert.That(field.GetValue(drawer), Is.Null, "A disabled drawer must release the demo visibility listener.");
        }
    }

    [UnityTest] public IEnumerator ActualResultPrefabKeepsSummaryAndControlsAndShowsSeparateContributions()
    {
        RequireDemo();
        BallARetroDemoRun.Current = new BallARetroDemoRun { IsEnabled = true, ChargePaidTotal = 1200, LastBallBonusTotal = 975 };
        root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Public/Main_Game/UI_Game/Prefabs/Canvas_GameScene.prefab"));
        var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0;
        var settings = root.GetComponent<PSH_Script_SettingController>();
        settings.panelSetting = root.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Panel_Setting").gameObject;
        settings.panelWarning = settings.panelSetting.GetComponentsInChildren<Transform>(true).Single(t => string.Equals(t.name, "Image_Warning", System.StringComparison.OrdinalIgnoreCase)).gameObject;
        var ending = root.GetComponentInChildren<CJS_Script_GameOverUI>(true);
        for (var node = ending.transform; node != null; node = node.parent) node.gameObject.SetActive(true);
        var legacyRank = new GameObject("Legacy ranking text", typeof(RectTransform), typeof(TextMeshProUGUI));
        legacyRank.transform.SetParent(root.transform, false);
        ending.leaderboardList = null; ending.textLeaderboard = legacyRank.GetComponent<TMP_Text>();
        ending.Show(12345); yield return null;
        Assert.That(ending.textLeaderboard.text, Is.Empty, "A local best must never appear as global rank 1 while the list refreshes.");
        yield return null;
        var result = ending.GetComponentsInChildren<BallARetroDemoPresentation>(true).Single(t => t.name == "DemoResult");
        Assert.That(BallARetroDemoPresentation.InstallResult(ending), Is.SameAs(result));
        Assert.That(((RectTransform)result.transform).sizeDelta, Is.EqualTo(new Vector2(924, 174)));
        Assert.That(Text(result.transform, "ChargeContribution").text, Is.EqualTo(PSH_Script_Localization.Text("Demo_ChargeContribution", 1200)));
        Assert.That(Text(result.transform, "LastBallContribution").text, Is.EqualTo(PSH_Script_Localization.Text("Demo_LastBallContribution", 975)));
        Assert.That(Text(result.transform, "PersonalBest").text, Is.EqualTo(PSH_Script_Localization.Text("Demo_PersonalBest", 12345)));
        Assert.That(Text(result.transform, "SubmissionStatus").text, Is.EqualTo(PSH_Script_Localization.Text("Demo_SavedLocally")));
        var retry = ending.GetComponentsInChildren<RectTransform>(true).Single(t => t.name == "Button_Retry_bg");
        var lobby = ending.GetComponentsInChildren<RectTransform>(true).Single(t => t.name == "Button_Lobby");
        Canvas.ForceUpdateCanvases();
        Assert.That(retry.gameObject.activeInHierarchy && lobby.gameObject.activeInHierarchy, Is.True);
        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(result.transform, retry);
        Assert.That(bounds.max.y, Is.LessThan(-87), "Retry must remain below contribution row.");
        foreach (var code in new[] { "ko", "en", "ja" })
        {
            LocaleTestContext.Select(code); yield return null;
            yield return Capture(canvas, "demo-result-" + code, result.transform);
        }
    }

    [UnityTest] public IEnumerator NewBaseAndUpgradedCardsFitActualCardPrefabInEveryLanguage()
    {
        RequireDemo();
        root = new GameObject("Demo cards", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Public/Main_Game/UI_Game/Prefabs/Button_Choice.prefab");
        var icons = new[] { Resources.Load<Sprite>("Demo/Choice_Capacitor"), Resources.Load<Sprite>("Demo/Choice_LastBall") };
        Assert.That(icons.All(s => s != null), Is.True);
        int choices = 0;
        RectTransform previousPage = null;
        foreach (var code in new[] { "ko", "en", "ja" })
        {
            LocaleTestContext.Select(code); yield return null;
            foreach (bool upgraded in new[] { false, true })
            {
                // Each variant receives its own real prefab, just as a new choice
                // screen does. Keep old TMP fallback submeshes alive offscreen.
                if (previousPage != null) previousPage.anchoredPosition = new Vector2(30000, 0);
                var page = (RectTransform)new GameObject("Card page", typeof(RectTransform)).transform;
                page.SetParent(root.transform, false); page.anchorMin = page.anchorMax = new Vector2(.5f, .5f);
                page.sizeDelta = new Vector2(1920, 1080);
                var cards = new[] { Object.Instantiate(prefab, page).GetComponent<CJS_Script_ChoiceCard>(), Object.Instantiate(prefab, page).GetComponent<CJS_Script_ChoiceCard>() };
                for (int i = 0; i < cards.Length; i++)
                {
                    var rect = (RectTransform)cards[i].transform; rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                    rect.anchoredPosition = new Vector2(i == 0 ? -350 : 350, 0); rect.localScale = Vector3.one;
                }
                cards[0].Bind(new CJS_ChoiceData { localizationKey = upgraded ? "Demo_CapacitorUpgrade" : "Demo_Capacitor", icon = icons[0], rarity = "Rare", funcIdx = 9 }, _ => choices++);
                cards[1].Bind(new CJS_ChoiceData { localizationKey = upgraded ? "Demo_LastBallUpgrade" : "Demo_LastBall", icon = icons[1], rarity = "Rare", funcIdx = 10 }, _ => choices++);
                yield return null;
                yield return Capture(canvas, "demo-cards-" + code + (upgraded ? "-upgraded" : "-base"), page);
                previousPage = page;
            }
        }
        Assert.That(choices, Is.Zero, "Rendering and locale changes must never select an upgrade.");
    }
#endif

    private IEnumerator Capture(Canvas canvas, string name, Transform inspected)
    {
        // Keep one render camera/target for the whole test. Repeatedly switching
        // Canvas render modes invalidates otherwise unchanged TMP submeshes.
        if (captureCamera == null)
        {
            captureCamera = new GameObject("Demo capture", typeof(Camera)).GetComponent<Camera>();
            captureCamera.transform.position = new Vector3(0, 0, -100);
            captureCamera.clearFlags = CameraClearFlags.SolidColor; captureCamera.backgroundColor = new Color(.025f, .035f, .055f);
            captureCanvas = canvas; originalMode = canvas.renderMode; originalCamera = canvas.worldCamera; originalPlane = canvas.planeDistance;
            captureTarget = RenderTexture.GetTemporary(1920, 1080, 24);
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = captureCamera; canvas.planeDistance = 100;
            captureCamera.targetTexture = captureTarget;
        }
        yield return null; Canvas.ForceUpdateCanvases();
        foreach (var graphic in canvas.GetComponentsInChildren<Graphic>()) graphic.SetAllDirty();
        foreach (var label in canvas.GetComponentsInChildren<TMP_Text>()) label.ForceMeshUpdate();
        yield return null; Canvas.ForceUpdateCanvases();
        foreach (var label in inspected.GetComponentsInChildren<TMP_Text>())
        {
            Assert.That(label.text, Does.Not.StartWith("Demo_"), name + "/" + label.name);
            Assert.That(label.isTextOverflowing, Is.False, name + "/" + label.name);
        }
        captureCamera.Render(); var oldTarget = RenderTexture.active; RenderTexture.active = captureTarget;
        try
        {
            var pixels = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); pixels.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "..", name + ".png"), pixels.EncodeToPNG());
            Object.DestroyImmediate(pixels);
        }
        finally { RenderTexture.active = oldTarget; }
    }
}
