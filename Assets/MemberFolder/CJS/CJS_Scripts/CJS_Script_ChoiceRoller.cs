using System.Collections.Generic;
using UnityEngine;

public class CJS_Script_ChoiceRoller : MonoBehaviour, CJS_IChoiceRoller
{
    [Header("Prototype Data (DB 전 임시 목록)")]
    public List<CJS_ChoiceData> allChoices = new List<CJS_ChoiceData>();

    [Header("Editor Preview (읽기전용)")]
    [SerializeField, Tooltip("현재 가중치 기준 정규화된 확률(%) 미리보기")]
    private List<string> previewPercents = new List<string>();

    [Header("Random")]
    [SerializeField] private int seed = 0; 
    private System.Random rnd;

    void Awake()
    {
        rnd = (seed == 0) ? new System.Random() : new System.Random(seed);

        if (allChoices.Count == 0)
        {
            allChoices.Add(new CJS_ChoiceData { name = "범퍼 점수 +100", description = "범퍼 기본 점수 100 증가", rarity = "Common", weight = 5f, isEnabled = true });
            allChoices.Add(new CJS_ChoiceData { name = "반발 계수 +10%", description = "반발 계수 10% 증가", rarity = "Rare", weight = 3f, isEnabled = true });
            allChoices.Add(new CJS_ChoiceData { name = "플리퍼 횟수 +1", description = "다음 라운드 플리퍼 1회 추가", rarity = "Epic", weight = 1.5f, isEnabled = true });
            allChoices.Add(new CJS_ChoiceData { name = "코인 +500", description = "즉시 500 코인", rarity = "Common", weight = 4f, isEnabled = true });
            allChoices.Add(new CJS_ChoiceData { name = "속도 +5%", description = "볼 속도 5% 증가", rarity = "Rare", weight = 2.5f, isEnabled = true });
            allChoices.Add(new CJS_ChoiceData { name = "전설 효과", description = "강력한 전설 효과", rarity = "Legendary", weight = 0.5f, isEnabled = true });
        }
    }

    void OnValidate()
    {
        RebuildPreviewPercents();
    }

    private void RebuildPreviewPercents()
    {
        previewPercents ??= new List<string>();
        previewPercents.Clear();

        float sum = 0f;
        for (int i = 0; i < allChoices.Count; i++)
        {
            var c = allChoices[i];
            if (c != null && c.isEnabled && c.weight > 0f) sum += c.weight;
        }

        if (sum <= 0f)
        {
            for (int i = 0; i < allChoices.Count; i++)
                previewPercents.Add($"{SafeName(allChoices[i])}: 0.0%");
            return;
        }

        for (int i = 0; i < allChoices.Count; i++)
        {
            var c = allChoices[i];
            float p = (c != null && c.isEnabled && c.weight > 0f) ? (c.weight / sum * 100f) : 0f;
            previewPercents.Add($"{SafeName(c)}: {p:0.0}%");
        }
    }

    private static string SafeName(CJS_ChoiceData c) => c == null ? "(null)" : c.name;

    public List<CJS_ChoiceData> Roll3(out Dictionary<CJS_ChoiceData, float> rollChances)
    {
        rollChances = new Dictionary<CJS_ChoiceData, float>();
        var pool = BuildPool();
        var result = new List<CJS_ChoiceData>(capacity: 3);

        for (int i = 0; i < 3 && pool.Count > 0; i++)
        {
            float sum = SumWeights(pool);
            if (sum <= 0f)
            {
                int idx = rnd.Next(0, pool.Count);
                var pickedFallback = pool[idx];
                result.Add(pickedFallback);
                rollChances[pickedFallback] = 0f; 
                pool.RemoveAt(idx);
                continue;
            }

            // 누적분포로 1개 추첨
            double ticket = rnd.NextDouble() * sum;
            float acc = 0f;
            CJS_ChoiceData picked = null;
            for (int k = 0; k < pool.Count; k++)
            {
                acc += Mathf.Max(0f, pool[k].weight);
                if (ticket <= acc)
                {
                    picked = pool[k];
                    break;
                }
            }
            picked ??= pool[pool.Count - 1];

            float chancePercent = (Mathf.Max(0f, picked.weight) / sum) * 100f;

            result.Add(picked);
            rollChances[picked] = chancePercent;

            pool.Remove(picked);
        }
        return result;
    }

    public void PushPicked(CJS_ChoiceData picked)
    {
        if (picked == null) return;
        Debug.Log("[ChoiceRoller] Picked: " + picked.name);
    }

    private List<CJS_ChoiceData> BuildPool()
    {
        var pool = new List<CJS_ChoiceData>();
        for (int i = 0; i < allChoices.Count; i++)
        {
            var c = allChoices[i];
            if (c == null) continue;
            if (!c.isEnabled) continue;
            if (c.weight <= 0f) continue;
            pool.Add(c);
        }
        if (pool.Count == 0)
        {
            for (int i = 0; i < allChoices.Count; i++)
                if (allChoices[i] != null && allChoices[i].isEnabled)
                    pool.Add(allChoices[i]);
        }
        return pool;
    }

    private static float SumWeights(List<CJS_ChoiceData> list)
    {
        float s = 0f;
        for (int i = 0; i < list.Count; i++)
            s += Mathf.Max(0f, list[i].weight);
        return s;
    }
}
