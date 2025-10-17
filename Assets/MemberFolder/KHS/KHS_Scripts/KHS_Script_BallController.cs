using UnityEngine;
using UnityEngine.UIElements;

public class KHS_Script_BallController : MonoBehaviour
{
    [SerializeField]
    private Rigidbody rigidBody = null;
    [SerializeField]
    private float Gravity = 9.8f;
    [SerializeField]
    private Vector3 GravDirection = Vector3.zero;

    private Vector3 initBallPos = Vector3.zero;
    void Start()
    {
        initBallPos = transform.position;
        GravDirection = GetComponentInParent<Transform>().forward * -1;
        rigidBody = GetComponentInChildren<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        rigidBody.AddForce(Gravity * GravDirection);
    }

    private void OnEnable()
    {
        KHS_Script_ResetController.OnReset += KHS_BallReset;
    }
    private void OnDisable()
    {
        KHS_Script_ResetController.OnReset -= KHS_BallReset;
    }

    private void KHS_BallReset()
    {
        transform.position = initBallPos;
        rigidBody.angularVelocity = Vector3.zero;
        rigidBody.linearVelocity = Vector3.zero;
    }
}
