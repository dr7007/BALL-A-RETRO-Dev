using UnityEngine;

[System.Serializable]
public class CJS_ChoiceSnapshot
{
    public string name;
    public string description;
    public string rarity;
    public Sprite icon;
    public int funcIdx;

    public static CJS_ChoiceSnapshot From(CJS_ChoiceData d)
    {
        if (d == null) return new CJS_ChoiceSnapshot();
        return new CJS_ChoiceSnapshot
        {
            name = d.name,
            description = d.description,
            rarity = d.rarity,
            icon = d.icon,
            funcIdx = d.funcIdx
        };
    }
}
