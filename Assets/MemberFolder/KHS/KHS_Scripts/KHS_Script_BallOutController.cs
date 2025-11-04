using System;
using UnityEngine;

public class KHS_Script_BallOutController : MonoBehaviour
{
    public static event Action BallOutEvt;

    private void OnCollisionEnter(Collision _collision)
    {
        if (_collision.collider.CompareTag("Ball"))
        {
            Debug.LogError("BallOut!");
            BallOutEvt?.Invoke();
        }
    }
}
