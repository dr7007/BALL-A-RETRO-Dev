using UnityEngine;

public class CJS_Script_SubmitOnKey : MonoBehaviour
{
    [Header("Refs (비워둬도 됩니다. 런타임에 자동 연결)")]
    public CJS_Script_PinballRankingService service;

    [Header("Submit Params")]
    public string gameMode = "Classic";
    public int level = 1;

    void Awake() { TryWire(); }
    void Start() { TryWire(); }
    void OnEnable() { TryWire(); }

    private void TryWire()
    {
        // 파괴된 참조(null) 또는 미연결이면 싱글톤/씬 검색으로 자가 배선
        if (!service)
            service = CJS_Script_PinballRankingService.Instance
                   ?? FindObjectOfType<CJS_Script_PinballRankingService>(includeInactive: true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9))
        {
            TryWire(); // 혹시라도 아직 못 찾았으면 한 번 더 시도
            if (!service)
            {
                Debug.LogError("[DebugSubmit] RankingService not found");
                return;
            }

            int score = Random.Range(1000, 50000);
            Debug.Log($"[DebugSubmit] F9 → {score}");

            service.SubmitScore(score, gameMode, level,
                onDone: _ => Debug.Log("[DebugSubmit] ok"),
                onFail: e => Debug.LogError("[DebugSubmit] " + e)
            );
        }
    }
}
