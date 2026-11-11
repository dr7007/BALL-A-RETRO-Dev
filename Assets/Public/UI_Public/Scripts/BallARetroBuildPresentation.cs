using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

// Full-game status stays in the existing Tab drawer. No input, camera or pause ownership.
public sealed class BallARetroBuildPresentation : MonoBehaviour
{
    private TMP_Text label;
    private PSH_Script_QuickChoiceOnOff drawer;

    public static void Install(Transform owner, TMP_FontAsset font)
    {
        if (BallARetroPlatform.IsDemoBuild || owner.gameObject.scene.name.Contains("Tutorial")) return;
        var drawer = owner.GetComponentInChildren<PSH_Script_QuickChoiceOnOff>(true);
        if (drawer == null || drawer.StatusHost == null || drawer.StatusHost.Find("BuildStatus") != null) return;
        var host = drawer.StatusHost;
        var scroll = host.GetComponentInChildren<ScrollRect>(true);
        if (scroll != null)
        {
            var rect = (RectTransform)scroll.transform;
            rect.sizeDelta -= new Vector2(0, 140);
            rect.anchoredPosition += new Vector2(0, 70);
        }
        var go = new GameObject("BuildStatus", typeof(RectTransform));
        go.layer = 5;
        var root = (RectTransform)go.transform;
        root.SetParent(host, false);
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, 0);
        root.anchoredPosition = new Vector2(0, 24); root.sizeDelta = new Vector2(384, 128);
        var label = go.AddComponent<TextMeshProUGUI>();
        label.font = font != null ? font : TMP_Settings.defaultFontAsset;
        label.fontSize = 21; label.enableAutoSizing = true; label.fontSizeMin = 16; label.fontSizeMax = 21;
        label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        label.color = new Color32(38, 255, 179, 255);
        var ui = go.AddComponent<BallARetroBuildPresentation>();
        ui.label = label; ui.drawer = drawer;
        drawer.OpenStateChanged += ui.OnDrawer;
        ui.Refresh();
    }
    private void OnEnable()
    {
        BallARetroRunBuild.Changed += Refresh;
        KHS_Script_ScoreManager.UILateUpdate += Refresh;
        LocalizationSettings.SelectedLocaleChanged += OnLocale;
        Refresh();
    }
    private void OnDisable()
    {
        BallARetroRunBuild.Changed -= Refresh;
        KHS_Script_ScoreManager.UILateUpdate -= Refresh;
        LocalizationSettings.SelectedLocaleChanged -= OnLocale;
    }
    private void OnDestroy() { if (drawer != null) drawer.OpenStateChanged -= OnDrawer; }
    private void OnDrawer(bool _) => Refresh();
    private void OnLocale(Locale _) => Refresh();
    private void Refresh()
    {
        if (label == null) return;
        var run = BallARetroRunBuild.Current;
        if (drawer == null || !drawer.IsOpen || run == null || !run.IsEnabled) { label.text = ""; return; }
        label.text = PSH_Script_Localization.Text("Build_Status", run.Charge, run.MaxCharge,
            run.ChargePerHit, run.LastBallActive ? run.LastBallBonusPercent : 0);
    }
}
