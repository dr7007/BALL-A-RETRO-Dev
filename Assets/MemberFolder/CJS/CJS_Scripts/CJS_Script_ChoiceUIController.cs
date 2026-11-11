using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.EventSystems;
using PSH;

public class CJS_Script_ChoiceUIController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private MonoBehaviour rollerBehaviour;      // Inspector에 CJS_Script_ChoiceRoller
    private CJS_IChoiceRoller roller;

    [SerializeField] private GameObject panelRoot;               // Panel_Choice
    [SerializeField] private Transform content;                  // 카드가 생성될 Grid 부모
    [SerializeField] private CJS_Script_ChoiceCard cardPrefab;   // 카드 프리팹(루트에 Button)

    [SerializeField] private GameObject gameUICanvasGo;           // 모니터 클릭 편의 - KHS
    [SerializeField] private KHS_Script_CameraManager camManager; // 동일

    [Header("Buttons")]
    [SerializeField] private Button btnReroll;
    [SerializeField] private TMP_Text txtRerollInfo;
    [SerializeField] private Button btnSkip;

    [Header("Settings")]
    [SerializeField, Min(0)] private int maxReroll = 5;

    private readonly List<CJS_Script_ChoiceCard> liveCards = new List<CJS_Script_ChoiceCard>();
    private int usedReroll;
    private bool isOpen;
    private bool busy;
    private bool gameEnded;
    private bool starterQueued;
    private int fullScreenRound = -1;
    private bool fullScreenCompleted;
    private readonly List<int> offerIds = new List<int>(3);
    private List<CJS_ChoiceData> currentOffer;
    private readonly List<Button> entryButtons = new List<Button>();
    private int RerollLimit => BallARetroPlatform.IsDemoBuild ? maxReroll : 3;
    public bool CanOpenFullScreen => !gameEnded && (fullScreenRound != CurrentRound() || !fullScreenCompleted);

    [SerializeField]
    private YJ_Script_BallController ballCon;
    [SerializeField]
    private int pickCount = 0;

    void OnValidate()
    {
        if (rollerBehaviour != null && !(rollerBehaviour is CJS_IChoiceRoller))
            Debug.LogWarning("CJS_Script_ChoiceUIController: rollerBehaviour는 CJS_IChoiceRoller를 구현해야 합니다.", this);
    }

    void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += RefreshLanguage;
        UpdateRerollUI();
        if (btnReroll != null) btnReroll.onClick.AddListener(OnClickReroll);
        if (btnSkip != null) btnSkip.onClick.AddListener(OnClickSkip);
        KHS_Script_ScoreManager.Round_Clear += PickCountBonusFunc;
        KHS_Script_ScoreManager.OnGameClear += OnGameEnded;
        KHS_Script_ScoreManager.OnGameOver += OnGameEnded;
        KHS_Script_UIImgFunc.RoundUIEvt += OnRoundBanner;
        PSH_Script_GameSceneDirector.NoIntroStartEvt += QueueDemoStarter;
    }

    void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= RefreshLanguage;
        if (btnReroll != null) btnReroll.onClick.RemoveListener(OnClickReroll);
        if (btnSkip != null) btnSkip.onClick.RemoveListener(OnClickSkip);
        KHS_Script_ScoreManager.Round_Clear -= PickCountBonusFunc;
        KHS_Script_ScoreManager.OnGameClear -= OnGameEnded;
        KHS_Script_ScoreManager.OnGameOver -= OnGameEnded;
        KHS_Script_UIImgFunc.RoundUIEvt -= OnRoundBanner;
        PSH_Script_GameSceneDirector.NoIntroStartEvt -= QueueDemoStarter;
    }

    void Awake()
    {
        roller = rollerBehaviour as CJS_IChoiceRoller;
        //if (panelRoot != null) panelRoot.SetActive(false);
        usedReroll = 0;
        isOpen = false;
        busy = false;

        if (!BallARetroPlatform.IsDemoBuild)
        {
            foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                    if (button.onClick.GetPersistentTarget(i) == this && button.onClick.GetPersistentMethodName(i) == nameof(ShowChoices))
                    { entryButtons.Add(button); break; }
            // The Japanese action and remaining-count labels need one readable line.
            if (btnReroll != null)
            {
                ((RectTransform)btnReroll.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 320);
                var layout = btnReroll.GetComponent<LayoutElement>() ?? btnReroll.gameObject.AddComponent<LayoutElement>();
                layout.minWidth = layout.preferredWidth = 320;
                foreach (var text in btnReroll.GetComponentsInChildren<TMP_Text>(true))
                    text.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

        SetChoiceButtonsVisible(false);
        UpdateRerollUI();
    }

    private void SetChoiceButtonsVisible(bool visible)
    {
        bool optionalControls = visible && !(BallARetroDemoRun.Current?.IsEnabled ?? false);
        if (btnSkip) btnSkip.gameObject.SetActive(optionalControls);
        if (btnReroll) btnReroll.gameObject.SetActive(optionalControls);
        if (txtRerollInfo) txtRerollInfo.gameObject.SetActive(optionalControls);
    }

    public void ShowChoices()
    {
        if (isOpen || gameEnded) return;
        var demo = BallARetroDemoRun.Current;
        if (demo != null && demo.IsEnabled && !demo.CanChoose(CurrentRound())) return;
        bool fullRun = !BallARetroPlatform.IsDemoBuild;
        bool newScreen = fullScreenRound != CurrentRound();
        if (fullRun && fullScreenRound == CurrentRound() && fullScreenCompleted) return;
        if (roller == null)
        {
            Debug.LogError("CJS_IChoiceRoller가 연결되지 않았습니다.", this);
            return;
        }
        if (cardPrefab == null || content == null)
        {
            Debug.LogError("cardPrefab 또는 content가 연결되지 않았습니다.", this);
            return;
        }

        isOpen = true;
        if (!fullRun || fullScreenRound != CurrentRound())
        {
            usedReroll = 0;
            if (fullRun)
            {
                fullScreenRound = CurrentRound();
                fullScreenCompleted = false;
                pickCount = Mathf.Max(1, ballCon != null ? ballCon.GetBallCount() : 1);
            }
        }
        if (demo != null && demo.IsEnabled && gameUICanvasGo) gameUICanvasGo.SetActive(false);

        if (panelRoot != null) panelRoot.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (demo != null && demo.IsEnabled) pickCount = 1;
        SetChoiceButtonsVisible(true);
        UpdateRerollUI();
        RefreshCards(reuse: fullRun && !newScreen);

        if (liveCards.Count > 0)
        {
            Button first = liveCards[0].GetComponentInChildren<Button>();
            if (first != null) EventSystem.current?.SetSelectedGameObject(first.gameObject);
        }
    }

    public void Hide()
    {
        if (!isOpen) return;

        isOpen = false;

        ClearCards();

        SetChoiceButtonsVisible(false);
        //if (panelRoot != null) panelRoot.SetActive(false);

        UpdateRerollUI();
        if (!gameEnded) PSH_Script_GameplayInput.Resume();
    }

    private void OnGameEnded() => gameEnded = true;

    private void RefreshCards(bool reroll = false, bool reuse = false)
    {
        ClearCards();

        List<CJS_ChoiceData> picks = reuse && currentOffer != null ? currentOffer
            : reroll && roller is CJS_IChoiceReroller reroller
                ? reroller.Reroll3(offerIds, out _) : roller.Roll3(out _);
        currentOffer = picks;
        offerIds.Clear();

        for (int i = 0; i < picks.Count; i++)
        {
            var card = Instantiate(cardPrefab, content);
            // A conditional draw probability is not whole-offer inclusion probability.
            card.BindWithChance(picks[i], picked => { if (liveCards.Contains(card)) OnPickCard(picked); }, null);
            liveCards.Add(card);
            offerIds.Add(picks[i].funcIdx);
        }
        if (liveCards.Count > 0)
            EventSystem.current?.SetSelectedGameObject(liveCards[0].GetComponent<Button>()?.gameObject);
        if (picks.Count == 0 && !BallARetroPlatform.IsDemoBuild)
        {
            fullScreenCompleted = true;
            SetEntryButtons(false);
            Hide();
            gameUIFuncInsert();
        }
    }

    private void ClearCards()
    {
        for (int i = 0; i < liveCards.Count; i++)
            if (liveCards[i] != null) Destroy(liveCards[i].gameObject);
        liveCards.Clear();
    }

    private void OnPickCard(CJS_ChoiceData picked)
    {
        if (busy || !isOpen || gameEnded || picked == null) return;
        busy = true;
        var demo = BallARetroDemoRun.Current;
        if (demo != null && demo.IsEnabled && !demo.TryBeginPick(CurrentRound()))
        {
            busy = false;
            return;
        }

        roller.PushPicked(picked);                      // 효과 적용
        CJS_Script_ChoiceState.I?.Add(picked);         // ★ 선택 스냅샷 기록(아이콘 포함)

        if (pickCount <= 1)
        {
            if (!BallARetroPlatform.IsDemoBuild) { fullScreenCompleted = true; SetEntryButtons(false); }
            Hide();
            gameUIFuncInsert();
        }
        else
        {
            pickCount--;
            RefreshCards();
        }
        busy = false;
    }

    private void OnClickReroll()
    {
        if (!isOpen || busy || gameEnded || BallARetroPlatform.IsDemoBuild) return;
        if (usedReroll >= RerollLimit) return;

        busy = true;
        usedReroll++;
        UpdateRerollUI();
        RefreshCards(true);
        StartCoroutine(RerollCooldown());
    }

    private IEnumerator RerollCooldown()
    {
        busy = true;
        yield return new WaitForSecondsRealtime(0.2f);
        busy = false;
        UpdateRerollUI();
    }

    private void OnClickSkip()
    {
        if (!isOpen || busy) return;
        if (BallARetroDemoRun.Current?.IsEnabled == true) return;
        fullScreenCompleted = true;
        SetEntryButtons(false);
        Hide();
    }

    private void RefreshLanguage(Locale _) => UpdateRerollUI();

    private void UpdateRerollUI()
    {
        if (txtRerollInfo != null)
            txtRerollInfo.text = PSH_Script_Localization.Text("Choice_Remaining", Mathf.Max(0, RerollLimit - usedReroll));

        if (btnReroll != null)
            btnReroll.interactable = isOpen && !busy && !gameEnded && (usedReroll < RerollLimit);

        if (btnSkip != null)
            btnSkip.interactable = isOpen;
    }

    private void gameUIFuncInsert()
    {
        if (gameUICanvasGo) gameUICanvasGo.SetActive(true);
        if (camManager) camManager.MonitorOff();
    }

    private void PickCountBonusFunc()
    {
        Debug.LogWarning("Round_Clear 이벤트 발생");
        pickCount = BallARetroDemoRun.Current?.IsEnabled == true ? 1 : ballCon.GetBallCount();
        if (!BallARetroPlatform.IsDemoBuild) SetEntryButtons(true);
    }

    private void SetEntryButtons(bool enabled)
    {
        foreach (var button in entryButtons) if (button != null) button.interactable = enabled;
    }

    private int CurrentRound() => KHS_Script_ScoreManager.Instance != null
        ? KHS_Script_ScoreManager.Instance.RoundRespone() : 1;

    private void OnRoundBanner(bool open)
    {
        if (!open && CurrentRound() == 1) QueueDemoStarter();
    }

    private void QueueDemoStarter()
    {
        if (!BallARetroPlatform.IsDemoBuild || starterQueued || gameEnded) return;
        starterQueued = true;
        StartCoroutine(OpenDemoStarter());
    }

    private IEnumerator OpenDemoStarter()
    {
        // Finish the banner's own Resume callbacks and all Start methods first.
        yield return null;
        yield return null;
        var demo = BallARetroDemoRun.Current;
        if (demo == null || !demo.IsEnabled || !demo.CanChoose(1)) yield break;
        ShowChoices();
        if (isOpen) camManager?.MonitorOnButton();
    }
}
