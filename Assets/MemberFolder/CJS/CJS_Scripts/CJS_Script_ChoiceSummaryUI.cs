using UnityEngine;

public class CJS_Script_ChoiceSummaryUI : MonoBehaviour
{
    [SerializeField] private Transform grid;   
    [SerializeField] private CJS_Script_ChoiceSlot slotPrefab;

    public void ShowSummary()
    {
        if (!grid || !slotPrefab) return;

        foreach (Transform c in grid) Destroy(c.gameObject);

        var list = CJS_Script_ChoiceState.I?.Picked;
        if (list == null) return;

        for (int i = 0; i < list.Count; i++)
        {
            var slot = Instantiate(slotPrefab, grid);
            slot.Bind(list[i]);
        }
    }
}
