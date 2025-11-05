using System;
using UnityEngine;

public class KHS_Script_FirstFloorContact : MonoBehaviour
{
    public static event Action FirstContactEvt;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball")) return;
        else
        {
            FirstContactEvt.Invoke();
        }
    }
}
