using System;
using System.Collections.Generic;
using UnityEngine;

public class CJS_Script_ChoiceState : MonoBehaviour
{
    public static CJS_Script_ChoiceState I { get; private set; }

    private readonly List<CJS_ChoiceSnapshot> picked = new();
    public IReadOnlyList<CJS_ChoiceSnapshot> Picked => picked;

    public event Action<CJS_ChoiceSnapshot> OnPicked;
    public event Action OnCleared; 

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        picked.Clear(); // 첫 진입 시 초기화
    }

    /// 새 런/로비 복귀/닉네임 재설정 등에서 호출
    public void ResetForNewRun()
    {
        picked.Clear();
        OnCleared?.Invoke(); 
        Debug.Log("[ChoiceState] cleared for new run");
    }

    public CJS_ChoiceSnapshot Add(CJS_ChoiceData d)
    {
        var snap = CJS_ChoiceSnapshot.From(d);
        picked.Add(snap);
        OnPicked?.Invoke(snap);
        return snap;
    }
}
