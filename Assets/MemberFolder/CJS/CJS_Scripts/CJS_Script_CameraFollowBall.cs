using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-5)]
public class CJS_Script_CameraFollowBall : MonoBehaviour
{
    public enum FollowSpace { CameraAxesOffset, WorldSpaceOffset }
    public enum ZAxisMode { WorldZ, LocalZ }  

    [Header("Targets")]
    public Transform ball;
    public string ballTag = "Ball";
    public Camera cam;

    [Header("Follow")]
    public bool followOnLaunch = true;
    public bool returnOnBallOutOrGameEnd = true;
    public FollowSpace followSpace = FollowSpace.CameraAxesOffset;

    [Tooltip("카메라축 기준 오프셋 (Right, Up, Forward)")]
    public Vector3 camAxesOffset = new Vector3(0f, 0.9f, -1.8f);
    [Tooltip("월드 좌표 오프셋")]
    public Vector3 worldOffset = new Vector3(0f, 1.2f, -1.6f);

    [Header("Axis Locks / Mode")]
    [Tooltip("포지션 X,Y를 고정(변경하지 않음)")]
    public bool lockXY = true;  
    [Tooltip("회전(X,Y,Z)을 고정(변경하지 않음)")]
    public bool lockRotation = true;    
    [Tooltip("Z만 변경 시, 월드Z로 움직일지(기본) 로컬Z로 움직일지")]
    public ZAxisMode zMode = ZAxisMode.WorldZ; 

    [Header("Smoothing")]
    [Min(0f)] public float positionSmoothTimeZ = 0.15f;
    [Min(0f)] public float lookAtLerpSpeed = 6f;       
    public bool lookAtBall = false;                    

    [Header("Zoom (FOV)")]
    public float zoomFOV = 45f;
    [Min(0f)] public float zoomInTime = 0.25f;
    [Min(0f)] public float zoomOutTime = 0.35f;

    [Header("Misc")]
    [Range(0f, 1f)] public float firstSnapFactor = 0.35f;

    // internal
    private Vector3 _defaultPos;
    private Quaternion _defaultRot;
    private float _defaultFOV;
    private bool _defaultSaved;

