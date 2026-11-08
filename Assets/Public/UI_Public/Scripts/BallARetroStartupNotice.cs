using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>A static, localized notice before the first interactive menu of each launch.</summary>
[DefaultExecutionOrder(-10000)]
public sealed class BallARetroStartupNotice : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset font;
    private static BallARetroStartupNotice instance;
    private TMP_Text title, body, hint, continueText, reduceText, quitText;
    private Button continueButton, reduceButton, quitButton;
    private bool previousAudioPause;
    private bool ready;
    public static bool IsVisible => instance != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ShowAtStartup()
    {
        if (instance != null) return;
        var prefab = Resources.Load<GameObject>("Release/StartupNotice");
        if (prefab == null)
        {
            Debug.LogError("[Startup] Missing localized startup notice prefab.");
            return;
        }
        Instantiate(prefab);
    }

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
        previousAudioPause = AudioListener.pause;
        AudioListener.pause = true;
        Build();
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += Refresh;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= Refresh;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => FocusNotice();
    private void OnDestroy()
    {
        if (instance != this) return;
        AudioListener.pause = previousAudioPause;
        instance = null;
    }

    private IEnumerator Start()
    {
        // Reuse the game's saved-locale -> OS-locale -> English startup selectors.
        // The opaque backdrop is already present while localization initializes.
        yield return LocalizationSettings.InitializationOperation;
        Refresh(null);
        yield return new WaitForSecondsRealtime(2);
        ready = true;
        continueButton.interactable = reduceButton.interactable = quitButton.interactable = true;
        FocusNotice();
        Debug.Log("[Startup] Photosensitivity notice ready: " + PSH_Script_Localization.LanguageCode);
    }

    // Run before EventSystem.Update, including the reading delay. A disabled notice
    // button holds selection so keyboard Submit cannot activate a background menu.
    private void Update() => FocusNotice();
    private void LateUpdate() => FocusNotice();

    private void FocusNotice()
    {
        var events = EventSystem.current;
        if (events == null) return;
        var selected = events.currentSelectedGameObject;
        if (selected == null || !selected.transform.IsChildOf(transform))
            events.SetSelectedGameObject(continueButton.gameObject);
    }

    private void Dismiss(bool reduceEffects)
    {
        if (!ready) return;
        if (reduceEffects) PSH_Script_EffectSettings.SetEnabled(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        Debug.Log("[Startup] Photosensitivity notice acknowledged; reduceEffects=" + reduceEffects);
        Destroy(gameObject);
    }

    private void Quit()
    {
        if (!ready) return;
        Debug.Log("[Startup] Quit requested from photosensitivity notice.");
        Application.Quit();
    }

    private void Refresh(Locale _)
    {
        if (title == null) return;
        title.text = PSH_Script_Localization.Text("Startup_WarningTitle");
        body.text = PSH_Script_Localization.Text("Startup_WarningBody");
        hint.text = PSH_Script_Localization.Text("Startup_WarningHint");
        continueText.text = PSH_Script_Localization.Text("Startup_Continue");
        reduceText.text = PSH_Script_Localization.Text("Startup_ReduceAndContinue");
        quitText.text = PSH_Script_Localization.Text("Startup_Quit");
    }

    private void Build()
    {
        gameObject.layer = 5;
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        gameObject.AddComponent<GraphicRaycaster>();
        var backdrop = Rect("Backdrop", transform, Vector2.zero, Vector2.zero);
        backdrop.anchorMin = Vector2.zero;
        backdrop.anchorMax = Vector2.one;
        backdrop.gameObject.AddComponent<Image>().color = new Color(.015f, .025f, .04f, 1);
        var card = Rect("Notice", transform, Vector2.zero, new Vector2(1360, 850));
        var brand = Label("Brand", card, new Vector2(0, 350), new Vector2(1220, 50), 27);
        brand.text = "BALL-A-RETRO";
        brand.color = new Color(.28f, .8f, .85f, 1);
        title = Label("Title", card, new Vector2(0, 270), new Vector2(1220, 80), 44);
        title.color = new Color(1, .75f, .35f, 1);
        body = Label("Body", card, new Vector2(0, 50), new Vector2(1220, 320), 30);
        body.alignment = TextAlignmentOptions.TopLeft;
        body.paragraphSpacing = 16;
        hint = Label("Hint", card, new Vector2(0, -170), new Vector2(1220, 100), 25);
        hint.color = new Color(.74f, .81f, .87f, 1);
        continueButton = MakeButton("Continue", card, -410, out continueText);
        reduceButton = MakeButton("ReduceAndContinue", card, 0, out reduceText);
        quitButton = MakeButton("Quit", card, 410, out quitText);
        continueButton.onClick.AddListener(() => Dismiss(false));
        reduceButton.onClick.AddListener(() => Dismiss(true));
        quitButton.onClick.AddListener(Quit);
        var buttons = new[] { continueButton, reduceButton, quitButton };
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].interactable = false;
            buttons[i].navigation = new Navigation {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = buttons[(i + 2) % 3], selectOnRight = buttons[(i + 1) % 3],
                selectOnUp = buttons[(i + 2) % 3], selectOnDown = buttons[(i + 1) % 3]
            };
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

    private TMP_Text Label(string name, Transform parent, Vector2 position, Vector2 size, float fontSize)
    {
        var label = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = label.fontSizeMax = fontSize;
        label.fontSizeMin = fontSize * .85f;
        label.enableAutoSizing = true;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(.92f, .94f, .96f, 1);
        label.raycastTarget = false;
        label.text = "";
        return label;
    }

    private Button MakeButton(string name, Transform parent, float x, out TMP_Text label)
    {
        var rect = Rect(name, parent, new Vector2(x, -310), new Vector2(390, 78));
        var image = rect.gameObject.AddComponent<Image>();
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = new Color(.10f, .16f, .21f, 1);
        colors.highlightedColor = colors.selectedColor = new Color(.18f, .35f, .4f, 1);
        colors.pressedColor = new Color(.12f, .25f, .3f, 1);
        colors.disabledColor = new Color(.05f, .08f, .11f, 1);
        colors.fadeDuration = 0;
        button.colors = colors;
        label = Label("Text", rect, Vector2.zero, new Vector2(366, 70), 26);
        return button;
    }
}
