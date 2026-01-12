using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KHS_Script_CloseObjController : MonoBehaviour
{
    [SerializeField] private YJ_Script_BallController ballCon = null;
    [SerializeField] private bool ballStart = false;
    private BoxCollider boxcollider = null;
    [SerializeField] private BoxCollider stuckPreventCollider = null;

    // 현재 닿아있는 Ball 콜라이더 개수 추적용
    private int ballContactCount = 0;

    void Start()
    {
        boxcollider = GetComponent<BoxCollider>();
        boxcollider.isTrigger = true;
        stuckPreventCollider.isTrigger = true;
        ballStart = false;
    }

    private void OnEnable()
    {
        KHS_Script_ResetController.OnReset += CloseObjReset;
        KHS_Script_BallOutController.BallOutEvt += CloseObjReset;
        //KHS_Script_ScoreManager.Next_Round_Init += CloseObjReset;
    }

    private void OnDisable()
    {
        KHS_Script_ResetController.OnReset -= CloseObjReset;
        KHS_Script_BallOutController.BallOutEvt -= CloseObjReset;
        //KHS_Script_ScoreManager.Next_Round_Init -= CloseObjReset;
    }

    private void CloseObjReset()
    {
        ballStart = false;
        boxcollider.isTrigger = true;
        stuckPreventCollider.isTrigger = true;
        ballContactCount = 0;
    }

    private void OnTriggerEnter(Collider _collider)
    {
        if (_collider.CompareTag("Ball"))
        {
            ballContactCount++;
        }
    }

    private void OnTriggerExit(Collider _collider)
    {
        if (_collider.CompareTag("Ball"))
        {
            ballContactCount = Mathf.Max(0, ballContactCount - 1);

            // 두 콜라이더 모두 빠져나간 경우만 실행
            if (ballContactCount == 0)
            {
                StartCoroutine(CloseObjectDelayCoroutine());
            }
        }
    }

    private IEnumerator CloseObjectDelayCoroutine()
    {
        yield return new WaitForSeconds(0.5f);
        ballStart = true;
        boxcollider.isTrigger = false;
        stuckPreventCollider.isTrigger = false;
    }
}