    private bool _following;
    private float _velZ;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        SaveDefaultPose();
        if (ball == null) TryFindBall();
    }

    void OnEnable()
    {
        KHS_Script_PlungerController.OnBallLaunched += HandleLaunched;

        if (returnOnBallOutOrGameEnd)
        {
            KHS_Script_BallOutController.BallOutEvt += ReturnToDefault;
            KHS_Script_ScoreManager.OnGameOver += ReturnToDefault;
            KHS_Script_ScoreManager.OnGameClear += ReturnToDefault;
        }
    }
    void OnDisable()
    {
        KHS_Script_PlungerController.OnBallLaunched -= HandleLaunched;

        if (returnOnBallOutOrGameEnd)
        {
            KHS_Script_BallOutController.BallOutEvt -= ReturnToDefault;
            KHS_Script_ScoreManager.OnGameOver -= ReturnToDefault;
            KHS_Script_ScoreManager.OnGameClear -= ReturnToDefault;
        }
    }

    void LateUpdate()
    {
        if (!_following || ball == null) return;

        // 1) 목표 위치 계산(전체 3D 위치)
        Vector3 desiredWorld = ComputeDesiredWorldPosition();

        // 2) Z만 보간하여 적용
        if (zMode == ZAxisMode.WorldZ)
        {
            float targetZ = desiredWorld.z;
            float newZ = Mathf.SmoothDamp(transform.position.z, targetZ, ref _velZ, positionSmoothTimeZ);

            if (lockXY)
            {
                // X,Y는 초기값(기본 포즈) 유지
                transform.position = new Vector3(_defaultPos.x, _defaultPos.y, newZ);
            }
            else
            {
                // X,Y는 현 위치 유지
                transform.position = new Vector3(transform.position.x, transform.position.y, newZ);
            }
        }
        else // LocalZ
        {
            Vector3 desiredLocal = transform.parent
                ? transform.parent.InverseTransformPoint(desiredWorld)
                : desiredWorld; // 부모가 없으면 world==local

            float targetLZ = desiredLocal.z;
            Vector3 lp = transform.localPosition;
            float newLZ = Mathf.SmoothDamp(lp.z, targetLZ, ref _velZ, positionSmoothTimeZ);

            if (lockXY)
                transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, newLZ);
            else
                transform.localPosition = new Vector3(desiredLocal.x, desiredLocal.y, newLZ);
        }

        // 3) 회전 잠금/옵션
        if (lockRotation)
        {
            // 고정 유지: 아무것도 하지 않음
        }
        else if (lookAtBall)
        {
            Vector3 dir = (ball.position - transform.position);
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, lookAtLerpSpeed * Time.deltaTime);
            }
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────
    private void HandleLaunched()
    {
        if (!followOnLaunch) return;
        if (ball == null) TryFindBall();

        StartFollow();
    }

    private void StartFollow()
    {
        if (ball == null) return;

        SaveDefaultPose();

        // 시작 스냅: Z만 보정
        Vector3 desiredWorld = ComputeDesiredWorldPosition();

        if (zMode == ZAxisMode.WorldZ)
        {
            float snapZ = Mathf.Lerp(transform.position.z, desiredWorld.z, firstSnapFactor);
            transform.position = new Vector3(_defaultPos.x, _defaultPos.y, snapZ);
        }
        else
        {
            Vector3 desiredLocal = transform.parent
                ? transform.parent.InverseTransformPoint(desiredWorld)
                : desiredWorld;

            float snapLZ = Mathf.Lerp(transform.localPosition.z, desiredLocal.z, firstSnapFactor);
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, snapLZ);
        }

        // FOV 줌인
        if (cam != null) StartCoroutine(CoZoom(cam.fieldOfView, zoomFOV, zoomInTime));

        _following = true;
    }

    private void ReturnToDefault()
    {
        if (!_defaultSaved) return;

        _following = false;
        StopAllCoroutines();

        // 위치 복귀: Z만 복구
        if (zMode == ZAxisMode.WorldZ)
        {
            StartCoroutine(CoReturnZ_World(_defaultPos.z, zoomOutTime));
            if (lockXY) { transform.position = new Vector3(_defaultPos.x, _defaultPos.y, transform.position.z); }
        }
        else
        {
            StartCoroutine(CoReturnZ_Local(transform.localPosition.x, transform.localPosition.y, _defaultPos, zoomOutTime));
        }

        // 회전은 잠금 상태면 그대로, 잠금 해제 상태라면 기본 회전으로
        if (!lockRotation)
            StartCoroutine(CoReturnRot(_defaultRot, zoomOutTime));

        // FOV 복귀
        if (cam != null) StartCoroutine(CoZoom(cam.fieldOfView, _defaultFOV, zoomOutTime));
    }

    private IEnumerator CoReturnZ_World(float targetZ, float t)
    {
        float startZ = transform.position.z;
        float elapsed = 0f;
        while (elapsed < t)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / t));
            float z = Mathf.Lerp(startZ, targetZ, s);

            if (lockXY)
                transform.position = new Vector3(_defaultPos.x, _defaultPos.y, z);
            else
                transform.position = new Vector3(transform.position.x, transform.position.y, z);

            yield return null;
        }
        transform.position = new Vector3(_defaultPos.x, _defaultPos.y, targetZ);
    }

    private IEnumerator CoReturnZ_Local(float keepX, float keepY, Vector3 defWorld, float t)
    {
        float startZ = transform.localPosition.z;
        float targetLZ = transform.parent
            ? transform.parent.InverseTransformPoint(defWorld).z
            : defWorld.z;

        float elapsed = 0f;
        while (elapsed < t)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / t));
            float z = Mathf.Lerp(startZ, targetLZ, s);
            transform.localPosition = new Vector3(keepX, keepY, z);
            yield return null;
        }
        transform.localPosition = new Vector3(keepX, keepY, targetLZ);
    }

    private IEnumerator CoReturnRot(Quaternion rot, float t)
    {
        Quaternion start = transform.rotation;
        float elapsed = 0f;
        while (elapsed < t)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / t));
            transform.rotation = Quaternion.Slerp(start, rot, s);
            yield return null;
        }
        transform.rotation = rot;
    }

    private IEnumerator CoZoom(float from, float to, float t)
    {
        if (cam == null) yield break;
        float elapsed = 0f;
        while (elapsed < t)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / t));
            cam.fieldOfView = Mathf.Lerp(from, to, s);
            yield return null;
        }
        cam.fieldOfView = to;
    }

    private Vector3 ComputeDesiredWorldPosition()
    {
        if (followSpace == FollowSpace.WorldSpaceOffset)
            return ball.position + worldOffset;

        // CameraAxesOffset → 현재 카메라축 기준
        Transform ax = transform;
        return ball.position
             + ax.right * camAxesOffset.x
             + ax.up * camAxesOffset.y
             + ax.forward * camAxesOffset.z;
    }

    private void SaveDefaultPose()
    {
        if (_defaultSaved) return;
        _defaultPos = transform.position;
        _defaultRot = transform.rotation;
        if (cam == null) cam = Camera.main;
        _defaultFOV = cam != null ? cam.fieldOfView : 60f;
        _defaultSaved = true;
    }

    private void TryFindBall()
    {
        if (!string.IsNullOrEmpty(ballTag))
        {
            var go = GameObject.FindWithTag(ballTag);
            if (go != null) ball = go.transform;
        }
        if (ball == null)
        {
            var bc = FindAnyObjectByType<KHS_Script_BallController>();
            if (bc != null) ball = bc.transform;
        }
    }
}
