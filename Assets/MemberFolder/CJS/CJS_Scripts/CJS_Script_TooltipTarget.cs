using UnityEngine;
using UnityEngine.EventSystems;

public class CJS_Script_TooltipTarget : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [TextArea][SerializeField] private string title;
    [TextArea][SerializeField] private string body;

    private bool hovered;
    private Vector2 position;
    private void OnDisable() { if (hovered) CJS_Script_TooltipUI.I?.Hide(); hovered = false; }

    public void Set(string titleText, string bodyText)
    {
        title = titleText;
        body = bodyText;
        if (hovered) CJS_Script_TooltipUI.I?.Show(title, body, position, true);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true; position = eventData.position;
        CJS_Script_TooltipUI.I?.Show(title, body, position, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        CJS_Script_TooltipUI.I?.Hide();
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        position = eventData.position;
        CJS_Script_TooltipUI.I?.UpdatePosition(position);
    }
}
