using UnityEngine;

[System.Serializable]
public class CJS_ChoiceData
{
    public string localizationKey;
    public string name = "New";
    [TextArea] public string description = "설명";
    public string rarity = "Common";
    public Sprite icon;

    [Header("확률/등장 제어")]
    [Min(0f)] public float weight = 1f;
    public bool isEnabled = true;

    [Header("동작 참조 함수 인덱스")]
    public int funcIdx = 0; 
    public KHS_Script_RogueLikeManager roguelike;
    public static string Localized(string key, string part, string fallback)
        => string.IsNullOrEmpty(key) ? fallback ?? "" : PSH_Script_Localization.Text(key + "_" + part);

    public static string LocalizedRarity(string rarity)
    {
        switch ((rarity ?? "").ToLowerInvariant())
        {
            case "common": return PSH_Script_Localization.Text("Rarity_Common");
            case "normal": return PSH_Script_Localization.Text("Rarity_Normal");
            case "rare": return PSH_Script_Localization.Text("Rarity_Rare");
            case "epic": return PSH_Script_Localization.Text("Rarity_Epic");
            case "legendary": return PSH_Script_Localization.Text("Rarity_Legendary");
            default: return rarity ?? "";
        }
    }
}
