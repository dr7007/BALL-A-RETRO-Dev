using UnityEngine;

public class KHS_Script_BoostController : MonoBehaviour
{
    [SerializeField]
    private Rigidbody ballRb = null;

    public float boostValue = 3.4f;

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Ball"))
            return;
        
        ballRb = other.GetComponent<Rigidbody>();
        ballRb.linearVelocity *= boostValue;
    }
}
