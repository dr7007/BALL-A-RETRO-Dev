using UnityEngine;

[System.Serializable]
public class CJS_ChoiceData
{
    public string name = "New";
    [TextArea] public string description = "설명";
    public string rarity = "Common";
    public Sprite icon;

    [Header("확률/등장 제어")]
    [Min(0f)] public float weight = 1f;
    public bool isEnabled = true;      
}
