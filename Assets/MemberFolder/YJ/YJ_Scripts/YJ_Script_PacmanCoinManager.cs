using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

// 팩맨 스테이지의 '코인'들을 담고 있는 부모 오브젝트에 부착
public class YJ_Script_PacManCoinManager : MonoBehaviour
{
    // 이 매니저의 자식으로 있는 모든 코인 스크립트 리스트
    private YJ_Script_PacManCoin[] allCoins;

    void Awake()
    {
        allCoins = GetComponentsInChildren<YJ_Script_PacManCoin>(true);
    }

    private void OnEnable()
    {
        KHS_Script_BallOutController.BallOutEvt += HideAllCoins;
    }

    private void OnDisable()
    {
        KHS_Script_BallOutController.BallOutEvt -= HideAllCoins;
    }

    public void AppearAllCoins()
    {
        if (allCoins == null) return;

        foreach (YJ_Script_PacManCoin coin in allCoins)
        {
            coin.ActivateCoin();
        }
    }

    public void HideAllCoins()
    {
        Debug.Log("BallOut 감지. 모든 팩맨 코인을 숨깁니다.");

        if (allCoins == null) return;

        foreach (YJ_Script_PacManCoin coin in allCoins)
        {
            coin.DesactivateCoin();
        }
    }
}