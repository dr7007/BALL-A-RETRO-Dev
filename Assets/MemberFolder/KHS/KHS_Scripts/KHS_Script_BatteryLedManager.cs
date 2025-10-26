using UnityEngine;

public class KHS_Script_BatteryLedManager : MonoBehaviour
{
    [SerializeField]
    private KHS_Script_BatteryLedReceiver[] receivers;
    private KHS_Script_ScoreManager scoreManager;

    private int ledOnMount = 0;
    private bool init_delay = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        init_delay = false;
        scoreManager = FindAnyObjectByType<KHS_Script_ScoreManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if(!init_delay)
        {
            foreach (var receiver in receivers)
            {
                receiver.GetComponent<ChangeSpriteRenderer>().F_ChangeSprite_Off();
            }
            init_delay = true;
        }
        for (int i = 0; i < receivers.Length; i++)
        {
            if(receivers[i].LEDCheck())
                ledOnMount++;
        }
        
        if(ledOnMount >= 5)
        {
            scoreManager.AddScore(3000);
            foreach(var  receiver in receivers)
            {
                receiver.ResetLED();
                receiver.GetComponent<ChangeSpriteRenderer>().F_ChangeSprite_Off();
            }
            ledOnMount = 0;
        }
        else
        {
            ledOnMount = 0;
        }
        
    }
}
