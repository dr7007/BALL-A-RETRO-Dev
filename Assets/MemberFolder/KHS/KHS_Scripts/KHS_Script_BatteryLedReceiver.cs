using System.Runtime.CompilerServices;
using UnityEngine;

public class KHS_Script_BatteryLedReceiver : MonoBehaviour
{
    [SerializeField]
    private bool isOn = false;

    private void OnEnable()
    {
        KHS_Script_BallOutController.BallOutEvt += ResetLED;
    }
    private void OnDisable()
    {
        KHS_Script_BallOutController.BallOutEvt -= ResetLED;
    }


    private void Start()
    {
        isOn = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void IsOnResponse()
    {
        Debug.Log("IsOnResponse");
        isOn = true;
    }
    public void ResetLED()
    {
        isOn = false;
    }

    public bool LEDCheck()
    {
        return isOn;
    }
}
