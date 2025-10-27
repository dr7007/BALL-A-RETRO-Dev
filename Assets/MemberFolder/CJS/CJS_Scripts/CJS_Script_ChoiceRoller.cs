using System.Collections.Generic;
using UnityEngine;


public class CJS_Script_ChoiceRoller : MonoBehaviour, CJS_IChoiceRoller
{
    [Header("Prototype Data (DB 전 임시 목록)")]
    public List<CJS_ChoiceData> allChoices = new List<CJS_ChoiceData>();

    private System.Random rnd;

    void Awake()
    {
        rnd = new System.Random();
        
        //더미데이터
        if (allChoices.Count == 0)
        {
            allChoices.Add(new CJS_ChoiceData { name = "범퍼 점수 +100", description = "범퍼 기본 점수 100 증가", rarity = "Common" });
            allChoices.Add(new CJS_ChoiceData { name = "반발 계수 +10%", description = "반발 계수 10% 증가", rarity = "Rare" });
            allChoices.Add(new CJS_ChoiceData { name = "플리퍼 횟수 +1", description = "다음 라운드 플리퍼 1회 추가", rarity = "Epic" });
            allChoices.Add(new CJS_ChoiceData { name = "코인 +500", description = "즉시 500 코인", rarity = "Common" });
            allChoices.Add(new CJS_ChoiceData { name = "속도 +5%", description = "볼 속도 5% 증가", rarity = "Rare" });
            allChoices.Add(new CJS_ChoiceData { name = "전설 효과", description = "강력한 전설 효과", rarity = "Legendary" });
        }
    }

    /// <summary>목록에서 중복 없이 3개 뽑기</summary>
    public List<CJS_ChoiceData> Roll3()
    {
        var pool = new List<CJS_ChoiceData>(allChoices);
        var result = new List<CJS_ChoiceData>(capacity: 3);

        for (int i = 0; i < 3 && pool.Count > 0; i++)
        {
            int idx = rnd.Next(0, pool.Count);
            result.Add(pool[idx]);
            pool.RemoveAt(idx);
        }
        return result;
    }

    /// <summary>선택 기록(후에 확률 보정/DB 연동시 사용)</summary>
    public void PushPicked(CJS_ChoiceData picked)
    {
        if (picked == null) return;
        Debug.Log("[ChoiceRoller] Picked: " + picked.name);
    }
}