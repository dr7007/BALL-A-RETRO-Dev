using TMPro;
using UnityEngine;

public class KHS_Script_UIToFinal : MonoBehaviour
{

    [SerializeField]
    private TextMeshProUGUI timeMTmp;
    [SerializeField]
    private TextMeshProUGUI timeSTmp;
    [SerializeField]
    private TextMeshProUGUI roundTmp;

    private KHS_Script_ScoreManager scoreManager;

    private void OnEnable()
    {
        KHS_Script_ScoreManager.OnGameClear += GetFinalInfomation;
        KHS_Script_ScoreManager.OnGameOver += GetFinalInfomation;
    }
    private void OnDisable()
    {
        KHS_Script_ScoreManager.OnGameClear -= GetFinalInfomation;
        KHS_Script_ScoreManager.OnGameOver -= GetFinalInfomation;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        scoreManager = FindAnyObjectByType<KHS_Script_ScoreManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void GetFinalInfomation()
    {
        int m = 0;
        int s = 0;
        float temp = Time.time;
        m = (int)(temp / 60);
        s = (int)(temp % 60);

        timeMTmp.text = $"{m}";
        timeSTmp.text = $"{s}";
        roundTmp.text = $"{scoreManager.ResponceFinal()}";
    }
}
