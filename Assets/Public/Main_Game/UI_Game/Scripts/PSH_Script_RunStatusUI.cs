using PSH;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

// Lives on Canvas_GameScene. The toast never captures clicks or pauses gameplay.
public class PSH_Script_RunStatusUI : MonoBehaviour
{
    [SerializeField] private TMP_Text resultSummaryText;
    [SerializeField] private float notificationDuration = 5f;

    private GameObject notificationRoot;
    private TMP_Text notificationText;
    private float notificationRemaining;
    private bool showingResult;

    private void OnEnable()
    {
        BallARetroDemoPresentation.InstallHud(transform, resultSummaryText != null ? resultSummaryText.font : null);
        KHS_Script_ScoreManager.OnRecordChallengeStarted += ShowChallenge;
        KHS_Script_ScoreManager.OnGameOver += HideNotification;
        KHS_Script_ScoreManager.OnGameClear += HideNotification;
        KHS_Script_ResetController.OnReset += ResetView;
        PSH_Script_DialogueUI.OnDialogueComplete += OnDialogueComplete;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        KHS_Script_ScoreManager.OnRecordChallengeStarted -= ShowChallenge;
        KHS_Script_ScoreManager.OnGameOver -= HideNotification;
        KHS_Script_ScoreManager.OnGameClear -= HideNotification;
        KHS_Script_ResetController.OnReset -= ResetView;
        PSH_Script_DialogueUI.OnDialogueComplete -= OnDialogueComplete;
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        HideNotification();
    }

    private void Update()
    {
        if (notificationRemaining <= 0f) return;
        // Keep the message readable while settings or a choice has paused the game.
        notificationRemaining -= Time.deltaTime;
        if (notificationRemaining <= 0f) HideNotification();
    }

    private void ShowChallenge()
    {
        EnsureNotification();
        notificationRemaining = notificationDuration;
        RefreshChallenge();
        notificationRoot.SetActive(true);
    }

    private void RefreshChallenge()
    {
        var manager = KHS_Script_ScoreManager.Instance;
        if (notificationText != null && manager != null)
            notificationText.text = string.Format(Text("Run_GoalReached"), manager.targetScore);
    }

    private void HideNotification()
    {
        notificationRemaining = 0f;
        if (notificationRoot != null) notificationRoot.SetActive(false);
    }

    private void ResetView()
    {
        showingResult = false;
        HideNotification();
    }

    private void OnDialogueComplete(string id)
    {
        var manager = KHS_Script_ScoreManager.Instance;
        if ((id != "Ending1" && id != "Ending2") || manager == null || !manager.HasEnded) return;
        showingResult = true;
        RefreshResult();
    }

    private void RefreshResult()
    {
        var manager = KHS_Script_ScoreManager.Instance;
        if (resultSummaryText == null || manager == null || !manager.HasEnded) return;

        bool flippersExhausted = manager.EndReason == KHS_Script_ScoreManager.GameOverReason.FlippersExhausted;
        string reason = manager.IsClearResult
            ? Text(flippersExhausted ? "Run_ClearFlippersExhausted" : "Run_ClearBallsExhausted")
            : Text(flippersExhausted ? "Run_FlippersExhausted" : "Run_BallsExhausted");
        string progress = manager.FinalUserScore >= manager.FinalTargetScore
            ? string.Format(Text("Run_TargetAchieved"), manager.FinalUserScore, manager.FinalTargetScore)
            : string.Format(Text("Run_TargetMissed"), manager.FinalUserScore, manager.FinalTargetScore,
                manager.FinalTargetScore - manager.FinalUserScore);

        resultSummaryText.enableAutoSizing = true;
        resultSummaryText.fontSizeMin = 24;
        resultSummaryText.fontSizeMax = 32;
        resultSummaryText.alignment = TextAlignmentOptions.MidlineLeft;
        resultSummaryText.maxVisibleCharacters = int.MaxValue;
        string titleColor = manager.IsClearResult ? "#26FFB3" : "#FFBC55";
        resultSummaryText.text = $"<color={titleColor}>{reason}</color>\n{progress}";
    }

    private void OnLocaleChanged(Locale locale)
    {
        if (notificationRemaining > 0f) RefreshChallenge();
        if (showingResult) RefreshResult();
    }

    private static string Text(string key)
    {
        var table = LocalizationSettings.StringDatabase.GetTable("UITable");
        return table?.GetEntry(key)?.LocalizedValue ?? key;
    }

    private void EnsureNotification()
    {
        if (notificationRoot != null) return;
        notificationRoot = new GameObject("RecordChallengeNotification", typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)notificationRoot.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0.25f, 1f);
        rect.anchorMax = new Vector2(0.75f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, BallARetroDemoPresentation.CanInstall(transform) ? -144f : -65f);
        rect.sizeDelta = new Vector2(0f, 110f);
        var background = notificationRoot.GetComponent<Image>();
        background.color = new Color(0.02f, 0.04f, 0.09f, 0.94f);
        background.raycastTarget = false;

        var textObject = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(rect, false);
        notificationText = textObject.GetComponent<TextMeshProUGUI>();
        if (resultSummaryText != null) notificationText.font = resultSummaryText.font;
        notificationText.color = new Color(0.15f, 1f, 0.7f);
        notificationText.fontSizeMin = 24;
        notificationText.fontSizeMax = 36;
        notificationText.enableAutoSizing = true;
        notificationText.alignment = TextAlignmentOptions.Center;
        notificationText.raycastTarget = false;
        var textRect = notificationText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20f, 12f);
        textRect.offsetMax = new Vector2(-20f, -12f);
    }
}
