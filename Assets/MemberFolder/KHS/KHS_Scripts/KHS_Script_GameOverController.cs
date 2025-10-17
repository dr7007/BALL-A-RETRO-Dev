using System;
using UnityEngine;

public class KHS_Script_GameOverController : MonoBehaviour
{
    public static event Action GameoverEvt;

    private void OnTriggerEnter(Collider _collider)
    {
        if (_collider.CompareTag("Ball"))
        {
            Debug.LogError("GameOver!");
            GameoverEvt?.Invoke();
        }
    }
}
