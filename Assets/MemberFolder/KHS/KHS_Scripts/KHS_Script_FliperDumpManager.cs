using System;
using UnityEngine;

public class KHS_Script_FliperDumpManager : MonoBehaviour
{
    public static event Action<Collision> OnFliperCollision;

    [SerializeField] private KHS_Script_FliperController flipperController;
    [SerializeField] private Rigidbody flipperRigidbody;
    
    private void Start()
    {
        if (flipperRigidbody == null)
            flipperRigidbody = GetComponent<Rigidbody>();

        if (flipperController == null)
            flipperController = FindAnyObjectByType<KHS_Script_FliperController>();
    }

    private void OnCollisionEnter(Collision _collision)
    {
        if (!_collision.gameObject.CompareTag("Ball")) return;

        OnFliperCollision?.Invoke(_collision);

    }

    private void OnEnable() { OnFliperCollision += OnFliperShot; }
    private void OnDisable() { OnFliperCollision -= OnFliperShot; }

    private void OnFliperShot(Collision _collision)
    {
        Debug.Log("OnFliperShot 조건");
    }
}