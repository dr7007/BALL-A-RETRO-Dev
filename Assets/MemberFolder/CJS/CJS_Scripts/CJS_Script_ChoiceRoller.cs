using System.Collections.Generic;
using UnityEngine;

public class CJS_Script_ChoiceRoller : MonoBehaviour, CJS_IChoiceReroller
{
    public enum ChanceDisplayMode
    {
        NormalizedPool,
        PerDraw        
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
    private Sprite demoCapacitorIcon;
    private Sprite demoLastBallIcon;

    void Awake()
    {
        rnd = (seed == 0) ? new System.Random() : new System.Random(seed);

        if (allChoices.Count == 0 && BallARetroPlatform.IsDemoBuild)
        {
            allChoices.Add(new CJS_ChoiceData { name = "범퍼 점수 +100", description = "범퍼 기본 점수 100 증가", rarity = "Common", weight = 5f, isEnabled = true });
            allChoices.Add(new CJS_ChoiceData { name = "반발 계수 +10%", description = "반발 계수 10% 증가", rarity = "Rare", weight = 3f, isEnabled = true });
            allChoices.Add(new CJS_ChoiceData { name = "플리퍼 횟수 +1", description = "다음 라운드 플리퍼 1회 추가", rarity = "Epic", weight = 1.5f, isEnabled = true });
            allChoices.Add(new CJS_ChoiceData { name = "코인 +500", description = "즉시 500 코인", rarity = "Common", weight = 4f, isEnabled = true });
            allChoices.Add(new CJS_ChoiceData { name = "속도 +5%", description = "볼 속도 5% 증가", rarity = "Rare", weight = 2.5f, isEnabled = true });
            allChoices.Add(new CJS_ChoiceData { name = "전설 효과", description = "강력한 전설 효과", rarity = "Legendary", weight = 0.5f, isEnabled = true });
        }

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


    public List<CJS_ChoiceData> Roll3(out Dictionary<CJS_ChoiceData, float> rollChances)
    {
        var demo = BallARetroDemoRun.Current;
        if (demo != null && demo.IsEnabled) return RollDemoChoices(demo, out rollChances);
        return RollFullChoices(null, out rollChances);
    }

    public List<CJS_ChoiceData> Reroll3(IReadOnlyCollection<int> previousIds,
        out Dictionary<CJS_ChoiceData, float> rollChances)
    {
        if (BallARetroPlatform.IsDemoBuild) return Roll3(out rollChances);
        return RollFullChoices(previousIds, out rollChances);
    }

    private List<CJS_ChoiceData> RollFullChoices(IReadOnlyCollection<int> previousIds,
        out Dictionary<CJS_ChoiceData, float> chances)
    {
        var pool = BuildPoolFromAllChoices();
        var result = new List<CJS_ChoiceData>(3);
        chances = new Dictionary<CJS_ChoiceData, float>();
        // Guarantee a changed offer when any eligible unseen effect exists.
        var unseen = previousIds == null ? null : pool.FindAll(c => !ContainsId(previousIds, c.funcIdx));
        while (result.Count < 3 && pool.Count > 0)
        {
            var candidates = result.Count == 0 && unseen != null && unseen.Count > 0 ? unseen : pool;
            float sum = SumWeights(candidates);
            double ticket = rnd.NextDouble() * sum;
            var picked = candidates[candidates.Count - 1];
            foreach (var candidate in candidates)
            {
                ticket -= candidate.weight;
                if (ticket < 0) { picked = candidate; break; }
            }
            result.Add(picked);
            // Conditional draw probability, not whole-offer inclusion probability.
            chances[picked] = picked.weight / sum * 100f;
            pool.Remove(picked);
            if (IsBonusMapChoice(picked)) pool.RemoveAll(IsBonusMapChoice);
        }
        return result;
    }

    private static bool ContainsId(IReadOnlyCollection<int> ids, int id)
    {
        foreach (int value in ids) if (value == id) return true;
        return false;
    }

    public static float RarityWeight(string rarity)
    {
        switch ((rarity ?? "").ToLowerInvariant())
        {
            case "common": case "normal": return 60;
            case "rare": return 30;
            case "epic": return 9;
            case "legendary": return 1;
            default: return 0;
        }
    }
    public void PushPicked(CJS_ChoiceData picked)
    {
        if (picked == null) return;
        var build = BallARetroRunBuild.Current;
        if (build != null && build.IsEnabled && (picked.funcIdx == 9 || picked.funcIdx == 10))
        {
            build.ApplyUpgrade(picked.funcIdx);
            return;
        }
        var demo = BallARetroDemoRun.Current;
        if (demo != null && demo.IsEnabled
            && (picked.funcIdx == BallARetroDemoRun.CapacitorChoiceId || picked.funcIdx == BallARetroDemoRun.LastBallChoiceId))
        {
            demo.ApplyUpgrade(picked.funcIdx);
            return;
        }
        picked.roguelike?.MatchingFunc(picked.funcIdx);
        Debug.Log("[ChoiceRoller] Picked: " + picked.name);
    }

    private List<CJS_ChoiceData> BuildPoolFromAllChoices()
    {
        var pool = new List<CJS_ChoiceData>();
        var ids = new HashSet<int>();
        foreach (var c in allChoices)
        {
            if (c == null || !c.isEnabled || c.weight <= 0 || float.IsNaN(c.weight) || float.IsInfinity(c.weight)
                || c.funcIdx < 0 || c.funcIdx > 8 || RarityWeight(c.rarity) <= 0 || !ids.Add(c.funcIdx)) continue;
            if (Application.isPlaying && c.roguelike == null) continue;
            pool.Add(new CJS_ChoiceData {
                funcIdx = c.funcIdx, localizationKey = c.funcIdx == 4 ? "Build_ScoreBoost" : c.localizationKey,
                name = c.name, description = c.description, icon = c.icon, rarity = c.rarity,
                weight = RarityWeight(c.rarity), roguelike = c.roguelike
            });
        }
        var build = BallARetroRunBuild.Current;
        if (build != null && build.IsEnabled)
        {
            if (build.CapacitorLevel < 2) pool.Add(FullChoice(9,
                build.CapacitorLevel == 0 ? "Build_Capacitor" : "Build_CapacitorUpgrade", FindChoice(5)?.icon));
            if (build.LastBallLevel < 2) pool.Add(FullChoice(10,
                build.LastBallLevel == 0 ? "Build_LastBall" : "Build_LastBallUpgrade", FindChoice(8)?.icon));
        }
        return pool;
    }

    private CJS_ChoiceData FullChoice(int id, string key, Sprite icon) => new CJS_ChoiceData {
        funcIdx = id, localizationKey = key, name = key, description = key,
        rarity = "Rare", icon = icon, weight = RarityWeight("Rare")
    };
    private List<CJS_ChoiceData> RollDemoChoices(BallARetroDemoRun demo, out Dictionary<CJS_ChoiceData, float> chances)
    {
        // Include the real run upgrades as well as the two demo strategies.
        // Use one demo weight scale: authored weights (5..200) would otherwise
        // drown out the new strategy cards. Never revive disabled/zero-weight cards.
        chances = new Dictionary<CJS_ChoiceData, float>();
        var pool = new List<CJS_ChoiceData>();
        var ids = new HashSet<int>();
        foreach (var source in allChoices)
        {
            if (source == null || !source.isEnabled || source.weight <= 0f
                || source.funcIdx < 0 || source.funcIdx > 8 || !ids.Add(source.funcIdx)) continue;
            pool.Add(new CJS_ChoiceData {
                funcIdx = source.funcIdx, localizationKey = source.funcIdx == 8 ? "Demo_ExtraBall" : source.localizationKey,
                name = source.name, description = source.description, icon = source.icon,
                rarity = source.rarity, weight = 1, roguelike = source.roguelike
            });
        }
        var result = new List<CJS_ChoiceData>(3);
        if (demo.CapacitorLevel < 2)
        {
            if (!demoCapacitorIcon) demoCapacitorIcon = Resources.Load<Sprite>("Demo/Choice_Capacitor");
            pool.Add(DemoChoice(BallARetroDemoRun.CapacitorChoiceId,
                demo.CapacitorLevel == 0 ? "Demo_Capacitor" : "Demo_CapacitorUpgrade",
                demoCapacitorIcon ? demoCapacitorIcon : FindChoice(5)?.icon));
        }
        if (demo.LastBallLevel < 2)
        {
            if (!demoLastBallIcon) demoLastBallIcon = Resources.Load<Sprite>("Demo/Choice_LastBall");
            pool.Add(DemoChoice(BallARetroDemoRun.LastBallChoiceId,
                demo.LastBallLevel == 0 ? "Demo_LastBall" : "Demo_LastBallUpgrade",
                demoLastBallIcon ? demoLastBallIcon : FindChoice(8)?.icon));
        }
        while (result.Count < 3 && pool.Count > 0)
        {
            int index = rnd.Next(pool.Count);
            var picked = pool[index];
            result.Add(picked);
            pool.RemoveAt(index);
            // Bonus-map effects are useful later, but three of them together
            // would leave a short demo without an immediate gameplay choice.
            if (IsBonusMapChoice(picked)) pool.RemoveAll(IsBonusMapChoice);
        }
        return result;
    }

    private static bool IsBonusMapChoice(CJS_ChoiceData choice) => choice.funcIdx >= 0 && choice.funcIdx <= 3;

    private CJS_ChoiceData FindChoice(int id) => allChoices.Find(choice => choice != null && choice.funcIdx == id);

    private CJS_ChoiceData DemoChoice(int id, string key, Sprite icon) => new CJS_ChoiceData {
        funcIdx = id, localizationKey = key, name = key, description = key,
        rarity = "Rare", icon = icon, weight = 1, roguelike = GetComponent<KHS_Script_RogueLikeManager>()
    };

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

            var previewChoice = pool.Find(candidate => candidate.funcIdx == c.funcIdx);
            if (c.isEnabled && c.weight > 0 && previewChoice != null && normalizedMap.TryGetValue(previewChoice, out float p))
                previewPercents.Add($"{c.name}: {p:0.0}%");
            else
                previewPercents.Add($"{c.name}: 0.0%");
        }
    }

    public void UnActiveChoices(int _idx)
    {
        // Effect IDs are stable even if the authored list is reordered.
        foreach (var choice in allChoices)
            if (choice != null && choice.funcIdx == _idx) choice.isEnabled = false;
    }

    private static string SafeName(CJS_ChoiceData c) => c == null ? "(null)" : c.name;
}
