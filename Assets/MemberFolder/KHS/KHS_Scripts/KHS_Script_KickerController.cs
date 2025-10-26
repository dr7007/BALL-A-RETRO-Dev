using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

public class KHS_Script_KickerController : MonoBehaviour
{
    [SerializeField]
    private Transform cover_Tr = null;
    [SerializeField]
    private Vector3 initPos = Vector3.zero;
    [SerializeField]
    private Vector3 closePos = Vector3.zero;

    [SerializeField]
    private bool isUsed = false;

    public float coverOpenDis = 0.4f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        initPos = cover_Tr.position;
        isUsed = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable()
    {
        KHS_Script_BallOutController.BallOutEvt += Open_Kicker;
        KHS_Script_SwitchComplete.kickerOpenevt += Open_Kicker;
    }

    private void OnDisable()
    {
        KHS_Script_BallOutController.BallOutEvt -= Open_Kicker;
        KHS_Script_SwitchComplete.kickerOpenevt -= Open_Kicker;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball"))
            return;

        if(!isUsed)
        {
            StartCoroutine(KickerCoroutine(other));
        }
    }

    public void Open_Kicker()
    {
        isUsed = false;
        cover_Tr.localPosition = initPos;
    }

    private IEnumerator KickerCoroutine(Collider _collider)
    {
        Rigidbody ballRb = _collider.GetComponent<Rigidbody>();
        yield return new WaitForSeconds(1.5f);
        ballRb.AddForce(0f, 0f, 40f, ForceMode.Impulse);
        yield return new WaitForSeconds(0.2f);
        isUsed = true;
        cover_Tr.localPosition = closePos;
    }
}
