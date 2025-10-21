using System;
using UnityEngine;

public class KHS_Script_FliperDumpManager : MonoBehaviour
{
    public static event Action<Collision> OnFliperCollision;

    [SerializeField] private KHS_Script_FliperController flipperController; // 참조 연결
    [SerializeField] private Rigidbody flipperRigidbody; // 플리퍼 자체의 Rigidbody

    private void Awake()
    {
        if (flipperRigidbody == null)
            flipperRigidbody = GetComponent<Rigidbody>();

        if (flipperController == null)
            flipperController = FindAnyObjectByType<KHS_Script_FliperController>();
    }

    private void OnCollisionEnter(Collision _collision)
    {
        if (_collision.gameObject.name == "Ball")
            OnFliperCollision?.Invoke(_collision);
    }
    private void OnEnable()
    {
        KHS_Script_FliperDumpManager.OnFliperCollision += OnFliperShot;
    }


    private void OnDisable()
    {
        KHS_Script_FliperDumpManager.OnFliperCollision -= OnFliperShot;
    }
    private void OnFliperShot(Collision _collision)
    {
        // Issue : 플리퍼 누른상태로 떼면 반대로 날아가는 기현상처리 생각중
        /*Rigidbody ballRb = _collision.rigidbody;
        if (ballRb == null || flipperRigidbody == null) return;

        // 충돌 정보 가져오기
        ContactPoint contact = _collision.GetContact(0);
        Vector3 contactPoint = contact.point;
        Vector3 normal = contact.normal;

        // 플리퍼 회전 중심에서 충돌 지점까지의 벡터
        Vector3 flipperCenter = flipperRigidbody.worldCenterOfMass;
        Vector3 radius = contactPoint - flipperCenter;

        // 플리퍼 회전 각속도 (rad/s)
        Vector3 angularVelocity = flipperRigidbody.angularVelocity;

        // 충돌 지점의 선형속도 (v = ω × r)
        Vector3 flipperLinearVelocity = Vector3.Cross(angularVelocity, radius);

        // 플리퍼의 회전 방향에 따라 공 반사 방향 계산
        Vector3 hitDir = Vector3.Reflect(flipperLinearVelocity.normalized, normal).normalized;

        // 힘의 크기 계산
        float impactPower = flipperLinearVelocity.magnitude * flipperController.impactForceMultiplier;

        // 공에 순간적인 힘 가하기 (Impulse)
        ballRb.AddForce(hitDir * impactPower, ForceMode.Impulse);

        Debug.DrawRay(contactPoint, hitDir * 2f, Color.red, 2f);
        Debug.Log($"플리퍼 '{name}' 충돌! 반사힘 {impactPower:F1}, 방향 {hitDir}");*/
    }
}

