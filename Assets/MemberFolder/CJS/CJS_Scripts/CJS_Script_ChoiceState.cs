using System;
using System.Collections.Generic;
using UnityEngine;

public class CJS_Script_ChoiceState : MonoBehaviour
{
    public static CJS_Script_ChoiceState I { get; private set; }

    // 인스펙터에 안 보이게
    private readonly List<CJS_ChoiceSnapshot> picked = new();
    public IReadOnlyList<CJS_ChoiceSnapshot> Picked => picked;

    public event Action<CJS_ChoiceSnapshot> OnPicked;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        picked.Clear(); // 플레이 진입 시 항상 초기화
    }

    public void ResetForNewRun() => picked.Clear();

    public CJS_ChoiceSnapshot Add(CJS_ChoiceData d)
    {
        var snap = CJS_ChoiceSnapshot.From(d);
        picked.Add(snap);
        OnPicked?.Invoke(snap);
        return snap;
    }
}
