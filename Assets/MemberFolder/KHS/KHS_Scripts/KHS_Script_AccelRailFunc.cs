using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class KHS_Script_AccelRailFunc : MonoBehaviour
{
    public enum RailMode { OneWay, TwoWay }

    [Header("Rail 설정")]
    [SerializeField] private RailMode railMode = RailMode.OneWay;
    [SerializeField] private Transform entryPoint;
    [SerializeField] private Transform controlPoint; // 곡선의 중간(반원 중심) 제어점
    [SerializeField] private Transform exitPoint;

    [Header("물리 설정")]
    [SerializeField] private float accelerationForce = 10f;
    [SerializeField] private float maxSpeed = 15f;
    [SerializeField] private float centeringForce = 4f;
    [SerializeField] private int curveResolution = 30;
    [SerializeField] private float exitDistanceThreshold = 0.5f;

    private readonly HashSet<Rigidbody> ballsInRail = new HashSet<Rigidbody>();
    // 양방향일 때, 공마다 "진입 방향" 기억용
    private readonly Dictionary<Rigidbody, bool> ballEnteredFromEntry = new Dictionary<Rigidbody, bool>();

    private Vector3[] curvePoints;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
        GenerateCurvePoints();
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

    private void FixedUpdate()
    {
        List<Rigidbody> ballsToRemove = new List<Rigidbody>();

        foreach (var ball in ballsInRail)
        {
            if (ball == null) continue;

            Vector3 closest = FindClosestPointOnCurve(ball.position, out int index);
            Vector3 nextPoint;

            bool fromEntry = true;
            if (railMode == RailMode.TwoWay)
                ballEnteredFromEntry.TryGetValue(ball, out fromEntry);

            // --- 탈출 판정 ---
            Vector3 targetExit = fromEntry ? exitPoint.position : entryPoint.position;
            float distanceToExit = Vector3.Distance(ball.position, targetExit);

            if (distanceToExit < exitDistanceThreshold)
            {
                ballsToRemove.Add(ball);
                continue;
            }

            // --- 진행 방향 계산 ---
            if (railMode == RailMode.OneWay)
                nextPoint = index < curvePoints.Length - 1 ? curvePoints[index + 1] : exitPoint.position;
            else
                nextPoint = fromEntry
                    ? (index < curvePoints.Length - 1 ? curvePoints[index + 1] : exitPoint.position)
                    : (index > 0 ? curvePoints[index - 1] : entryPoint.position);

            Vector3 dir = (nextPoint - ball.position).normalized;

            if (ball.linearVelocity.magnitude < maxSpeed)
                ball.AddForce(dir * accelerationForce, ForceMode.Acceleration);

            Vector3 toCenter = (closest - ball.position);
            ball.AddForce(toCenter * centeringForce, ForceMode.Acceleration);
        }

        // --- 탈출 처리 ---
        foreach (var b in ballsToRemove)
        {
            ballsInRail.Remove(b);
            ballEnteredFromEntry.Remove(b);
            Debug.Log($"Ball exited rail (auto): {name}");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball")) return;
        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        if (!ballsInRail.Contains(rb))
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

        ballsInRail.Remove(rb);
        ballEnteredFromEntry.Remove(rb);
        Debug.Log($"Ball exited rail [{name}]");
    }

    /// <summary>
    /// 가장 가까운 곡선상의 점을 찾음
    /// </summary>
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


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

}