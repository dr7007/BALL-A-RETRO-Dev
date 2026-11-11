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
    public static event Action<int, Vector3> OnScoreAt;
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
            // Capture before callbacks can hide or transport the ball (e.g. slots).
            Vector3 hit = _collider.ClosestPoint(transform.position);
            triggeredBalls.Add(_collider);
            ChargeDemoCapacitor();
            OnBallTrigger?.Invoke(_collider);
            StartCoroutine(ReleaseBall(_collider));

            OnScoreAt?.Invoke(bumpScore, hit);
            OnScore?.Invoke(bumpScore);

            if (isSpecial)
            {
                bumpScore += 5;
            }

            // [ADD] 트리거 충돌 근사 지점 방송
            OnObstacleHitAt?.Invoke(hit);
        }
    }
    private void OnCollisionEnter(Collision _collision)
    {
        if (!_collision.gameObject.CompareTag("Ball")) return;
        Vector3 hit = _collision.transform.position;
        if (_collision.contactCount > 0)
        {
            hit = Vector3.zero;
            for (int i = 0; i < _collision.contactCount; i++) hit += _collision.GetContact(i).point;
            hit /= _collision.contactCount;
        }
        OnBallCollision?.Invoke(_collision);
        Rigidbody ballRb = _collision.rigidbody;
        if (ballRb != null)
        {
            ChargeDemoCapacitor();
            // 충돌 지점의 반대 방향으로 힘을 실어 튕겨냄
            if (_collision.contactCount > 0)
                ballRb.AddForce(-_collision.GetContact(0).normal * bounceForce, ForceMode.Impulse);

            // 여기에 사운드 재생, 파티클 효과 등 추가 가능
            OnScoreAt?.Invoke(bumpScore, hit);
            OnScore?.Invoke(bumpScore);
            if (isSpecial)
            {
                bumpScore += 5;
            }

            // [ADD] 콜리전 정확한 접점 평균 방송
            OnObstacleHitAt?.Invoke(hit);
        }
    }
    private IEnumerator ReleaseBall(Collider col)
    {
        yield return new WaitForSeconds(waitTime);
        triggeredBalls.Remove(col);
    }

    private void ChargeDemoCapacitor()
    {
        var demo = BallARetroDemoRun.Current;
        // Ordinary switches, coins and bonus-map chips also use DumpManager.
        // Only the table's actual illuminated bumpers charge this upgrade.
        if (demo != null && demo.IsEnabled && GetComponent<KHS_Script_BumperEffect>() != null)
            demo.RecordBumperHit();
        var build = BallARetroRunBuild.Current;
        if (build != null && build.IsEnabled && GetComponent<KHS_Script_BumperEffect>() != null)
            build.RecordBumperHit();
    }
}
