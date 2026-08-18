using PSH;
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
    public bool lockXY = true;            // X,Y 고정
    public bool lockRotation = true;      // 회전 고정
    public ZAxisMode zMode = ZAxisMode.WorldZ;

    [Header("Bounds")]
    [Tooltip("Z 하한선을 적용할지")]
    public bool useMinZClamp = true;
    [Tooltip("WorldZ 모드일 때 사용할 최소 Z(이 값보다 작아지지 않음)")]
    public float minWorldZ = -2.3f;
    [Tooltip("LocalZ 모드일 때 사용할 최소 Z")]
    public float minLocalZ = -2.3f;

    [Header("Smoothing")]
    [Min(0f)] public float positionSmoothTimeZ = 0.15f;
    [Min(0f)] public float lookAtLerpSpeed = 6f;
    public bool lookAtBall = false;       // 회전 고정이 기본이므로 false

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

    private bool _temporarilyDisabled = false; // 일시 비활성화 상태 플래그

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        SaveDefaultPose();
        if (ball == null) TryFindBall();
    }

    void OnEnable()
    {
        KHS_Script_PlungerController.OnBallLaunched += HandleLaunched;
        KHS_Script_CameraManager.CameraChangeEvt += ChangeCameraFunc;
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
        KHS_Script_CameraManager.CameraChangeEvt -= ChangeCameraFunc;
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

        Vector3 desiredWorld = ComputeDesiredWorldPosition();

        if (zMode == ZAxisMode.WorldZ)
        {
            float targetZ = desiredWorld.z;
            if (useMinZClamp) targetZ = Mathf.Max(targetZ, minWorldZ);             // ★ 월드Z 하한
            float newZ = Mathf.SmoothDamp(transform.position.z, targetZ, ref _velZ, positionSmoothTimeZ);

            if (useMinZClamp) newZ = Mathf.Max(newZ, minWorldZ);                   // ★ 보간값도 클램프
            transform.position = new Vector3(
                lockXY ? _defaultPos.x : transform.position.x,
                lockXY ? _defaultPos.y : transform.position.y,
                newZ
            );
        }
        else // LocalZ
        {
            Vector3 desiredLocal = transform.parent ? transform.parent.InverseTransformPoint(desiredWorld) : desiredWorld;
            float targetLZ = desiredLocal.z;
            if (useMinZClamp) targetLZ = Mathf.Max(targetLZ, minLocalZ);           // ★ 로컬Z 하한
            float newLZ = Mathf.SmoothDamp(transform.localPosition.z, targetLZ, ref _velZ, positionSmoothTimeZ);
            if (useMinZClamp) newLZ = Mathf.Max(newLZ, minLocalZ);

            transform.localPosition = new Vector3(
                transform.localPosition.x,  // lockXY=true라도 로컬X/Y는 유지(이미 고정 효과)
                transform.localPosition.y,
                newLZ
            );
        }

        if (!lockRotation && lookAtBall)
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

        Vector3 desiredWorld = ComputeDesiredWorldPosition();

        if (zMode == ZAxisMode.WorldZ)
        {
            float snapZ = Mathf.Lerp(transform.position.z, desiredWorld.z, firstSnapFactor);
            if (useMinZClamp) snapZ = Mathf.Max(snapZ, minWorldZ);                 // ★ 스냅 시 클램프
            transform.position = new Vector3(_defaultPos.x, _defaultPos.y, snapZ);
        }
        else
        {
            Vector3 desiredLocal = transform.parent ? transform.parent.InverseTransformPoint(desiredWorld) : desiredWorld;
            float snapLZ = Mathf.Lerp(transform.localPosition.z, desiredLocal.z, firstSnapFactor);
            if (useMinZClamp) snapLZ = Mathf.Max(snapLZ, minLocalZ);               // ★ 스냅 시 클램프
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, snapLZ);
        }

        if (cam != null) StartCoroutine(CoZoom(cam.fieldOfView, zoomFOV, zoomInTime));
        _following = true;
    }

    private void ReturnToDefault()
    {
        if (!_defaultSaved) return;

        _following = false;
        StopAllCoroutines();

        if (zMode == ZAxisMode.WorldZ)
        {
            float targetZ = _defaultPos.z;
            if (useMinZClamp) targetZ = Mathf.Max(targetZ, minWorldZ);             // ★ 복귀 목표도 클램프
            StartCoroutine(CoReturnZ_World(targetZ, zoomOutTime));
            if (lockXY) transform.position = new Vector3(_defaultPos.x, _defaultPos.y, transform.position.z);
        }
        else
        {
            StartCoroutine(CoReturnZ_Local(transform.localPosition.x, transform.localPosition.y, _defaultPos, zoomOutTime));
        }

        if (!lockRotation)
            StartCoroutine(CoReturnRot(_defaultRot, zoomOutTime));

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
            if (useMinZClamp) z = Mathf.Max(z, minWorldZ);                          // ★
            transform.position = new Vector3(_defaultPos.x, _defaultPos.y, z);
            yield return null;
        }
        float fin = useMinZClamp ? Mathf.Max(targetZ, minWorldZ) : targetZ;
        transform.position = new Vector3(_defaultPos.x, _defaultPos.y, fin);
    }

    private IEnumerator CoReturnZ_Local(float keepX, float keepY, Vector3 defWorld, float t)
    {
        float startZ = transform.localPosition.z;
        float targetLZ = transform.parent ? transform.parent.InverseTransformPoint(defWorld).z : defWorld.z;
        if (useMinZClamp) targetLZ = Mathf.Max(targetLZ, minLocalZ);                // ★

        float elapsed = 0f;
        while (elapsed < t)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / t));
            float z = Mathf.Lerp(startZ, targetLZ, s);
            if (useMinZClamp) z = Mathf.Max(z, minLocalZ);                          // ★
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
            var bc = FindAnyObjectByType<YJ_Script_BallController>();
            if (bc != null) ball = bc.transform;
        }
    }
    private void ChangeCameraFunc(Camera _cam)
    {
        Debug.LogError($"ChangeCameraFunc {_cam.name}");
        cam = _cam;

        // 룰렛 카메라일 경우 - 기능 일시 정지
        if (cam != null && cam.gameObject.name == "RouletteCamera")
        {
            // 팔로우 중이라면 멈추고 상태 저장
            if (_following)
            {
                _following = false;
                StopAllCoroutines();
            }
            _temporarilyDisabled = true;
            return;
        }

        // 다른 카메라가 들어올 때 - 다시 기능 복귀
        if (_temporarilyDisabled)
        {
            _temporarilyDisabled = false;
            SaveDefaultPose(); // 혹시 위치 초기화를 보장하기 위해
        }

        // 정상적인 카메라 전환 시 팔로우 재개
        StartFollow();
    }

    // ── KHS Write ────────────────────────────────────────────────────────────────
    public void PauseFollowForMonitor()
    {
        _following = false;
        _velZ = 0f;

        StopAllCoroutines();
    }


    public void ResumeFollowAfterMonitor(
        Vector3 restoredPosition,
        Quaternion restoredRotation)
    {
        // 카메라의 연출 이전 상태 복구
        transform.position = restoredPosition;
        transform.rotation = restoredRotation;

        // SmoothDamp의 이전 속도 제거
        _velZ = 0f;

        // 현재 카메라 회전값을 기준으로
        // Follow 위치를 다시 계산
        if (ball != null)
        {
            Vector3 desiredWorld =
                ComputeDesiredWorldPosition();

            if (zMode == ZAxisMode.WorldZ)
            {
                float targetZ = desiredWorld.z;

                if (useMinZClamp)
                    targetZ = Mathf.Max(targetZ, minWorldZ);

                transform.position =
                    new Vector3(
                        _defaultPos.x,
                        _defaultPos.y,
                        targetZ
                    );
            }
            else
            {
                Vector3 desiredLocal =
                    transform.parent
                        ? transform.parent.InverseTransformPoint(desiredWorld)
                        : desiredWorld;

                float targetLZ = desiredLocal.z;

                if (useMinZClamp)
                    targetLZ = Mathf.Max(targetLZ, minLocalZ);

                transform.localPosition =
                    new Vector3(
                        transform.localPosition.x,
                        transform.localPosition.y,
                        targetLZ
                    );
            }
        }

        // 마지막에 Follow 활성화
        _following = true;
    }
    public void RefreshDefaultPose()
    {
        _defaultPos = transform.position;
        _defaultRot = transform.rotation;

        if (cam == null)
            cam = GetComponent<Camera>();

        if (cam != null)
            _defaultFOV = cam.fieldOfView;

        _defaultSaved = true;

        _velZ = 0f;
    }
}
