using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-5)]
public class KHS_Script_CameraController : MonoBehaviour
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
    public Vector3 camAxesOffset = new Vector3(0f, .9f, -1.8f);
    public Vector3 worldOffset = new Vector3(0f, 1.2f, -1.6f);
    [Header("Axis Locks / Mode")]
    public bool lockXY = true;
    public bool lockRotation = true;
    public ZAxisMode zMode = ZAxisMode.WorldZ;
    [Header("Bounds")]
    public bool useMinZClamp = true;
    public float minWorldZ = -2.3f;
    public float minLocalZ = -2.3f;
    // The gameplay board ends near world Z 7.3; its rig follows at Z - 1.8.
    // This leaves margin beyond the upper rail without following an escaped ball.
    public bool useMaxZClamp = true;
    public float maxWorldZ = 6f;
    public float maxLocalZ = 6f;
    [Header("Smoothing")]
    [Min(0f)] public float positionSmoothTimeZ = .15f;
    [Min(0f)] public float lookAtLerpSpeed = 6f;
    public bool lookAtBall;
    [Header("Zoom (FOV)")]
    public float zoomFOV = 45f;
    [Min(0f)] public float zoomInTime = .25f;
    [Min(0f)] public float zoomOutTime = .35f;
    [Header("Misc")]
    [Range(0f, 1f)] public float firstSnapFactor = .35f;

    Vector3 _defaultPos, _defaultLocalPos, _monitorSavedPos;
    Quaternion _defaultRot, _monitorSavedRot;
    float _defaultFOV, _velZ;
    bool _defaultSaved, _following, _temporarilyDisabled, _resumeAfterCameraChange, _returningToDefault;
    bool _monitorPaused, _monitorPoseSaved, _monitorWasFollowing, _returnAfterMonitor;
    bool _focusPaused, _focusWasFollowing, _focusLaunchRequested, _returnAfterFocus;
    readonly Dictionary<Camera, float> _cameraFovs = new Dictionary<Camera, float>();

    void Awake()
    {
        if (!cam) cam = GetComponent<Camera>();
        if (!cam) cam = Camera.main;
        if (!ball) TryFindBall();
        SaveDefaultPose();
    }
    void OnEnable()
    {
        KHS_Script_PlungerController.OnBallLaunched += HandleLaunched;
        KHS_Script_CameraManager.CameraChangeEvt += ChangeCameraFunc;
        if (!returnOnBallOutOrGameEnd) return;
        KHS_Script_BallOutController.BallOutEvt += ReturnToDefault;
        KHS_Script_ScoreManager.OnGameOver += ReturnToDefault;
        KHS_Script_ScoreManager.OnGameClear += ReturnToDefault;
    }
    void OnDisable()
    {
        KHS_Script_PlungerController.OnBallLaunched -= HandleLaunched;
        KHS_Script_CameraManager.CameraChangeEvt -= ChangeCameraFunc;
        KHS_Script_BallOutController.BallOutEvt -= ReturnToDefault;
        KHS_Script_ScoreManager.OnGameOver -= ReturnToDefault;
        KHS_Script_ScoreManager.OnGameClear -= ReturnToDefault;
        StopAllCoroutines();
    }
    void LateUpdate()
    {
        if (_monitorPaused || _focusPaused || _temporarilyDisabled || !_following || !ball) return;
        if (lockRotation) transform.rotation = _defaultRot;
        Vector3 desired = ComputeDesiredWorldPosition();
        bool local = zMode == ZAxisMode.LocalZ;
        float target = local && transform.parent ? transform.parent.InverseTransformPoint(desired).z : desired.z;
        float current = local ? transform.localPosition.z : transform.position.z;
        float next = Mathf.SmoothDamp(ClampZ(current, local), ClampZ(target, local), ref _velZ, positionSmoothTimeZ);
        SetZ(ClampZ(next, local), local);
        if (!lockRotation && lookAtBall)
        {
            Vector3 direction = ball.position - transform.position;
            if (direction.sqrMagnitude > .0001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction, Vector3.up), lookAtLerpSpeed * Time.deltaTime);
        }
    }
    void HandleLaunched()
    {
        if (!followOnLaunch) return;
        if (_focusPaused) { _focusLaunchRequested = true; return; }
        if (_monitorPaused || _temporarilyDisabled) return;
        if (!ball) TryFindBall();
        StartFollow();
    }
    public void StartFollow()
    {
        if (_focusPaused) { _focusLaunchRequested = true; return; }
        if (!ball || _monitorPaused || _temporarilyDisabled) return;
        SaveDefaultPose();
        StopAllCoroutines();
        bool local = zMode == ZAxisMode.LocalZ;
        Vector3 desired = ComputeDesiredWorldPosition();
        float target = local && transform.parent ? transform.parent.InverseTransformPoint(desired).z : desired.z;
        float current = local ? transform.localPosition.z : transform.position.z;
        SetZ(ClampZ(Mathf.Lerp(current, ClampZ(target, local), firstSnapFactor), local), local);
        _velZ = 0;
        _following = true;
        _returningToDefault = false;
        StartZoom(zoomFOV, zoomInTime);
    }

    public void BeginMonitor()
    {
        if (_monitorPaused) return;
        _returnAfterMonitor |= _returningToDefault;
        _monitorSavedPos = transform.position;
        _monitorSavedRot = transform.rotation;
        _monitorPoseSaved = true;
        _monitorWasFollowing = _following || _resumeAfterCameraChange;
        _following = false;
        _monitorPaused = true;
        _velZ = 0;
        StopAllCoroutines();
    }
    public bool HasMonitorSavedPose() => _monitorPoseSaved;
    public Vector3 GetMonitorSavedPosition() => _monitorSavedPos;
    public Quaternion GetMonitorSavedRotation() => _monitorSavedRot;
    public void EndMonitor()
    {
        if (_monitorPoseSaved)
        {
            transform.SetPositionAndRotation(_monitorSavedPos, _monitorSavedRot);
            SetZ(ClampZ(zMode == ZAxisMode.LocalZ ? transform.localPosition.z : transform.position.z, zMode == ZAxisMode.LocalZ), zMode == ZAxisMode.LocalZ);
        }
        _monitorPoseSaved = false;
        _monitorPaused = false;
        _following = _monitorWasFollowing;
        _velZ = 0;
        if (_returnAfterMonitor) { _returnAfterMonitor = false; ReturnToDefault(); }
    }

    // Tutorial focus owns the child camera while this rig remains still. Keep
    // launch events subscribed so the first tutorial shot is followed on release.
    public void BeginFocus()
    {
        if (_focusPaused) return;
        _returnAfterFocus |= _returningToDefault;
        _focusPaused = true;
        _focusWasFollowing = _following;
        _focusLaunchRequested = false;
        _following = false;
        _velZ = 0;
        StopAllCoroutines();
    }
    public void EndFocus()
    {
        if (!_focusPaused) return;
        _focusPaused = false;
        _following = _focusWasFollowing || _focusLaunchRequested;
        _focusLaunchRequested = false;
        _velZ = 0;
        if (_returnAfterFocus) { _returnAfterFocus = false; ReturnToDefault(); }
        else if (_following && !_monitorPaused && !_temporarilyDisabled) StartZoom(zoomFOV, zoomInTime);
    }
    public void ResetToDefaultView() => ReturnToDefault();
    void ReturnToDefault()
    {
        if (!_defaultSaved) return;
        _following = _resumeAfterCameraChange = _monitorWasFollowing = _focusWasFollowing = _focusLaunchRequested = false;
        _returningToDefault = true;
        _velZ = 0;
        StopAllCoroutines();
        if (_focusPaused) { _returnAfterFocus = true; return; }
        if (_monitorPaused) { _returnAfterMonitor = true; return; }
        if (!isActiveAndEnabled)
        {
            transform.SetPositionAndRotation(_defaultPos, _defaultRot);
            if (cam) cam.fieldOfView = DefaultFov(cam);
            return;
        }
        StartCoroutine(ReturnPose());
        StartZoom(DefaultFov(cam), zoomOutTime);
    }
    IEnumerator ReturnPose()
    {
        Vector3 start = transform.position;
        Quaternion rotation = transform.rotation;
        float elapsed = 0;
        while (elapsed < zoomOutTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / zoomOutTime));
            transform.SetPositionAndRotation(Vector3.Lerp(start, _defaultPos, t), Quaternion.Slerp(rotation, _defaultRot, t));
            SetZ(ClampZ(zMode == ZAxisMode.LocalZ ? transform.localPosition.z : transform.position.z, zMode == ZAxisMode.LocalZ), zMode == ZAxisMode.LocalZ);
            yield return null;
        }
        transform.SetPositionAndRotation(_defaultPos, _defaultRot);
        SetZ(ClampZ(zMode == ZAxisMode.LocalZ ? _defaultLocalPos.z : _defaultPos.z, zMode == ZAxisMode.LocalZ), zMode == ZAxisMode.LocalZ);
        _returningToDefault = false;
    }
    void ChangeCameraFunc(Camera next)
    {
        if (!next || _monitorPaused) return;
        // Main activation after EndMonitor must not cancel its queued reset.
        if (next == cam && !_temporarilyDisabled) return;
        StopAllCoroutines();
        _velZ = 0;
        if (next.gameObject.name == "RouletteCamera")
        {
            if (!_temporarilyDisabled) _resumeAfterCameraChange = _following;
            _following = false;
            _temporarilyDisabled = true;
            return;
        }
        cam = next;
        DefaultFov(cam);
        if (_temporarilyDisabled)
        {
            _temporarilyDisabled = false;
            _following = _resumeAfterCameraChange;
            _resumeAfterCameraChange = false;
        }
        // A selection event is not a ball launch. Never capture a shifted rig as
        // a new default, and never snap the rig toward a ball on another table.
        if (_returningToDefault) ReturnToDefault();
        else if (_following && !_focusPaused) StartZoom(zoomFOV, zoomInTime);
    }
    public void RefreshDefaultPose()
    {
        if (_following || _monitorPaused || _focusPaused || _temporarilyDisabled) return;
        _defaultSaved = false;
        SaveDefaultPose();
    }
    void SaveDefaultPose()
    {
        if (_defaultSaved) return;
        _defaultPos = transform.position;
        _defaultLocalPos = transform.localPosition;
        _defaultRot = transform.rotation;
        _defaultFOV = cam ? cam.fieldOfView : 60;
        if (cam) _cameraFovs[cam] = _defaultFOV;
        _defaultSaved = true;
    }
    float DefaultFov(Camera camera)
    {
        if (!camera) return _defaultFOV;
        if (!_cameraFovs.TryGetValue(camera, out float value))
        {
            value = camera.fieldOfView;
            _cameraFovs.Add(camera, value);
        }
        return value;
    }
    void StartZoom(float target, float duration)
    {
        if (cam && isActiveAndEnabled) StartCoroutine(Zoom(cam, cam.fieldOfView, Mathf.Clamp(target, 1, 179), duration));
    }
    IEnumerator Zoom(Camera target, float from, float to, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration && target && target == cam && !_monitorPaused && !_focusPaused)
        {
            elapsed += Time.unscaledDeltaTime;
            target.fieldOfView = Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }
        if (target && target == cam && !_monitorPaused && !_focusPaused) target.fieldOfView = to;
    }
    float ClampZ(float value, bool local)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) value = local ? _defaultLocalPos.z : _defaultPos.z;
        float min = local ? minLocalZ : minWorldZ;
        float max = local ? maxLocalZ : maxWorldZ;
        if (useMinZClamp) value = Mathf.Max(value, min);
        if (useMaxZClamp) value = Mathf.Min(value, useMinZClamp ? Mathf.Max(min, max) : max);
        return value;
    }
    void SetZ(float value, bool local)
    {
        Vector3 position = local ? transform.localPosition : transform.position;
        Vector3 baseline = local ? _defaultLocalPos : _defaultPos;
        if (lockXY) { position.x = baseline.x; position.y = baseline.y; }
        position.z = value;
        if (local) transform.localPosition = position;
        else transform.position = position;
    }
    Vector3 ComputeDesiredWorldPosition()
    {
        if (followSpace == FollowSpace.WorldSpaceOffset) return ball.position + worldOffset;
        return ball.position + transform.right * camAxesOffset.x + transform.up * camAxesOffset.y + transform.forward * camAxesOffset.z;
    }
    void TryFindBall()
    {
        if (!string.IsNullOrEmpty(ballTag))
        {
            var found = GameObject.FindWithTag(ballTag);
            if (found) ball = found.transform;
        }
        if (!ball)
        {
            var controller = FindAnyObjectByType<YJ_Script_BallController>();
            if (controller) ball = controller.transform;
        }
    }
}
