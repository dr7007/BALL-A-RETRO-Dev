using UnityEngine;

public class CJS_Script_SubmitOnKey : MonoBehaviour
{
    public CJS_Script_PinballRankingService service;
    public string gameMode = "Classic";
    public int level = 1;

    void Start()
    {
        if (service == null)
            service = FindObjectOfType<CJS_Script_PinballRankingService>(includeInactive: true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9))
        {
            int score = Random.Range(1000, 50000);
            Debug.Log($"[DebugSubmit] F9 ¡æ {score}");
            service.SubmitScore(score, gameMode, level,
                onDone: _ => Debug.Log("[DebugSubmit] ok"),
                onFail: e => Debug.LogError("[DebugSubmit] " + e)
            );
        }
    }
}
