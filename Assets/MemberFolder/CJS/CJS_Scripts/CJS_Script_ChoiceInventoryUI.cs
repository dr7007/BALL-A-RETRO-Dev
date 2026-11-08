using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CJS_Script_ChoiceInventoryUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform grid;
    [SerializeField] private CJS_Script_ChoiceSlot slotPrefab;

    private bool needsRebuild = true;
    private bool isVisible;
    private CJS_Script_ChoiceState subscribedState;
    private Coroutine layoutCoroutine;

    void OnEnable()
    {
        BindState();
        KHS_Script_ResetController.OnReset += HandleGlobalReset;
    }

    private void Start() { BindState(); if (isVisible && needsRebuild) RebuildAll(); }

    private void BindState()
    {
        var state = CJS_Script_ChoiceState.I;
        if (subscribedState == state) return;
        UnbindState();
        subscribedState = state;
        if (subscribedState != null)
        {
            subscribedState.OnPicked += HandlePicked;
            subscribedState.OnCleared += HandleCleared;
        }
        needsRebuild = true;
    }

    private void UnbindState()
    {
        if (subscribedState != null)
        {
            subscribedState.OnPicked -= HandlePicked;
            subscribedState.OnCleared -= HandleCleared;
        }
        subscribedState = null;
    }

    void OnDisable()
    {
        UnbindState();
        KHS_Script_ResetController.OnReset -= HandleGlobalReset;
        if (layoutCoroutine != null) { StopCoroutine(layoutCoroutine); layoutCoroutine = null; }
    }

    public void SetVisible(bool v)
    {
        if (isActiveAndEnabled) BindState();
        isVisible = v;
        if (isVisible && needsRebuild) RebuildAll();
    }

    public void RebuildAll()
    {
        if (!grid || !slotPrefab) return;

        foreach (Transform c in grid) { c.gameObject.SetActive(false); Destroy(c.gameObject); }

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
        if (!grid) return;
        foreach (Transform c in grid) { c.gameObject.SetActive(false); Destroy(c.gameObject); }
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
        var rt = grid as RectTransform;
        if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        if (layoutCoroutine != null) StopCoroutine(layoutCoroutine);
        if (isActiveAndEnabled) layoutCoroutine = StartCoroutine(RefreshNextFrame());
    }

    private IEnumerator RefreshNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        var rt = grid as RectTransform;
        if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        layoutCoroutine = null;
    }
}
