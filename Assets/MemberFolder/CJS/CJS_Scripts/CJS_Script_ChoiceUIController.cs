using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// 패널 열고/닫고, 카드 생성/리롤/스킵/선택 처리
/// </summary>
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
        // 인터페이스 캐스팅 미리 점검
        if (rollerBehaviour != null && !(rollerBehaviour is CJS_IChoiceRoller))
            Debug.LogWarning("rollerBehaviour는 CJS_IChoiceRoller를 구현해야 합니다.", this);
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
        UpdateRerollUI();
    }

    /// <summary>패널 열기(외부 호출)</summary>
    public void ShowChoices()
    {
        if (isOpen) return;
        if (roller == null)
        {
            Debug.LogError("CJS_IChoiceRoller가 연결되지 않았습니다.", this);
            return;
        }

        isOpen = true;
        usedReroll = 0;

        if (panelRoot != null) panelRoot.SetActive(true);
        Time.timeScale = 0f;

        UpdateRerollUI();
        RefreshCards();

        if (liveCards.Count > 0)
        {
            Button first = liveCards[0].GetComponentInChildren<Button>();
            if (first != null) EventSystem.current?.SetSelectedGameObject(first.gameObject);
        }
    }

    /// <summary>패널 닫기</summary>
    public void Hide()
    {
        if (!isOpen) return;

        isOpen = false;
        Time.timeScale = 1f;

        ClearCards();
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void RefreshCards()
    {
        ClearCards();

        List<CJS_ChoiceData> picks = roller.Roll3();
        for (int i = 0; i < picks.Count; i++)
        {
            var card = Instantiate(cardPrefab, content);
            card.Bind(picks[i], OnPickCard);
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
            btnReroll.interactable = usedReroll < maxReroll;
    }
}
