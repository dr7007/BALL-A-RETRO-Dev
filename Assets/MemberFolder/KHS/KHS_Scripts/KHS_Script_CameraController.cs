using PSH;
using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-5)]
public class KHS_Script_CameraController : MonoBehaviour
{
    public enum FollowSpace
    {
        CameraAxesOffset,
        WorldSpaceOffset
    }

    public enum ZAxisMode
    {
        WorldZ,
        LocalZ
    }

    // =========================================================
    // Target
    // =========================================================

    [Header("Targets")]
    public Transform ball;
    public string ballTag = "Ball";
    public Camera cam;

    // =========================================================
    // Follow
    // =========================================================

    [Header("Follow")]
    public bool followOnLaunch = true;
    public bool returnOnBallOutOrGameEnd = true;

    public FollowSpace followSpace =
        FollowSpace.CameraAxesOffset;

    [Tooltip("카메라축 기준 오프셋 (Right, Up, Forward)")]
    public Vector3 camAxesOffset =
        new Vector3(0f, 0.9f, -1.8f);

    [Tooltip("월드 좌표 오프셋")]
    public Vector3 worldOffset =
        new Vector3(0f, 1.2f, -1.6f);

    // =========================================================
    // Axis
    // =========================================================

    [Header("Axis Locks / Mode")]
    public bool lockXY = true;
    public bool lockRotation = true;

    public ZAxisMode zMode =
        ZAxisMode.WorldZ;

    // =========================================================
    // Bounds
    // =========================================================

    [Header("Bounds")]

    public bool useMinZClamp = true;

    public float minWorldZ = -2.3f;

    public float minLocalZ = -2.3f;

    // =========================================================
    // Smoothing
    // =========================================================

    [Header("Smoothing")]

    [Min(0f)]
    public float positionSmoothTimeZ = 0.15f;

    [Min(0f)]
    public float lookAtLerpSpeed = 6f;

    public bool lookAtBall = false;


    // =========================================================
    // Zoom
    // =========================================================

    [Header("Zoom (FOV)")]

    public float zoomFOV = 45f;

    [Min(0f)]
    public float zoomInTime = 0.25f;

    [Min(0f)]
    public float zoomOutTime = 0.35f;


    // =========================================================
    // Misc
    // =========================================================

    [Header("Misc")]

    [Range(0f, 1f)]
    public float firstSnapFactor = 0.35f;

    // =========================================================
    // Internal
    // =========================================================

    private Vector3 _defaultPos;
    private Quaternion _defaultRot;

    private float _defaultFOV;

    private bool _defaultSaved;

    private bool _following;

    private bool _temporarilyDisabled;

    private bool _monitorPaused;

    private float _velZ;


    // =========================================================
    // Monitor 이전 상태
    // =========================================================

    private Vector3 _monitorSavedPos;
    private Quaternion _monitorSavedRot;

    private bool _monitorPoseSaved;

    // =========================================================
    // Unity
    // =========================================================

    private void Awake()
    {
        if (cam == null)
            cam = GetComponent<Camera>();

        if (cam == null)
            cam = Camera.main;

        if (ball == null)
            TryFindBall();

        SaveDefaultPose();
    }
    private void OnEnable()
    {
        KHS_Script_PlungerController.OnBallLaunched += HandleLaunched;

        KHS_Script_CameraManager.CameraChangeEvt +=
            ChangeCameraFunc;

        if (returnOnBallOutOrGameEnd)
        {
            KHS_Script_BallOutController.BallOutEvt +=
                ReturnToDefault;

            KHS_Script_ScoreManager.OnGameOver +=
                ReturnToDefault;

            KHS_Script_ScoreManager.OnGameClear +=
                ReturnToDefault;
        }
    }


