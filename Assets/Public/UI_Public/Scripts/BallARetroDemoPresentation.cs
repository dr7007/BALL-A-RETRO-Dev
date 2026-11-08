using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

/// <summary>Read-only demo UI. Run rules and score ownership stay in DemoRun.</summary>
public sealed class BallARetroDemoPresentation : MonoBehaviour
{
    private enum View { Lobby, Hud, Result }
    private View view;
    private TMP_FontAsset font;
    private TMP_Text charge, lastBall, personalBest, submissionStatus;
    private RectTransform safeArea;
    private GameObject content;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private string recordMode = "Classic";
    private int recordLevel = 1;
    private bool ready;
    private bool savedLocally;

    private static readonly Color Mint = new Color32(38, 255, 179, 255);
    private static readonly Color Amber = new Color32(255, 188, 85, 255);

    public static bool CanInstall(Transform owner)
    {
        if (!BallARetroPlatform.IsDemoBuild || owner == null) return false;
        return (owner.gameObject.scene.name ?? string.Empty).IndexOf("Tutorial", StringComparison.OrdinalIgnoreCase) < 0;
    }

    public static BallARetroDemoPresentation InstallLobby(Transform owner, TMP_FontAsset font)
    {
        if (!CanInstall(owner)) return null;
        var existing = owner.Find("DemoLobby");
        if (existing != null) return existing.GetComponent<BallARetroDemoPresentation>();
        var ui = Create(owner, font, View.Lobby, "DemoLobby");
        ui.safeArea = ui.NewRect("SafeArea", ui.transform, Vector2.zero, Vector2.zero);
        ui.UpdateSafeArea();
        var panel = ui.Panel("DemoInfo", ui.safeArea, new Vector2(720, 156));
        panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.zero;
        panel.anchoredPosition = new Vector2(32, 32);
        ui.Label("Badge", panel, "Demo_Badge", new Vector2(0, 53), new Vector2(680, 28), 24, Mint);
        ui.Label("Scope", panel, "Demo_Scope", new Vector2(0, 19), new Vector2(680, 32), 25, Color.white);
        ui.Wishlist(panel, new Vector2(0, -37), new Vector2(540, 44));
        ui.Complete();
        return ui;
    }

