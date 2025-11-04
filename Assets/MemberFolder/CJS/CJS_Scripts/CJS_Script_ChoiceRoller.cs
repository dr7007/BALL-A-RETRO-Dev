using System.Collections.Generic;
using UnityEngine;

public class CJS_Script_ChoiceRoller : MonoBehaviour, CJS_IChoiceRoller
{
    public enum ChanceDisplayMode
    {
        NormalizedPool, // ★ 인스펙터와 동일: 전체 풀 기준 정규화(권장)
        PerDraw         // 각 뽑기 시점의 남은 풀 기준(기존 표시 방식)
    }

    [Header("Prototype Data (DB 전 임시 목록)")]
    public List<CJS_ChoiceData> allChoices = new List<CJS_ChoiceData>();

    [Header("Editor Preview (읽기전용)")]
    [SerializeField, Tooltip("현재 가중치 기준 정규화된 확률(%) 미리보기")]
    private List<string> previewPercents = new List<string>();

    [Header("Display/Draw Settings")]
    [SerializeField] private ChanceDisplayMode displayMode = ChanceDisplayMode.NormalizedPool;

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

        // 선택지 효과용 매니저 주입(있으면)
        var rl = GetComponent<KHS_Script_RogueLikeManager>();
        foreach (var choice in allChoices)
        {
            if (choice != null) choice.roguelike = rl;
        }
    }

    void OnValidate()
    {
        // 인스펙터 미리보기도 실제 풀과 동일한 기준(활성+가중치>0)으로 계산
        var pool = BuildPoolFromAllChoices();
        var map = ComputeNormalizedPercentMap(pool);
        RebuildPreviewPercents(pool, map);
    }

    // ─────────────────────────────────────────────────────────────────────────────

    public List<CJS_ChoiceData> Roll3(out Dictionary<CJS_ChoiceData, float> rollChances)
    {
        // 1) 실제 추첨 풀 구성(활성 + weight>0). 하나도 없으면 활성만 허용
        var pool = BuildPoolFromAllChoices();

        // 2) "인스펙터와 동일한" 표시 확률 테이블(전체 풀 기준 정규화) 미리 계산
        var normalizedMap = ComputeNormalizedPercentMap(pool);

        // 3) 비복원 가중치 추첨(로직 동일). 다만 UI에 넘길 확률은 displayMode에 따라 선택
        rollChances = new Dictionary<CJS_ChoiceData, float>();
        var result = new List<CJS_ChoiceData>(capacity: 3);

        // 작업용 리스트(제거용)
        var work = new List<CJS_ChoiceData>(pool);

        for (int i = 0; i < 3 && work.Count > 0; i++)
        {
            float sum = SumWeights(work);
            CJS_ChoiceData picked;

            if (sum <= 0f)
            {
                // 모든 weight가 0이거나 음수인 비정상 케이스: 균등 랜덤 폴백
                int idx = rnd.Next(0, work.Count);
                picked = work[idx];
            }
            else
            {
                // 누적 분포 표본추출
                double ticket = rnd.NextDouble() * sum;
                float acc = 0f;
                picked = work[0];
                for (int k = 0; k < work.Count; k++)
                {
                    acc += Mathf.Max(0f, work[k].weight);
                    if (ticket <= acc)
                    {
                        picked = work[k];
                        break;
                    }
                }
            }

            result.Add(picked);

            float displayPercent = 0f;
            if (displayMode == ChanceDisplayMode.PerDraw)
            {
                float localSum = Mathf.Max(0.00001f, sum);
                displayPercent = Mathf.Max(0f, picked.weight) / localSum * 100f;
            }
            else // NormalizedPool
            {
                if (!normalizedMap.TryGetValue(picked, out displayPercent))
                    displayPercent = 0f;
            }
            rollChances[picked] = displayPercent;

            work.Remove(picked);
        }

        return result;
    }

    public void PushPicked(CJS_ChoiceData picked)
    {
        if (picked == null) return;
        picked.roguelike?.MatchingFunc(picked.funcIdx);
        Debug.Log("[ChoiceRoller] Picked: " + picked.name);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 내부 유틸

    private List<CJS_ChoiceData> BuildPoolFromAllChoices()
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
            {
                var c = allChoices[i];
                if (c != null && c.isEnabled) pool.Add(c);
            }
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

    private static Dictionary<CJS_ChoiceData, float> ComputeNormalizedPercentMap(List<CJS_ChoiceData> pool)
    {
        var map = new Dictionary<CJS_ChoiceData, float>(pool.Count);
        float sum = 0f;
        for (int i = 0; i < pool.Count; i++)
            sum += Mathf.Max(0f, pool[i].weight);

        if (sum <= 0f)
        {
            for (int i = 0; i < pool.Count; i++) map[pool[i]] = 0f;
            return map;
        }

        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            float p = Mathf.Max(0f, c.weight) / sum * 100f;
            map[c] = p;
        }
        return map;
    }

    private void RebuildPreviewPercents(List<CJS_ChoiceData> pool, Dictionary<CJS_ChoiceData, float> normalizedMap)
    {
        previewPercents ??= new List<string>();
        previewPercents.Clear();

        if (pool.Count == 0)
        {
            for (int i = 0; i < allChoices.Count; i++)
                previewPercents.Add($"{SafeName(allChoices[i])}: 0.0%");
            return;
        }

        //
        for (int i = 0; i < allChoices.Count; i++)
        {
            var c = allChoices[i];
            if (c == null)
            {
                previewPercents.Add("(null): 0.0%");
                continue;
            }

            if (normalizedMap.TryGetValue(c, out float p))
                previewPercents.Add($"{c.name}: {p:0.0}%");
            else
                previewPercents.Add($"{c.name}: 0.0%");
        }
    }

    private static string SafeName(CJS_ChoiceData c) => c == null ? "(null)" : c.name;
}