    private void OnDisable()
    {
        KHS_Script_PlungerController.OnBallLaunched -=
            HandleLaunched;

        KHS_Script_CameraManager.CameraChangeEvt -=
            ChangeCameraFunc;

        if (returnOnBallOutOrGameEnd)
        {
            KHS_Script_BallOutController.BallOutEvt -=
                ReturnToDefault;

            KHS_Script_ScoreManager.OnGameOver -=
                ReturnToDefault;

            KHS_Script_ScoreManager.OnGameClear -=
                ReturnToDefault;
        }
    }
    private void LateUpdate()
    {
        // ★ Monitor / 다른 카메라 연출 중에는
        // Follow가 절대로 Transform을 건드리지 않는다.
        if (_monitorPaused)
            return;

        if (!_following)
            return;

        if (ball == null)
            return;

        Vector3 desiredWorld =
            ComputeDesiredWorldPosition();


        // -----------------------------------------------------
        // World Z
        // -----------------------------------------------------

        if (zMode == ZAxisMode.WorldZ)
        {
            float targetZ =
                desiredWorld.z;

            if (useMinZClamp)
            {
                targetZ =
                    Mathf.Max(targetZ, minWorldZ);
            }

            float newZ =
                Mathf.SmoothDamp(
                    transform.position.z,
                    targetZ,
                    ref _velZ,
                    positionSmoothTimeZ
                );

            if (useMinZClamp)
            {
                newZ =
                    Mathf.Max(newZ, minWorldZ);
            }

            transform.position =
                new Vector3(
                    lockXY
                        ? _defaultPos.x
                        : transform.position.x,

                    lockXY
                        ? _defaultPos.y
                        : transform.position.y,

                    newZ
                );
        }


        // -----------------------------------------------------
        // Local Z
        // -----------------------------------------------------

        else
        {
            Vector3 desiredLocal =
                transform.parent != null
                    ? transform.parent.InverseTransformPoint(
                        desiredWorld)
                    : desiredWorld;

            float targetLZ =
                desiredLocal.z;

            if (useMinZClamp)
            {
                targetLZ =
                    Mathf.Max(targetLZ, minLocalZ);
            }

            float newLZ =
                Mathf.SmoothDamp(
                    transform.localPosition.z,
                    targetLZ,
                    ref _velZ,
                    positionSmoothTimeZ
                );

            if (useMinZClamp)
            {
                newLZ =
                    Mathf.Max(newLZ, minLocalZ);
            }

            transform.localPosition =
                new Vector3(
                    transform.localPosition.x,
                    transform.localPosition.y,
                    newLZ
                );
        }


        // -----------------------------------------------------
        // Rotation
        // -----------------------------------------------------

        if (!lockRotation && lookAtBall)
        {
            Vector3 dir =
                ball.position -
                transform.position;

            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot =
                    Quaternion.LookRotation(
                        dir.normalized,
                        Vector3.up
                    );

                transform.rotation =
                    Quaternion.Slerp(
                        transform.rotation,
                        targetRot,
                        lookAtLerpSpeed *
                        Time.deltaTime
                    );
            }
        }
    }
    // =========================================================
    // Ball Launch
    // =========================================================

    private void HandleLaunched()
    {
        if (!followOnLaunch)
            return;

        if (_monitorPaused)
            return;

        if (ball == null)
            TryFindBall();

        StartFollow();
    }


    // =========================================================
    // Start Follow
    // =========================================================

    public void StartFollow()
    {
        if (ball == null)
            return;

        if (_monitorPaused)
            return;

        SaveDefaultPose();

        Vector3 desiredWorld =
            ComputeDesiredWorldPosition();


        if (zMode == ZAxisMode.WorldZ)
        {
            float snapZ =
                Mathf.Lerp(
                    transform.position.z,
                    desiredWorld.z,
                    firstSnapFactor
                );

            if (useMinZClamp)
            {
                snapZ =
                    Mathf.Max(
                        snapZ,
                        minWorldZ
                    );
            }

            transform.position =
                new Vector3(
                    _defaultPos.x,
                    _defaultPos.y,
                    snapZ
                );
        }
        else
        {
            Vector3 desiredLocal =
                transform.parent != null
                    ? transform.parent.InverseTransformPoint(
                        desiredWorld)
                    : desiredWorld;

            float snapLZ =
                Mathf.Lerp(
                    transform.localPosition.z,
                    desiredLocal.z,
                    firstSnapFactor
                );

            if (useMinZClamp)
            {
                snapLZ =
                    Mathf.Max(
                        snapLZ,
                        minLocalZ
                    );
            }

            transform.localPosition =
                new Vector3(
                    transform.localPosition.x,
                    transform.localPosition.y,
                    snapLZ
                );
        }


        _velZ = 0f;

        _following = true;

        if (cam != null)
        {
            StopCoroutineSafe();

            StartCoroutine(
                CoZoom(
                    cam.fieldOfView,
                    zoomFOV,
                    zoomInTime
                )
            );
        }
    }


    // =========================================================
    // Monitor 진입
    // =========================================================

    public void BeginMonitor()
    {
        Debug.Log(
            "[CameraFollow] BeginMonitor"
        );

        // 현재 카메라의 실제 상태를 저장
        _monitorSavedPos =
            transform.position;

        _monitorSavedRot =
            transform.rotation;

        _monitorPoseSaved = true;


        // Follow 완전 정지
        _following = false;

        _monitorPaused = true;

        _velZ = 0f;

        StopAllCoroutines();
    }


    // =========================================================
    // Monitor 종료 준비
    // =========================================================

    public bool HasMonitorSavedPose()
    {
        return _monitorPoseSaved;
    }


    public Vector3 GetMonitorSavedPosition()
    {
        return _monitorSavedPos;
    }


    public Quaternion GetMonitorSavedRotation()
    {
        return _monitorSavedRot;
    }


    // =========================================================
    // Monitor 종료
    // =========================================================

    public void EndMonitor()
    {
        if (!_monitorPoseSaved)
        {
            Debug.LogWarning(
                "[CameraFollow] 저장된 Monitor Pose가 없습니다."
            );

            _monitorPaused = false;

            return;
        }


        // Monitor 연출이 끝난 후
        // 저장해둔 실제 플레이 카메라 자세를 적용
        transform.position =
            _monitorSavedPos;

        transform.rotation =
            _monitorSavedRot;


        // 이전 SmoothDamp 속도 제거
        _velZ = 0f;


        // 여기서 현재 자세를 새로운 기준 자세로 확정
        _defaultPos =
            _monitorSavedPos;

        _defaultRot =
            _monitorSavedRot;

        _defaultSaved = true;


        // Monitor 상태 종료
        _monitorPoseSaved = false;

        _monitorPaused = false;


        // 중요한 부분
        // 바로 Follow를 켜지 않고 한 프레임 뒤에 켠다.
        StartCoroutine(
            ResumeFollowNextFrame()
        );
    }


    private IEnumerator ResumeFollowNextFrame()
    {
        // CameraManager의 MoveCameraSmooth가
        // 완전히 끝난 뒤 다음 프레임까지 기다림
        yield return null;

        if (ball != null)
        {
            // 현재 올바른 카메라 회전값을 기준으로
            // 공의 위치를 다시 계산
            Vector3 desiredWorld =
                ComputeDesiredWorldPosition();

            if (zMode == ZAxisMode.WorldZ)
            {
                float targetZ =
                    desiredWorld.z;

                if (useMinZClamp)
                {
                    targetZ =
                        Mathf.Max(
                            targetZ,
                            minWorldZ
                        );
                }

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
                    transform.parent != null
                        ? transform.parent.InverseTransformPoint(
                            desiredWorld)
                        : desiredWorld;

                float targetLZ =
                    desiredLocal.z;

                if (useMinZClamp)
                {
                    targetLZ =
                        Mathf.Max(
                            targetLZ,
                            minLocalZ
                        );
                }

                transform.localPosition =
                    new Vector3(
                        transform.localPosition.x,
                        transform.localPosition.y,
                        targetLZ
                    );
            }
        }

        _velZ = 0f;

        _following = true;

        Debug.Log(
            "[CameraFollow] Follow 재개 완료"
        );
    }


    // =========================================================
    // Default Pose 갱신
    // =========================================================

    public void RefreshDefaultPose()
    {
        _defaultPos =
            transform.position;

        _defaultRot =
            transform.rotation;

        if (cam == null)
            cam = GetComponent<Camera>();

        if (cam != null)
        {
            _defaultFOV =
                cam.fieldOfView;
        }

        _defaultSaved = true;

        _velZ = 0f;
    }


    // =========================================================
    // Game End / Ball Out
    // =========================================================

    private void ReturnToDefault()
    {
        if (!_defaultSaved)
            return;

        _following = false;

        _monitorPaused = false;

        StopAllCoroutines();

        if (zMode == ZAxisMode.WorldZ)
        {
            float targetZ =
                _defaultPos.z;

            if (useMinZClamp)
            {
                targetZ =
                    Mathf.Max(
                        targetZ,
                        minWorldZ
                    );
            }

            StartCoroutine(
                CoReturnZ_World(
                    targetZ,
                    zoomOutTime
                )
            );

            if (lockXY)
            {
                transform.position =
                    new Vector3(
                        _defaultPos.x,
                        _defaultPos.y,
                        transform.position.z
                    );
            }
        }
        else
        {
            StartCoroutine(
                CoReturnZ_Local(
                    transform.localPosition.x,
                    transform.localPosition.y,
                    _defaultPos,
                    zoomOutTime
                )
            );
        }


        if (!lockRotation)
        {
            StartCoroutine(
                CoReturnRot(
                    _defaultRot,
                    zoomOutTime
                )
            );
        }


        if (cam != null)
        {
            StartCoroutine(
                CoZoom(
                    cam.fieldOfView,
                    _defaultFOV,
                    zoomOutTime
                )
            );
        }
    }


    // =========================================================
    // Camera Change
    // =========================================================

    private void ChangeCameraFunc(Camera newCamera)
    {
        if (newCamera == null)
            return;

        Debug.Log(
            $"[CameraFollow] Camera Change → {newCamera.name}"
        );

        cam = newCamera;


        // Roulette Camera
        if (newCamera.gameObject.name ==
            "RouletteCamera")
        {
            _following = false;

            _temporarilyDisabled = true;

            StopAllCoroutines();

            return;
        }


        if (_temporarilyDisabled)
        {
            _temporarilyDisabled = false;

            RefreshDefaultPose();
        }


        // Monitor 중이면 자동 Follow 금지
        if (_monitorPaused)
            return;

        StartFollow();
    }


    // =========================================================
    // Helpers
    // =========================================================

    private Vector3 ComputeDesiredWorldPosition()
    {
        if (ball == null)
            return transform.position;

        if (followSpace ==
            FollowSpace.WorldSpaceOffset)
        {
            return ball.position +
                   worldOffset;
        }


        Transform ax =
            transform;

        return ball.position
             + ax.right *
               camAxesOffset.x

             + ax.up *
               camAxesOffset.y

             + ax.forward *
               camAxesOffset.z;
    }


    private void SaveDefaultPose()
    {
        if (_defaultSaved)
            return;

        _defaultPos =
            transform.position;

        _defaultRot =
            transform.rotation;

        if (cam == null)
            cam = Camera.main;

        _defaultFOV =
            cam != null
                ? cam.fieldOfView
                : 60f;

        _defaultSaved = true;
    }


    private void TryFindBall()
    {
        if (!string.IsNullOrEmpty(ballTag))
        {
            GameObject go =
                GameObject.FindWithTag(
                    ballTag
                );

            if (go != null)
                ball = go.transform;
        }


        if (ball == null)
        {
            var bc =
                FindAnyObjectByType<
                    YJ_Script_BallController
                >();

            if (bc != null)
                ball = bc.transform;
        }
    }


    private void StopCoroutineSafe()
    {
        StopAllCoroutines();
    }


    // =========================================================
    // Coroutines
    // =========================================================

    private IEnumerator CoReturnZ_World(
        float targetZ,
        float duration)
    {
        float startZ =
            transform.position.z;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(
                        elapsed / duration
                    )
                );

            float z =
                Mathf.Lerp(
                    startZ,
                    targetZ,
                    t
                );

            if (useMinZClamp)
                z =
                    Mathf.Max(
                        z,
                        minWorldZ
                    );

            transform.position =
                new Vector3(
                    _defaultPos.x,
                    _defaultPos.y,
                    z
                );

            yield return null;
        }

        transform.position =
            new Vector3(
                _defaultPos.x,
                _defaultPos.y,
                targetZ
            );
    }


    private IEnumerator CoReturnZ_Local(
        float keepX,
        float keepY,
        Vector3 defaultWorld,
        float duration)
    {
        float startZ =
            transform.localPosition.z;

        float targetLZ =
            transform.parent != null
                ? transform.parent
                    .InverseTransformPoint(
                        defaultWorld
                    ).z
                : defaultWorld.z;

        if (useMinZClamp)
        {
            targetLZ =
                Mathf.Max(
                    targetLZ,
                    minLocalZ
                );
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(
                        elapsed / duration
                    )
                );

            float z =
                Mathf.Lerp(
                    startZ,
                    targetLZ,
                    t
                );

            if (useMinZClamp)
            {
                z =
                    Mathf.Max(
                        z,
                        minLocalZ
                    );
            }

            transform.localPosition =
                new Vector3(
                    keepX,
                    keepY,
                    z
                );

            yield return null;
        }

        transform.localPosition =
            new Vector3(
                keepX,
                keepY,
                targetLZ
            );
    }


    private IEnumerator CoReturnRot(
        Quaternion targetRot,
        float duration)
    {
        Quaternion start =
            transform.rotation;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(
                        elapsed / duration
                    )
                );

            transform.rotation =
                Quaternion.Slerp(
                    start,
                    targetRot,
                    t
                );

            yield return null;
        }

        transform.rotation =
            targetRot;
    }


    private IEnumerator CoZoom(
        float from,
        float to,
        float duration)
    {
        if (cam == null)
            yield break;

        if (duration <= 0f)
        {
            cam.fieldOfView = to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(
                        elapsed / duration
                    )
                );

            cam.fieldOfView =
                Mathf.Lerp(
                    from,
                    to,
                    t
                );

            yield return null;
        }

        cam.fieldOfView = to;
    }
}