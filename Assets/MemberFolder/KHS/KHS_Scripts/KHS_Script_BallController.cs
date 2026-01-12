using System;
using UnityEngine;
using UnityEngine.UIElements;

public class KHS_Script_BallController : MonoBehaviour
{
    public static event Action GameOverEvt;

    [SerializeField]
    private Rigidbody rigidBody = null;
    [SerializeField]
    private float Gravity = 9.8f;
    [SerializeField]
    private Vector3 GravDirection = Vector3.zero;
    [SerializeField]
    private int BallCount = 0;

    private bool isEnable = true;

    private Vector3 initBallPos = Vector3.zero;

    private void Awake()
    {
        isEnable = true;
    }
    void Start()
    {
        initBallPos = transform.position;
        GravDirection = GetComponentInParent<Transform>().forward * -1;
        rigidBody = GetComponentInChildren<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(isEnable)
            rigidBody.AddForce(Gravity * GravDirection);
        if (Input.GetKeyDown(KeyCode.Q))
        {
            rigidBody.AddForce(3f, 3f, 3f, ForceMode.Impulse);
        }
    }

    private void OnEnable()
    {
        KHS_Script_ResetController.OnReset += KHS_BallReset;
        KHS_Script_BallOutController.BallOutEvt += KHS_GameOverBall;
        //KHS_Script_ScoreManager.Next_Round_Init += KHS_BallReset;
    }


    private void OnDisable()
    {
        KHS_Script_ResetController.OnReset -= KHS_BallReset;
        KHS_Script_BallOutController.BallOutEvt -= KHS_GameOverBall;
        //KHS_Script_ScoreManager.Next_Round_Init -= KHS_BallReset;
    }

    private void KHS_GameOverBall()
    {
        --BallCount;
        if (BallCount <= 0)
        {
            gameObject.SetActive(false);
            GameOverEvt.Invoke();
        }
        else
            KHS_BallReset();
    }
    private void KHS_BallReset()
    {
        transform.position = initBallPos;
        rigidBody.angularVelocity = Vector3.zero;
        rigidBody.linearVelocity = Vector3.zero;
    }

    public int BallCountResponse()
    {
        return BallCount;
    }
}
