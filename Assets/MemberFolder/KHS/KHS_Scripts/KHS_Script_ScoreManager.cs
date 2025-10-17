using UnityEngine;

public class KHS_Script_ScoreManager : MonoBehaviour
{
    [Header("���ھ� ����")]
    [Tooltip("���� ���ھ� ������ ǥ���մϴ�")]
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
        Debug.LogWarning($"���� ���ھ�: {curScore} (+{value})");
    }

    private void ScoreReset()
    {
        Debug.LogWarning($"���� �� ������ ���ھ� ǥ�� : {curScore}");
        curScore = 0;
    }
}
