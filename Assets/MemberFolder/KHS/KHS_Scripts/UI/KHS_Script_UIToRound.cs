using TMPro;
using UnityEngine;

public class KHS_Script_UIToRound : MonoBehaviour
{
    private KHS_Script_ScoreManager scoreManager;
    [SerializeField]
    private TextMeshProUGUI tmp;

    private void OnEnable()
    {
        KHS_Script_ScoreManager.Next_Round_Init += UpdateUIRound;
    }
    private void OnDisable()
    {
        KHS_Script_ScoreManager.Next_Round_Init -= UpdateUIRound;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        scoreManager = FindAnyObjectByType<KHS_Script_ScoreManager>();
    }

    private void Start()
    {
        UpdateUIRound();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void UpdateUIRound()
    {
        tmp.text = $"{scoreManager.RoundRespone()}";
    }
}
