using UnityEngine;

public class KHS_Script_ScoreManager : MonoBehaviour
{
    [Header("스코어 정보")]
    [Tooltip("현재 스코어 정보를 표시합니다")]
    [SerializeField]
    private int curScore = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        curScore = 0;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable()
    {
        KHS_Script_ResetController.OnReset += ScoreReset;
    }
    private void OnDisable()
    {
        KHS_Script_ResetController.OnReset -= ScoreReset;
    }
    public void AddScore(int value)
    {
        curScore += value;
        Debug.LogWarning($"현재 스코어: {curScore} (+{value})");
    }

    private void ScoreReset()
    {
        Debug.LogWarning($"리셋 전 마지막 스코어 표기 : {curScore}");
        curScore = 0;
    }
}
