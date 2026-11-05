using TMPro;
using UnityEngine;

public class KHS_Script_UIToScore : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI currentScoreUI;

    private void Awake()
    {
        if (currentScoreUI == null) currentScoreUI = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        KHS_Script_ScoreManager.OnScoreChanged += Refresh;
        RefreshCurrentScore();
    }

    private void Start() => RefreshCurrentScore();

    private void OnDisable() => KHS_Script_ScoreManager.OnScoreChanged -= Refresh;

    private void RefreshCurrentScore()
    {
        var manager = KHS_Script_ScoreManager.Instance;
        if (manager != null) Refresh(manager.curScore);
    }

    private void Refresh(int score)
    {
        if (currentScoreUI != null) currentScoreUI.text = score.ToString();
    }
}
