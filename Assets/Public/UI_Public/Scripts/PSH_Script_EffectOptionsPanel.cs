using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using Effect = PSH_Script_EffectSettings.Effect;

/// <summary>Shared effects dialog, opened from the settings prefab in every scene.</summary>
public class PSH_Script_EffectOptionsPanel : MonoBehaviour
{
    private readonly Toggle[] switches = new Toggle[6];
    private readonly TMP_Text[] values = new TMP_Text[6];
    private TMP_Text status;
    private TMP_FontAsset font;

    public static PSH_Script_EffectOptionsPanel Create(TMP_FontAsset font, int sortingOrder)
    {
        var root = new GameObject("EffectOptions", typeof(RectTransform));
        root.layer = 5;
        root.SetActive(false);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0;
        root.AddComponent<GraphicRaycaster>();
        var panel = root.AddComponent<PSH_Script_EffectOptionsPanel>();
        panel.font = font != null ? font : TMP_Settings.defaultFontAsset;
        panel.Build();
        return panel;
    }

    private void OnEnable()
    {
        PSH_Script_EffectSettings.Changed += Refresh;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        Refresh(false);
    }
    private void OnDisable()
    {
        PSH_Script_EffectSettings.Changed -= Refresh;
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }
    private void OnLocaleChanged(Locale _) => Refresh(false);

    private void Build()
    {
        var shade = Rect("Backdrop", transform, Vector2.zero, Vector2.zero);
        shade.anchorMin = Vector2.zero;
        shade.anchorMax = Vector2.one;
        shade.gameObject.AddComponent<Image>().color = new Color(0.015f, 0.02f, 0.04f, .93f);
        var card = Rect("Panel", transform, Vector2.zero, new Vector2(920, 830));
        card.gameObject.AddComponent<Image>().color = new Color(.035f, .05f, .08f, 1);
        Border(card.gameObject);
        Label("Title", card, "Effects_Title", new Vector2(0, 350), new Vector2(820, 58), 36);
        Label("Hint", card, "Effects_Hint",
            new Vector2(0, 302), new Vector2(840, 40), 23);
        Button("DefaultMode", card, "Effects_AllOn", new Vector2(-210, 240), new Vector2(390, 58),
            () => PSH_Script_EffectSettings.SetEnabled(true));
        Button("ReduceEffects", card, "Effects_AllOff", new Vector2(210, 240), new Vector2(390, 58),
            () => PSH_Script_EffectSettings.SetEnabled(false));
        status = Label("Status", card, "", new Vector2(0, 181), new Vector2(820, 36), 24);

        string[] labels = { "Effects_CameraShake", "Effects_Particles", "Effects_Trails", "Effects_Flashes", "Effects_ScreenEffects", "Effects_PostProcessing" };
        for (int i = 0; i < switches.Length; i++)
        {
            var effect = (Effect)i;
            float y = 110 - i * 66;
            var text = Label(effect + "Label", card, labels[i], new Vector2(-100, y), new Vector2(540, 48), 28);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            var rect = Rect(effect.ToString(), card, new Vector2(310, y), new Vector2(160, 48));
            var background = rect.gameObject.AddComponent<Image>();
            var toggle = rect.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = background;
            Colors(toggle);
            Border(rect.gameObject);
            var indicator = Rect("Indicator", rect, new Vector2(-56, 0), new Vector2(12, 24));
            var indicatorImage = indicator.gameObject.AddComponent<Image>();
            indicatorImage.color = new Color(.45f, 1, .6f, 1);
            indicatorImage.raycastTarget = false;
            toggle.graphic = indicatorImage;
            values[i] = Label("Value", rect, "ON", new Vector2(12, 0), new Vector2(110, 44), 26);
            switches[i] = toggle;
            toggle.onValueChanged.AddListener(value => PSH_Script_EffectSettings.Set(effect, value));
            rect.gameObject.AddComponent<PSH_Script_BaseButtonHover>();
        }
        Button("Close", card, "Effects_Back", new Vector2(0, -335), new Vector2(380, 58),
            () => gameObject.SetActive(false));
    }

    private void Refresh(bool _)
    {
        if (status == null) return;
        status.text = PSH_Script_Localization.Text(PSH_Script_EffectSettings.AllEnabled ? "Effects_StatusOn"
            : PSH_Script_EffectSettings.IsEnabled ? "Effects_StatusCustom" : "Effects_StatusOff");
        status.color = new Color(.745f, .949f, .69f, 1);
        for (int i = 0; i < switches.Length; i++)
        {
            bool enabled = PSH_Script_EffectSettings.Get((Effect)i);
            switches[i].SetIsOnWithoutNotify(enabled);
            values[i].text = enabled ? "ON" : "OFF";
        }
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }
    private TMP_Text Label(string name, Transform parent, string text, Vector2 position, Vector2 size, float fontSize)
    {
        var rect = Rect(name, parent, position, size);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = fontSize;
        label.text = text;
        if (!string.IsNullOrEmpty(text) && text != "ON")
            rect.gameObject.AddComponent<PSH_Script_LocalizedText>().SetKey(text);
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }
    private void Button(string name, Transform parent, string text, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        var rect = Rect(name, parent, position, size);
        var image = rect.gameObject.AddComponent<Image>();
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        Colors(button);
        Border(rect.gameObject);
        Label("Text", rect, text, Vector2.zero, size - new Vector2(16, 4), 25);
        button.onClick.AddListener(action);
        rect.gameObject.AddComponent<PSH_Script_BaseButtonHover>();
    }
    private static void Border(GameObject go)
    {
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(.38f, .12f, .75f, 1);
        outline.effectDistance = new Vector2(2, -2);
    }
    private static void Colors(Selectable selectable)
    {
        selectable.transition = Selectable.Transition.ColorTint;
        var colors = selectable.colors;
        colors.normalColor = new Color(.10f, .12f, .18f, 1);
        colors.highlightedColor = new Color(.22f, .30f, .36f, 1);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(.13f, .38f, .33f, 1);
        colors.fadeDuration = .08f;
        selectable.colors = colors;
    }
}
