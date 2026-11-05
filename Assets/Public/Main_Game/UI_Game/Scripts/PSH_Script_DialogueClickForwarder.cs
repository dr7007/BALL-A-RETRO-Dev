using UnityEngine;
using UnityEngine.EventSystems;

namespace PSH
{
    // 다른 Canvas에 있는 대사 패널의 클릭을 현재 대사 컨트롤러에 전달합니다.
    [DisallowMultipleComponent]
    public sealed class PSH_Script_DialogueClickForwarder : MonoBehaviour, IPointerClickHandler
    {
        private PSH_Script_DialogueUI dialogue;

        public void Bind(PSH_Script_DialogueUI owner)
        {
            dialogue = owner;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (dialogue != null)
                dialogue.OnPanelPointerClick(gameObject, eventData);
        }
    }
}
