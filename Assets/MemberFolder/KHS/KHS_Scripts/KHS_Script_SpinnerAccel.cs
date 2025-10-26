using UnityEngine;

public class KHS_Script_SpinnerAccel : MonoBehaviour
{
    [SerializeField]
    private Rigidbody rb;

    public float additiveSpeed = 2f;

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
        if (!other.CompareTag("Ball"))
            return;
        Debug.Log("잘 동작함");
        rb = other.GetComponent<Rigidbody>();
        rb.linearVelocity = rb.linearVelocity * additiveSpeed;
    }
}
