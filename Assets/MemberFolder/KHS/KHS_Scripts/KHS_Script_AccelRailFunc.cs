using System.Collections.Generic;
using UnityEngine;

public class KHS_Script_AccelRailFunc : MonoBehaviour
{
    public enum RailMode { OneWay, TwoWay }

    [Header("Rail 설정")]
    [SerializeField] private RailMode railMode = RailMode.OneWay;
    [SerializeField] private Transform entryPoint;
    [SerializeField] private Transform controlPoint; // 곡선의 중심 제어점
    [SerializeField] private Transform exitPoint;

    [Header("물리 설정")]
    [SerializeField] private float accelerationForce = 10f;
    [SerializeField] private float maxSpeed = 15f;
    [SerializeField] private float centeringForce = 4f;
    [SerializeField] private int curveResolution = 30;
    [SerializeField] private float exitDistanceThreshold = 0.5f;

    [Header("재진입 보호 설정")]
    [SerializeField] private float releaseTime = 0.3f; // 탈출 후 재진입 금지 시간

    // 내부 상태
    private Collider[] colliders;
    private Vector3[] curvePoints;
    private readonly HashSet<Rigidbody> ballsInRail = new HashSet<Rigidbody>();
    private readonly Dictionary<Rigidbody, bool> ballEnteredFromEntry = new Dictionary<Rigidbody, bool>();
    private readonly Dictionary<Rigidbody, int> ballColliderContactCount = new Dictionary<Rigidbody, int>();
    private readonly Dictionary<Rigidbody, float> recentlyReleasedBalls = new Dictionary<Rigidbody, float>();


    private void Awake()
    {
        colliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders)
            c.isTrigger = true;

