using UnityEngine;

public class KHS_Script_CloseObjController : MonoBehaviour
{
    [SerializeField]
    private KHS_Script_BallController ballCon = null;
    [SerializeField]
    private bool ballStart = false;
    private BoxCollider boxcollider = null;

    void Start()
    {
        boxcollider = GetComponent<BoxCollider>();
        boxcollider.isTrigger = true;
        ballStart = false;
    }

    private void OnTriggerExit(Collider _collider)
    {
        if (_collider.CompareTag("Ball"))
        {
            ballStart = true;
            boxcollider.isTrigger = false;
        }
    }
    private void OnEnable()
    {
        KHS_Script_ResetController.OnReset += CloseObjReset;
        KHS_Script_GameOverController.GameoverEvt += CloseObjReset;
    }
    private void OnDisable()
    {
        KHS_Script_ResetController.OnReset -= CloseObjReset;
        KHS_Script_GameOverController.GameoverEvt -= CloseObjReset;
    }

    private void CloseObjReset()
    {
        ballStart = false;
        boxcollider.isTrigger = true;
    }
}
