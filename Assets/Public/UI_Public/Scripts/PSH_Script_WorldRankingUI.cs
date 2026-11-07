using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

/// <summary>Steam-only lobby entry and global top-ten panel.</summary>
public sealed class PSH_Script_WorldRankingUI : MonoBehaviour
{
    private TMP_FontAsset font;
    private GameObject panel;
    private TMP_Text status;
    private Transform rows;
    private Button refresh;
    private string statusKey = "Rank_Loading";
    private int requestVersion;

    public static PSH_Script_WorldRankingUI Install(Transform lobby, TMP_FontAsset font)
    {
        if (!BallARetroPlatform.IsSteamBuild) return null;
        var root = Rect("WorldRanking", lobby, Vector2.zero, Vector2.zero);
        Stretch(root);
        var ui = root.gameObject.AddComponent<PSH_Script_WorldRankingUI>();
        ui.font = font != null ? font : TMP_Settings.defaultFontAsset;
        ui.Build();
        return ui;
    }

    private void OnEnable() => LocalizationSettings.SelectedLocaleChanged += RefreshLanguage;
    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= RefreshLanguage;
        requestVersion++;
    }
    private void RefreshLanguage(Locale _)
    {
        if (status != null) status.text = PSH_Script_Localization.Text(statusKey);
    }
    private void SetStatus(string key) { statusKey = key; RefreshLanguage(null); }

    private void Build()
    {
        var open = Button("OpenWorldRanking", transform, "Rank_World", Vector2.zero, new Vector2(300, 64), Show);
        var anchor = (RectTransform)open.transform;
        anchor.anchorMin = anchor.anchorMax = anchor.pivot = new Vector2(1, 0);
        anchor.anchoredPosition = new Vector2(-32, 32);

        var backdrop = Rect("WorldRankingPanel", transform, Vector2.zero, Vector2.zero);
        Stretch(backdrop);
        panel = backdrop.gameObject;
        panel.AddComponent<Image>().color = new Color(.01f, .02f, .04f, .94f);
        panel.AddComponent<PSH_Script_ModalCanvas>().SetSortingOrder(80);
        var card = Rect("Card", backdrop, Vector2.zero, new Vector2(860, 760));
        card.gameObject.AddComponent<Image>().color = new Color(.035f, .05f, .08f, 1);
        Border(card.gameObject);
        Label("Title", card, "Rank_World", new Vector2(0, 315), new Vector2(720, 64), 36);
        Label("Rank", card, "Rank_Position", new Vector2(-330, 245), new Vector2(100, 38), 25);
        Label("Name", card, "Rank_Player", new Vector2(-30, 245), new Vector2(440, 38), 25);
        Label("Score", card, "Rank_Score", new Vector2(280, 245), new Vector2(180, 38), 25);
        rows = Rect("Rows", card, Vector2.zero, new Vector2(760, 420));
        status = Label("Status", card, "", new Vector2(0, -245), new Vector2(760, 60), 23);
        status.enableAutoSizing = true;
        status.fontSizeMin = 18;
        status.fontSizeMax = 23;
        status.textWrappingMode = TextWrappingModes.Normal;
        refresh = Button("Refresh", card, "Rank_Refresh", new Vector2(-185, -325), new Vector2(300, 60), Refresh);
        Button("Close", card, "Button_Close", new Vector2(185, -325), new Vector2(300, 60), Close);
        panel.SetActive(false);
    }

    public void Show()
    {
        if (!BallARetroPlatform.IsSteamBuild || panel == null) return;
        panel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Refresh();
    }
    public void Close()
    {
        requestVersion++;
        if (panel) panel.SetActive(false);
    }
    private void Refresh()
    {
        int request = ++requestVersion;
        ClearRows();
        SetStatus("Rank_Loading");
        refresh.interactable = false;
        CJS_Script_PinballRankingService.Instance.FetchLeaderboard("Classic", 1,
            result =>
            {
                if (!this || request != requestVersion || !panel.activeInHierarchy) return;
                refresh.interactable = true;
                DrawRows(result.top10 ?? Array.Empty<ScoreRow>());
            },
            error =>
            {
                if (!this || request != requestVersion || !panel.activeInHierarchy) return;
                refresh.interactable = true;
                SetStatus(SteamManager.TryGetLoggedInPlayer(out _, out _) ? "Rank_LoadFailed" : "Nickname_Steam");
            });
    }
    private void ClearRows()
    {
        foreach (Transform row in rows) { row.gameObject.SetActive(false); Destroy(row.gameObject); }
    }
    private void DrawRows(ScoreRow[] data)
    {
        SetStatus(data.Length == 0 ? "Rank_Empty" : "Rank_TopTen");
        for (int i = 0; i < Mathf.Min(10, data.Length); i++)
        {
            float y = 195 - i * 40;
            var row = data[i];
            var rank = Label("Position", rows, "", new Vector2(-330, y), new Vector2(100, 36), 24);
            rank.text = (row.rank > 0 ? row.rank : i + 1).ToString();
            var name = Label("Player", rows, "", new Vector2(-30, y), new Vector2(440, 36), 24);
            name.text = row.nickname;
            name.richText = false;
            name.alignment = TextAlignmentOptions.MidlineLeft;
            name.overflowMode = TextOverflowModes.Ellipsis;
            var score = Label("Score", rows, "", new Vector2(280, y), new Vector2(180, 36), 24);
            score.text = row.score.ToString("N0");
            score.alignment = TextAlignmentOptions.MidlineRight;
        }
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private TMP_Text Label(string name, Transform parent, string key, Vector2 position, Vector2 size, float fontSize)
    {
        var label = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = fontSize;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.color = new Color32(190, 242, 176, 255);
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        if (!string.IsNullOrEmpty(key)) label.gameObject.AddComponent<PSH_Script_LocalizedText>().SetKey(key);
        else label.text = "";
        return label;
    }
    private Button Button(string name, Transform parent, string key, Vector2 position, Vector2 size, UnityAction action)
    {
        var rect = Rect(name, parent, position, size);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(.10f, .12f, .18f, 1);
        Border(rect.gameObject);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        Label("Label", rect, key, Vector2.zero, size - new Vector2(20, 8), 28);
        rect.gameObject.AddComponent<PSH_Script_BaseButtonHover>();
        return button;
    }
    private static void Border(GameObject go)
    {
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(.38f, .12f, .75f, 1);
        outline.effectDistance = new Vector2(2, -2);
    }
}