    public static BallARetroDemoPresentation InstallHud(Transform owner, TMP_FontAsset font)
    {
        if (!CanInstall(owner)) return null;
        var existing = owner.Find("DemoHud");
        if (existing != null) return existing.GetComponent<BallARetroDemoPresentation>();
        var ui = Create(owner, font, View.Hud, "DemoHud");
        // Place with the regular HUD, below subsequently drawn choice/settings/results.
        ui.transform.SetSiblingIndex(Mathf.Min(1, owner.childCount - 1));
        ui.safeArea = ui.NewRect("SafeArea", ui.transform, Vector2.zero, Vector2.zero);
        ui.UpdateSafeArea();
        var panel = ui.Panel("BonusStatus", ui.safeArea, new Vector2(816, 70));
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, 1);
        panel.anchoredPosition = new Vector2(0, -62);
        ui.content = panel.gameObject;
        ui.charge = ui.Label("Charge", panel, null, new Vector2(0, 17), new Vector2(784, 28), 23, Mint);
        ui.lastBall = ui.Label("LastBall", panel, null, new Vector2(0, -17), new Vector2(784, 28), 23, Color.white);
        ui.Complete();
        return ui;
    }

    public static BallARetroDemoPresentation InstallResult(CJS_Script_GameOverUI owner)
    {
        if (owner == null || !CanInstall(owner.transform)) return null;
        foreach (var existing in owner.GetComponentsInChildren<BallARetroDemoPresentation>(true))
            if (existing.view == View.Result) return existing;
        // Reuse the old online rank row and reserve 28 extra pixels for an honest
        // offline-save status. The separate 816x90 run summary stays intact.
        RectTransform rankRow = null;
        foreach (var rect in owner.GetComponentsInChildren<RectTransform>(true))
            if (rect.name == "Image_Rank_bg") { rankRow = rect; break; }
        if (rankRow == null) return null;
        var ui = Create(owner.transform, owner.textFinalScore != null ? owner.textFinalScore.font : null,
            View.Result, "DemoResult");
        ui.recordMode = owner.gameMode;
        ui.recordLevel = owner.level;
        var root = (RectTransform)ui.transform;
        root.SetParent(rankRow.parent, false);
        root.anchorMin = rankRow.anchorMin; root.anchorMax = rankRow.anchorMax;
        root.pivot = rankRow.pivot; root.anchoredPosition = rankRow.anchoredPosition;
        root.sizeDelta = rankRow.sizeDelta + new Vector2(0, 28);
        root.SetSiblingIndex(rankRow.GetSiblingIndex() + 1);
        rankRow.gameObject.SetActive(false);
        var panel = ui.Panel("ContributionPanel", root, Vector2.zero);
        Stretch(panel);
        ui.Label("Heading", panel, "Demo_Contributions", new Vector2(60, 70), new Vector2(740, 24), 21, Mint);
        ui.charge = ui.Label("ChargeContribution", panel, null, new Vector2(-135, 41), new Vector2(370, 28), 23, Color.white);
        ui.lastBall = ui.Label("LastBallContribution", panel, null, new Vector2(255, 41), new Vector2(370, 28), 23, Color.white);
        ui.personalBest = ui.Label("PersonalBest", panel, null, new Vector2(60, 10), new Vector2(740, 28), 23, Amber);
        ui.submissionStatus = ui.Label("SubmissionStatus", panel, null, new Vector2(60, -18), new Vector2(740, 24), 17, Color.white);
        ui.submissionStatus.fontSizeMin = 14;
        ui.Wishlist(panel, new Vector2(60, -58), new Vector2(560, 38));
        // Keep the authored ranking icon and its serialized click callback.
        var rankButton = rankRow.Find("Image_RankOpen") as RectTransform;
        if (rankButton != null)
        {
            rankButton.SetParent(root, false);
            rankButton.anchorMin = rankButton.anchorMax = rankButton.pivot = new Vector2(.5f, .5f);
            rankButton.anchoredPosition = new Vector2(-400, 0);
            rankButton.sizeDelta = new Vector2(98, 104); rankButton.localScale = Vector3.one;
            rankButton.gameObject.SetActive(true);
        }
        ui.Complete();
        return ui;
    }

    private static BallARetroDemoPresentation Create(Transform owner, TMP_FontAsset font, View view, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(owner, false);
        Stretch((RectTransform)go.transform);
        var ui = go.AddComponent<BallARetroDemoPresentation>();
        ui.font = font != null ? font : TMP_Settings.defaultFontAsset;
        ui.view = view;
        return ui;
    }

    private void Complete() { ready = true; Refresh(); }
    private void OnEnable()
    {
        BallARetroDemoRun.Changed += Refresh;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        KHS_Script_ScoreManager.OnGameOver += Refresh;
        KHS_Script_ScoreManager.OnGameClear += Refresh;
        Refresh();
    }
    private void OnDisable()
    {
        BallARetroDemoRun.Changed -= Refresh;
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        KHS_Script_ScoreManager.OnGameOver -= Refresh;
        KHS_Script_ScoreManager.OnGameClear -= Refresh;
    }
    private void OnLocaleChanged(Locale _) => Refresh();
    private void Update()
    {
        // Screen/safe-area structs are allocation-free; no text/material rebuild per frame.
        if (safeArea != null && (lastSafeArea != Screen.safeArea || lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height))
            UpdateSafeArea();
    }
    private void UpdateSafeArea()
    {
        if (safeArea == null || Screen.width <= 0 || Screen.height <= 0) return;
        lastSafeArea = Screen.safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        safeArea.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
        safeArea.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
        safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
    }

    public void Refresh()
    {
        if (!ready) return;
        var run = BallARetroDemoRun.Current;
        if (view == View.Hud)
        {
            var score = KHS_Script_ScoreManager.Instance;
            bool visible = run != null && run.IsEnabled && score != null && !score.HasEnded;
            content.SetActive(visible);
            if (!visible) return;
            charge.text = run.CapacitorLevel > 0 ? Text("Demo_Charge", run.Charge, run.MaxCharge) : Text("Demo_ChargeLocked");
            lastBall.text = run.LastBallLevel > 0
                ? Text(run.LastBallActive ? "Demo_LastBall" : "Demo_LastBallReady", run.LastBallBonusPercent)
                : Text("Demo_LastBallLocked");
            lastBall.color = run.LastBallActive ? Amber : Color.white;
        }
        else if (view == View.Result)
        {
            charge.text = Text("Demo_ChargeContribution", run != null && run.IsEnabled ? run.ChargePaidTotal : 0);
            lastBall.text = Text("Demo_LastBallContribution", run != null && run.IsEnabled ? run.LastBallBonusTotal : 0);
            personalBest.text = PersonalBestText(recordMode, recordLevel);
            submissionStatus.text = savedLocally ? Text("Demo_SavedLocally") : string.Empty;
        }
    }

    public void SetSubmissionState(bool localOnly) { savedLocally = localOnly; Refresh(); }

    public static string PersonalBestText(string mode = "Classic", int level = 1)
    {
        var rows = BallARetroPersonalRecordStore.Read(string.Empty, mode, level);
        return rows.Length > 0 ? Text("Demo_PersonalBest", rows[0].score) : Text("Demo_NoPersonalBest");
    }

    public static void OpenWishlist()
    {
        if (!BallARetroPlatform.IsDemoBuild) return;
        string url = BallARetroSteamSettings.FullGameStoreUrl;
        if (!string.IsNullOrEmpty(url)) Application.OpenURL(url);
    }

    private void Wishlist(Transform parent, Vector2 position, Vector2 size)
    {
        var rect = NewRect("Wishlist", parent, position, size);
        var graphic = rect.gameObject.AddComponent<Image>();
        graphic.color = new Color(.10f, .12f, .18f, 1);
        var outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = Mint; outline.effectDistance = new Vector2(1, -1);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = graphic;
        button.interactable = !string.IsNullOrEmpty(BallARetroSteamSettings.FullGameStoreUrl);
        button.onClick.AddListener(OpenWishlist);
        Label("Label", rect, "Demo_Wishlist", Vector2.zero, size - new Vector2(24, 8), 22, Mint);
        rect.gameObject.AddComponent<PSH_Script_BaseButtonHover>();
    }
    private RectTransform Panel(string name, Transform parent, Vector2 size)
    {
        var rect = NewRect(name, parent, Vector2.zero, size);
        var graphic = rect.gameObject.AddComponent<Image>();
        graphic.color = new Color(.02f, .04f, .09f, .94f);
        graphic.raycastTarget = false;
        return rect;
    }
    private RectTransform NewRect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.gameObject.layer = 5;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private TMP_Text Label(string name, Transform parent, string key, Vector2 position, Vector2 size, float pointSize, Color color)
    {
        var label = NewRect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font; label.fontSize = pointSize;
        label.enableAutoSizing = true; label.fontSizeMin = 18; label.fontSizeMax = pointSize;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.alignment = TextAlignmentOptions.Center; label.color = color; label.raycastTarget = false;
        if (!string.IsNullOrEmpty(key)) label.gameObject.AddComponent<PSH_Script_LocalizedText>().SetKey(key);
        return label;
    }
    private static string Text(string key, params object[] values) => PSH_Script_Localization.Text(key, values);
}
