using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class CJS_Script_ChoiceUIController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private MonoBehaviour rollerBehaviour;      // Inspector에 CJS_Script_ChoiceRoller
    private CJS_IChoiceRoller roller;

    [SerializeField] private GameObject panelRoot;               // Panel_Choice
    [SerializeField] private Transform content;                  // 카드가 생성될 Grid 부모
    [SerializeField] private CJS_Script_ChoiceCard cardPrefab;   // 카드 프리팹(루트에 Button)

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

    void OnValidate()
    {
        if (rollerBehaviour != null && !(rollerBehaviour is CJS_IChoiceRoller))
            Debug.LogWarning("CJS_Script_ChoiceUIController: rollerBehaviour는 CJS_IChoiceRoller를 구현해야 합니다.", this);
    }

    void OnEnable()
    {
        if (btnReroll != null) btnReroll.onClick.AddListener(OnClickReroll);
        if (btnSkip != null) btnSkip.onClick.AddListener(OnClickSkip);
    }

    void OnDisable()
    {
        if (btnReroll != null) btnReroll.onClick.RemoveListener(OnClickReroll);
        if (btnSkip != null) btnSkip.onClick.RemoveListener(OnClickSkip);
    }

    void Awake()
    {
        roller = rollerBehaviour as CJS_IChoiceRoller;
        if (panelRoot != null) panelRoot.SetActive(false);
        usedReroll = 0;
        isOpen = false;
        busy = false;

        SetChoiceButtonsVisible(false);
        UpdateRerollUI();
    }

    private void SetChoiceButtonsVisible(bool visible)
    {
        if (btnSkip) btnSkip.gameObject.SetActive(visible);
        if (btnReroll) btnReroll.gameObject.SetActive(visible);
        if (txtRerollInfo) txtRerollInfo.gameObject.SetActive(visible);
    }

    public void ShowChoices()
    {
        if (isOpen) return;
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

        if (panelRoot != null) panelRoot.SetActive(true);
        Time.timeScale = 0f;

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
        Time.timeScale = 1f;

        ClearCards();

        SetChoiceButtonsVisible(false);
        if (panelRoot != null) panelRoot.SetActive(false);

        UpdateRerollUI();
    }

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

            card.BindWithChance(picks[i], OnPickCard, chance);
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
        if (busy) return;
        busy = true;

        roller.PushPicked(picked);

        Hide();
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
        Hide();
    }

    private void UpdateRerollUI()
    {
        if (txtRerollInfo != null)
            txtRerollInfo.text = "새로고침: " + Mathf.Max(0, maxReroll - usedReroll) + "회";

        if (btnReroll != null)
            btnReroll.interactable = isOpen && (usedReroll < maxReroll);

        if (btnSkip != null)
            btnSkip.interactable = isOpen;
    }
}
