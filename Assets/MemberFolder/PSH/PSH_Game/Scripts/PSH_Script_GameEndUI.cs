using UnityEngine;

public class PSH_Script_GameEndUI : MonoBehaviour
{
    [SerializeField] GameObject panel_Rank;

    public void OpenRankingPen()
    {
        panel_Rank.gameObject.SetActive(true);
    }
    public void CloseRaning()
    {
        panel_Rank.gameObject.SetActive(false);
    }
}
