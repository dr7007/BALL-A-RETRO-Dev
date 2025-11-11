using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CJS_Script_ChoiceInventoryUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform grid;
    [SerializeField] private CJS_Script_ChoiceSlot slotPrefab;

    private bool needsRebuild;
    private bool isVisible;

    void OnEnable()
    {
        var s = CJS_Script_ChoiceState.I;
        if (s != null)
        {
            s.OnPicked += HandlePicked;
            s.OnCleared += HandleCleared;  
        }
        KHS_Script_ResetController.OnReset += HandleGlobalReset; 
    }

    void OnDisable()
    {
        var s = CJS_Script_ChoiceState.I;
        if (s != null)
        {
            s.OnPicked -= HandlePicked;
            s.OnCleared -= HandleCleared;
        }
        KHS_Script_ResetController.OnReset -= HandleGlobalReset;
    }

    public void SetVisible(bool v)
    {
        isVisible = v;
        if (isVisible && needsRebuild) RebuildAll();
    }

    public void RebuildAll()
    {
        if (!grid || !slotPrefab) return;

        foreach (Transform c in grid) Destroy(c.gameObject);

        var list = CJS_Script_ChoiceState.I?.Picked;
        int count = (list == null) ? 0 : list.Count;
        Debug.Log($"[InventoryUI] RebuildAll count={count}");

        if (list != null)
            for (int i = 0; i < list.Count; i++)
                AddSlot(list[i]);

        needsRebuild = false;
        RefreshLayoutNow();
    }

    private void HandlePicked(CJS_ChoiceSnapshot snap)
    {
        if (!grid || !slotPrefab) return;

        if (isVisible)
        {
            AddSlot(snap);
            RefreshLayoutNow();
        }
        else
        {
            needsRebuild = true;
        }
    }

    private void HandleCleared()     
    {
        ClearUI();
    }

    private void HandleGlobalReset() 
    {
        ClearUI();
    }

    private void ClearUI()
    {
        foreach (Transform c in grid) Destroy(c.gameObject);
        needsRebuild = false;
        RefreshLayoutNow();
    }

    private void AddSlot(CJS_ChoiceSnapshot snap)
    {
        var slot = Instantiate(slotPrefab, grid);
        slot.Bind(snap);
    }

    private void RefreshLayoutNow()
    {
        Canvas.ForceUpdateCanvases();
        var rt = grid as RectTransform;
        if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        Canvas.ForceUpdateCanvases();
        StartCoroutine(RefreshNextFrame());
    }

    private IEnumerator RefreshNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        var rt = grid as RectTransform;
        if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }
}
