using System.Collections;
using UnityEngine;

public class KHS_Script_CloseObjController : MonoBehaviour
{
    [SerializeField]
    private YJ_Script_BallController ballCon = null;
    [SerializeField]
    private bool ballStart = false;
    private BoxCollider boxcollider = null;
    [SerializeField]
    private BoxCollider stuckPreventCollider = null;

    void Start()
    {
        boxcollider = GetComponent<BoxCollider>();
        boxcollider.isTrigger = true;
        stuckPreventCollider.isTrigger = true;
        ballStart = false;
    }

    private void OnTriggerExit(Collider _collider)
    {
        if (_collider.CompareTag("Ball"))
        {
            StartCoroutine(CloseObjectDelayCoroutine());
        }
    }
    private void OnEnable()
    {
        KHS_Script_ResetController.OnReset += CloseObjReset;
        KHS_Script_BallOutController.BallOutEvt += CloseObjReset;
        KHS_Script_ScoreManager.Next_Round_Init += CloseObjReset;
    }
    private void OnDisable()
    {
        KHS_Script_ResetController.OnReset -= CloseObjReset;
        KHS_Script_BallOutController.BallOutEvt -= CloseObjReset;
        KHS_Script_ScoreManager.Next_Round_Init -= CloseObjReset;
    }

    private void CloseObjReset()
    {
        ballStart = false;
        boxcollider.isTrigger = true;
        stuckPreventCollider.isTrigger = true;
    }

    private IEnumerator CloseObjectDelayCoroutine()
    {
        yield return new WaitForSeconds(0.5f);
        ballStart = true;
        boxcollider.isTrigger = false;
        stuckPreventCollider.isTrigger = false;
    }
}
