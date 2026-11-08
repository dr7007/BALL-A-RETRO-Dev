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
        usedReroll = 0;
        if (demo != null && demo.IsEnabled && gameUICanvasGo) gameUICanvasGo.SetActive(false);

        if (panelRoot != null) panelRoot.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (demo != null && demo.IsEnabled) pickCount = 1;
        else PickCountBonusFunc();
        SetChoiceButtonsVisible(true);
        UpdateRerollUI();
        RefreshCards();

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

    private void RefreshCards()
    {
        ClearCards();

        Dictionary<CJS_ChoiceData, float> chanceMap;
        List<CJS_ChoiceData> picks = roller.Roll3(out chanceMap);

        for (int i = 0; i < picks.Count; i++)
        {
            var card = Instantiate(cardPrefab, content);
            float chance = 0f;
            if (chanceMap != null && chanceMap.TryGetValue(picks[i], out float p))
                chance = p;

            card.BindWithChance(picks[i], picked => { if (liveCards.Contains(card)) OnPickCard(picked); },
                BallARetroDemoRun.Current?.IsEnabled == true ? (float?)null : chance);
            liveCards.Add(card);
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
        if (!isOpen || busy) return;
        if (usedReroll >= maxReroll) return;

        usedReroll++;
        UpdateRerollUI();
        RefreshCards();
        StartCoroutine(RerollCooldown());
    }

    private IEnumerator RerollCooldown()
    {
        busy = true;
        yield return new WaitForSecondsRealtime(0.2f);
        busy = false;
    }

    private void OnClickSkip()
    {
        if (!isOpen || busy) return;
        if (BallARetroDemoRun.Current?.IsEnabled == true) return;
        Hide();
    }

    private void RefreshLanguage(Locale _) => UpdateRerollUI();

    private void UpdateRerollUI()
    {
        if (txtRerollInfo != null)
            txtRerollInfo.text = PSH_Script_Localization.Text("Choice_Remaining", Mathf.Max(0, maxReroll - usedReroll));

        if (btnReroll != null)
            btnReroll.interactable = isOpen && (usedReroll < maxReroll);

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