        GenerateCurvePoints();
    }

    private float releaseCheckTimer = 0f;
    private void Update()
    {
        releaseCheckTimer += Time.deltaTime;
        if (releaseCheckTimer < 0.1f) return; // 0.1초마다만 검사
        releaseCheckTimer = 0f;

        List<Rigidbody> toRemove = null;
        foreach (var kvp in recentlyReleasedBalls)
        {
            if (Time.time - kvp.Value > releaseTime)
            {
                toRemove ??= new List<Rigidbody>();
                toRemove.Add(kvp.Key);
            }
        }
        if (toRemove != null)
            foreach (var rb in toRemove) recentlyReleasedBalls.Remove(rb);
    }

    private void GenerateCurvePoints()
    {
        curvePoints = new Vector3[curveResolution];
        for (int i = 0; i < curveResolution; i++)
        {
            float t = i / (curveResolution - 1f);
            // Quadratic Bezier
            curvePoints[i] = Mathf.Pow(1 - t, 2) * entryPoint.position +
                             2 * (1 - t) * t * controlPoint.position +
                             Mathf.Pow(t, 2) * exitPoint.position;
        }
    }
    /// <summary>
    /// Bezier 곡선의 방향(미분값)을 계산해 레일 방향을 부드럽게 추적
    /// </summary>
    private Vector3 GetCurveDirection(int index, bool fromEntry)
    {
        int nextIndex = fromEntry ? Mathf.Min(index + 1, curvePoints.Length - 1)
                                  : Mathf.Max(index - 1, 0);

        if (index == nextIndex)
            return (fromEntry ? exitPoint.position - curvePoints[index]
                              : entryPoint.position - curvePoints[index]).normalized;

        return (curvePoints[nextIndex] - curvePoints[index]).normalized;
    }


    private void FixedUpdate()
    {
        List<Rigidbody> ballsToRemove = new List<Rigidbody>();

        foreach (var ball in ballsInRail)
        {
            if (ball == null) continue;

            Vector3 closest = FindClosestPointOnCurve(ball.position, out int index);

            bool fromEntry = true;
            if (railMode == RailMode.TwoWay)
                ballEnteredFromEntry.TryGetValue(ball, out fromEntry);

            // --- 진행 방향 벡터 계산 개선 ---
            // Bezier 상의 "다음 목표점" 대신, 곡선의 "미분 벡터"를 사용해 자연스러운 진행방향 확보
            Vector3 dir = GetCurveDirection(index, fromEntry).normalized;

            // --- 탈출 판정 ---
            Vector3 targetExit = fromEntry ? exitPoint.position : entryPoint.position;
            float distanceToExit = Vector3.Distance(ball.position, targetExit);
            if (distanceToExit < exitDistanceThreshold)
            {
                ballsToRemove.Add(ball);
                continue;
            }

            // --- 가속 처리 ---
            if (ball.linearVelocity.magnitude < maxSpeed)
                ball.AddForce(dir * accelerationForce, ForceMode.Acceleration);

            // --- 레일 중심으로 정렬 (보정 강도 완화 및 방향 제한) ---
            Vector3 toCenter = (closest - ball.position);
            float distance = toCenter.magnitude;
            if (distance > 0.001f)
            {
                Vector3 centeringDir = toCenter.normalized;
                ball.AddForce(centeringDir * centeringForce * Mathf.Clamp01(distance), ForceMode.Acceleration);
            }

        }

        // --- 탈출 처리 ---
        foreach (var b in ballsToRemove)
        {
            ballsInRail.Remove(b);
            ballEnteredFromEntry.Remove(b);
            recentlyReleasedBalls[b] = Time.time;
            Debug.Log($"Ball exited rail [{name}] - release lock started");
        }
    }

    private void OnTriggerEnter(Collider other)
    {

        if (!other.CompareTag("Ball")) return;
        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;
        if (recentlyReleasedBalls.TryGetValue(rb, out float t))
        {
            if (Time.time - t < releaseTime)
                return;
        }

 
        // ReleaseTime 중이면 무시
        if (recentlyReleasedBalls.ContainsKey(rb))
            return;

        // --- OneWay일 경우 진입 방향 검사 + 반발 처리 ---
        if (railMode == RailMode.OneWay)
        {
            Vector3 railDir = (exitPoint.position - entryPoint.position).normalized;
            Vector3 incomingDir = (rb.position - entryPoint.position).normalized;

            float dot = Vector3.Dot(railDir, incomingDir);

            // 반대 방향에서 들어왔을 경우
            if (dot < 0.2f)
            {
                Debug.Log($"Ball tried to enter [{name}] from wrong direction — applying bounce-back force.");

                // 반대 방향으로 부드럽게 밀어냄
                Vector3 pushDir = (rb.position - entryPoint.position).normalized;
                rb.AddForce(pushDir * 25f, ForceMode.Acceleration);

                // 너무 과하게 튀는 걸 방지하기 위해 기존 속도 감속
                rb.linearVelocity *= 0.5f;

                return;
            }
        }


        // 콜라이더 접촉 카운트 관리
        if (!ballColliderContactCount.ContainsKey(rb))
            ballColliderContactCount[rb] = 0;
        ballColliderContactCount[rb]++;

        // 첫 진입일 때만 처리
        if (ballColliderContactCount[rb] == 1)
        {
            ballsInRail.Add(rb);

            if (railMode == RailMode.TwoWay)
            {
                float distToEntry = Vector3.Distance(rb.position, entryPoint.position);
                float distToExit = Vector3.Distance(rb.position, exitPoint.position);
                bool fromEntry = distToEntry < distToExit;

                ballEnteredFromEntry[rb] = fromEntry;
                Debug.Log($"Ball entered rail [{name}] from {(fromEntry ? "ENTRY" : "EXIT")}");
            }
            else
            {
                Debug.Log($"Ball entered rail [{name}]");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Ball")) return;
        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        if (!ballColliderContactCount.ContainsKey(rb)) return;

        ballColliderContactCount[rb]--;

        // 모든 콜라이더에서 벗어난 경우만 완전 탈출 처리
        if (ballColliderContactCount[rb] <= 0)
        {
            ballColliderContactCount.Remove(rb);

            if (ballsInRail.Contains(rb))
            {
                ballsInRail.Remove(rb);
                ballEnteredFromEntry.Remove(rb);

                recentlyReleasedBalls[rb] = Time.time;
                Debug.Log($"Ball fully exited rail [{name}] - release lock started");
            }
        }
    }

    private Vector3 FindClosestPointOnCurve(Vector3 pos, out int index)
    {
        float minDist = float.MaxValue;
        Vector3 closest = Vector3.zero;
        index = 0;

        for (int i = 0; i < curvePoints.Length; i++)
        {
            float dist = Vector3.SqrMagnitude(pos - curvePoints[i]);
            if (dist < minDist)
            {
                minDist = dist;
                closest = curvePoints[i];
                index = i;
            }
        }
        return closest;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (entryPoint == null || controlPoint == null || exitPoint == null) return;

        Vector3 prev = entryPoint.position;
        Gizmos.color = Color.cyan;
        for (int i = 1; i <= 30; i++)
        {
            float t = i / 30f;
            Vector3 point = Mathf.Pow(1 - t, 2) * entryPoint.position +
                            2 * (1 - t) * t * controlPoint.position +
                            Mathf.Pow(t, 2) * exitPoint.position;
            Gizmos.DrawLine(prev, point);
            prev = point;
        }
    }
#endif
}
