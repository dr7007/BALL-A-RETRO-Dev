using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class KHS_Script_ResetController : MonoBehaviour
{
    public static event Action OnReset;

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.R))
        {
            OnReset.Invoke();
        }
    }
}
