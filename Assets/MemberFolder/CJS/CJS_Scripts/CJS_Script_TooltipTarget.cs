using UnityEngine;
using UnityEngine.EventSystems;

public class CJS_Script_TooltipTarget : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [TextArea][SerializeField] private string title;
    [TextArea][SerializeField] private string body;

    public void Set(string titleText, string bodyText)
    {
        title = titleText;
        body = bodyText;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        CJS_Script_TooltipUI.I?.Show(title, body, eventData.position, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CJS_Script_TooltipUI.I?.Hide();
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        CJS_Script_TooltipUI.I?.UpdatePosition(eventData.position);
    }
}
