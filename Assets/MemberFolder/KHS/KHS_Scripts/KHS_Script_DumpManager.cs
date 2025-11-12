using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KHS_Script_DumpManager : MonoBehaviour
{
    private HashSet<Collider> triggeredBalls = new HashSet<Collider>();

    public static event Action<Collider> OnBallTrigger;
    public static event Action<Collision> OnBallCollision;

    public static event Action<int> OnScore;
    public float bounceForce = 2f;
    public int bumpScore = 0;
    public float waitTime = 4.5f;

    public bool isSpecial = false;

    // [ADD] 충돌 지점 브로드캐스트(이펙트/사운드용)
    public static event Action<Vector3> OnObstacleHitAt;

    private void OnEnable()
    {
        KHS_Script_BallOutController.BallOutEvt += DumpReset;
    }
    private void OnDisable()
    {
        KHS_Script_BallOutController.BallOutEvt -= DumpReset;
    }

    private void DumpReset()
    {
        YJ_Script_DropTargetController _target = GetComponent<YJ_Script_DropTargetController>();
        if (_target)
        {
            _target.Activate_Object();
        }
        else
            return;
    }

    private void OnTriggerEnter(Collider _collider)
    {
        //if(_collider.CompareTag("Ball"))
        //    OnBallTrigger?.Invoke(_collider);
        if (_collider.CompareTag("Ball") && !triggeredBalls.Contains(_collider))
        {
            triggeredBalls.Add(_collider);
            OnBallTrigger?.Invoke(_collider);
            StartCoroutine(ReleaseBall(_collider));

            OnScore?.Invoke(bumpScore);

            if (isSpecial)
            {
                bumpScore += 5;
            }

            // [ADD] 트리거 충돌 근사 지점 방송
            Vector3 hit = _collider.ClosestPoint(transform.position);
            if (hit == transform.position) hit += transform.forward * 0.01f;
            OnObstacleHitAt?.Invoke(hit);
        }
    }
    private void OnCollisionEnter(Collision _collision)
    {
        if (_collision.gameObject.name == "Ball")
            OnBallCollision?.Invoke(_collision);
        Rigidbody ballRb = _collision.gameObject.GetComponent<Rigidbody>();
        if (ballRb != null)
        {
            // 충돌 지점의 반대 방향으로 힘을 실어 튕겨냄
            Vector3 direction = _collision.contacts[0].normal;
            ballRb.AddForce(-direction * bounceForce, ForceMode.Impulse);

            // 여기에 사운드 재생, 파티클 효과 등 추가 가능
            OnScore.Invoke(bumpScore);
            if (isSpecial)
            {
                bumpScore += 5;
            }

            // [ADD] 콜리전 정확한 접점 평균 방송
            if (_collision.contactCount > 0)
            {
                Vector3 sum = Vector3.zero;
                for (int i = 0; i < _collision.contactCount; i++) sum += _collision.GetContact(i).point;
                Vector3 hit = sum / _collision.contactCount;
                OnObstacleHitAt?.Invoke(hit);
            }
            else
            {
                OnObstacleHitAt?.Invoke(_collision.transform.position);
            }
        }
    }
    private IEnumerator ReleaseBall(Collider col)
    {
        yield return new WaitForSeconds(waitTime);
        triggeredBalls.Remove(col);
    }
}
