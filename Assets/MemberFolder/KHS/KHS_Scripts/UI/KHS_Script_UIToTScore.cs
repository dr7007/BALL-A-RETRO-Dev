using TMPro;
using UnityEngine;

public class KHS_Script_UIToTScore : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI targetScoreUI = null;

    private void Awake()
    {
        if (targetScoreUI == null) targetScoreUI = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        KHS_Script_ScoreManager.UILateUpdate += UpdateTScore;
        KHS_Script_ScoreManager.OnScoreChanged += OnScoreChanged;
        KHS_Script_ScoreManager.Next_Round_Init += UpdateTScore;
        KHS_Script_PlungerController.TestTScoreUpdateEvt += UpdateTScore;
        UpdateTScore();
    }
    private void OnDisable()
    {
        KHS_Script_ScoreManager.UILateUpdate -= UpdateTScore;
        KHS_Script_ScoreManager.OnScoreChanged -= OnScoreChanged;
        KHS_Script_ScoreManager.Next_Round_Init -= UpdateTScore;
        KHS_Script_PlungerController.TestTScoreUpdateEvt -= UpdateTScore;
    }

    private void UpdateTScore()
    {
        // ScoreManager and TutorialDirector may initialize after this HUD's Start.
        var manager = KHS_Script_ScoreManager.Instance;
        if (manager != null && targetScoreUI != null)
            targetScoreUI.text = " / " + manager.targetScore;
    }

    private void Start() => UpdateTScore();
    private void OnScoreChanged(int _) => UpdateTScore();
}
