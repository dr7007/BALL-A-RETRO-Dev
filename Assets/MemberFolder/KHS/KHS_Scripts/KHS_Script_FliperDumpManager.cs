using System;
using UnityEngine;

public class KHS_Script_FliperDumpManager : MonoBehaviour
{
    public static event Action<Collision> OnFliperCollision;
    public static event Action<Collision> OffFliperCollision;

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

    private void OnCollisionStay(Collision _collision)
    {
        if (!_collision.gameObject.CompareTag("Ball")) return;
        OnFliperCollision?.Invoke(_collision); // 눌렀을 때 이미 접촉 중인 경우를 커버
    }

    private void OnCollisionExit(Collision _collision)
    {
        if (!_collision.gameObject.CompareTag("Ball")) return;
        Debug.Log("OffCollision 판정");
        OffFliperCollision?.Invoke(_collision);
    }
}
