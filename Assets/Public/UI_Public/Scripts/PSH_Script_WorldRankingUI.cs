using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

/// <summary>Shared lobby/result ranking view. Only Steam builds request online scores.</summary>
public sealed class PSH_Script_WorldRankingUI : MonoBehaviour
{
    private TMP_FontAsset font;
    private GameObject panel;
    private TMP_Text status;
    private Transform rows;
    private Button refresh;
    private string statusKey = "Rank_Loading";
    private int requestVersion;
    public event Action Closed;

    public static PSH_Script_WorldRankingUI Install(Transform lobby, TMP_FontAsset font, bool showEntry = true, Sprite entryIcon = null)
    {
        var root = Rect("WorldRanking", lobby, Vector2.zero, Vector2.zero);
        Stretch(root);
        var ui = root.gameObject.AddComponent<PSH_Script_WorldRankingUI>();
        ui.font = font != null ? font : TMP_Settings.defaultFontAsset;
        ui.Build(showEntry, entryIcon);
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

    private void Build(bool showEntry, Sprite entryIcon)
    {
        if (showEntry)
        {
            var open = Button("OpenWorldRanking", transform, "", Vector2.zero, new Vector2(110, 116), Show);
            var graphic = open.GetComponent<Image>();
            graphic.sprite = entryIcon;
            graphic.preserveAspect = true;
            graphic.color = Color.white;
            open.GetComponent<Outline>().enabled = false;
            if (entryIcon == null) open.GetComponentInChildren<TMP_Text>().text = "RANK";
            var anchor = (RectTransform)open.transform;
            anchor.anchorMin = anchor.anchorMax = anchor.pivot = new Vector2(1, 0);
            anchor.anchoredPosition = new Vector2(-32, 32);
        }

        var backdrop = Rect("WorldRankingPanel", transform, Vector2.zero, Vector2.zero);
        Stretch(backdrop);
        panel = backdrop.gameObject;
        panel.AddComponent<Image>().color = Color.black;
        panel.AddComponent<PSH_Script_ModalCanvas>().SetSortingOrder(80);
        var card = Rect("Card", backdrop, Vector2.zero, new Vector2(1600, 900));
        Frame(card, new Vector2(1600, 900));
        Frame(card, new Vector2(1580, 880));
        Literal("Title", card, "SCORE  RANKING", new Vector2(0, 350), new Vector2(1100, 64), 44, Color.magenta);
        Literal("Rank", card, "RANK", new Vector2(-520, 260), new Vector2(180, 48), 34, Color.white);
        Literal("Name", card, "NAME", new Vector2(-190, 260), new Vector2(430, 48), 34, Color.white);
        Literal("Step", card, "STEP", new Vector2(130, 260), new Vector2(160, 48), 34, Color.white);
        Literal("Score", card, "SCORE", new Vector2(460, 260), new Vector2(360, 48), 34, Color.white);
        rows = Rect("Rows", card, Vector2.zero, new Vector2(1300, 510));
        Literal("Logo", card, "BALL A-RETRO", new Vector2(0, -325), new Vector2(700, 48), 28, new Color32(0, 240, 104, 255));
        status = Label("Status", card, "", new Vector2(0, -390), new Vector2(800, 70), 23);
        status.enableAutoSizing = true;
        status.fontSizeMin = 18;
        status.fontSizeMax = 23;
        status.textWrappingMode = TextWrappingModes.Normal;
        refresh = Button("Refresh", card, "", new Vector2(-615, -385), new Vector2(220, 60), Refresh);
        refresh.GetComponentInChildren<TMP_Text>().text = "REFRESH";
        Button("Close", card, "", new Vector2(615, -385), new Vector2(220, 60), Close).GetComponentInChildren<TMP_Text>().text = "CLOSE";
        panel.SetActive(false);
    }

    public void Show()
    {
        if (panel == null) return;
        panel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Refresh();
    }
    public void Close()
    {
        requestVersion++;
        if (panel) panel.SetActive(false);
        Closed?.Invoke();
    }
    private void Refresh()
    {
        int request = ++requestVersion;
        ClearRows();
        if (!BallARetroPlatform.IsSteamBuild)
        {
            SetStatus("Rank_SteamOnly");
            refresh.interactable = false;
            return;
        }
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
            float y = 195 - i * 46;
            var row = data[i];
            int position = row.rank > 0 ? row.rank : i + 1;
            var color = RankColor(position);
            Literal("Position", rows, Ordinal(position), new Vector2(-520, y), new Vector2(180, 42), 32, color);
            var name = Literal("Player", rows, "", new Vector2(-190, y), new Vector2(430, 42), 32, color);
            name.text = row.nickname;
            name.richText = false;
            name.overflowMode = TextOverflowModes.Ellipsis;
            Literal("Step", rows, row.step > 0 ? row.step.ToString() : "—", new Vector2(130, y), new Vector2(160, 42), 32, color);
            Literal("Score", rows, row.score.ToString(), new Vector2(460, y), new Vector2(360, 42), 32, color);
        }
    }

    private static string Ordinal(int rank) => rank + (rank % 100 >= 11 && rank % 100 <= 13 ? "TH" : rank % 10 == 1 ? "ST" : rank % 10 == 2 ? "ND" : rank % 10 == 3 ? "RD" : "TH");
    private static Color RankColor(int rank) => rank switch
    {
        1 => Color.red, 2 => new Color32(255, 255, 160, 255), 3 => Color.magenta,
        4 => new Color32(0, 220, 110, 255), 5 => new Color32(255, 90, 166, 255),
        6 => new Color32(255, 190, 0, 255), 7 => Color.green, _ => Color.gray
    };

    private TMP_Text Literal(string name, Transform parent, string text, Vector2 position, Vector2 size, float fontSize, Color color)
    {
        var label = Label(name, parent, "", position, size, fontSize);
        label.text = text; label.color = color;
        return label;
    }
    private static void Frame(Transform parent, Vector2 size)
    {
        for (int i = 0; i < 4; i++)
        {
            bool horizontal = i < 2;
            var position = horizontal ? new Vector2(0, (i == 0 ? -1 : 1) * size.y / 2) : new Vector2((i == 2 ? -1 : 1) * size.x / 2, 0);
            var edge = Rect("Border", parent, position, horizontal ? new Vector2(size.x, 3) : new Vector2(3, size.y));
            var graphic = edge.gameObject.AddComponent<Image>();
            graphic.color = new Color32(54, 121, 91, 255); graphic.raycastTarget = false;
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
