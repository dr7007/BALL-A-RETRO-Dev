using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CJS_TutorialCameraFocus : MonoBehaviour
{
    public Camera cam;
    public MonoBehaviour[] disableWhileFocusing;
    [Header("Focus Tweaks")]
    public float focusFov = 35f;
    public float moveDuration = .35f;

    Vector3 _originalLocalPosition, _originalWorldPosition;
    Quaternion _originalLocalRotation, _originalWorldRotation;
    Transform _originalParent;
    float _originalFov;
    bool _captured, _holding, _restoring, _releaseRequested;
    KHS_Script_CameraController _follow;
    readonly Dictionary<MonoBehaviour, bool> _suspended = new Dictionary<MonoBehaviour, bool>();

    public void CaptureOriginal()
    {
        if (_captured) return;
        if (!cam) cam = Camera.main;
        if (!cam) return;
        HoldCamera();
        _originalParent = cam.transform.parent;
        _originalLocalPosition = cam.transform.localPosition;
        _originalLocalRotation = cam.transform.localRotation;
        _originalWorldPosition = cam.transform.position;
        _originalWorldRotation = cam.transform.rotation;
        _originalFov = cam.fieldOfView;
        _captured = true;
    }
    void HoldCamera()
    {
        if (_holding || !cam) return;
        _holding = true;
        _follow = cam.GetComponentInParent<KHS_Script_CameraController>(true);
        if (_follow) _follow.BeginFocus();
        if (disableWhileFocusing != null)
            foreach (var behaviour in disableWhileFocusing) Suspend(behaviour);
        foreach (var shaker in cam.GetComponents<CJS_Script_CameraShaker>()) Suspend(shaker);
    }
    void Suspend(MonoBehaviour behaviour)
    {
        if (!behaviour || behaviour == this || behaviour == _follow || _suspended.ContainsKey(behaviour)) return;
        _suspended.Add(behaviour, behaviour.enabled);
        behaviour.StopAllCoroutines();
        behaviour.enabled = false;
    }
    public void FocusTo(Transform target)
    {
        if (!target) return;
        CaptureOriginal();
        if (!cam || !_captured) return;
        HoldCamera();
        StopAllCoroutines();
        _releaseRequested = false;
        _restoring = false;
        StartCoroutine(Move(target.position, target.rotation, Mathf.Clamp(focusFov, 1, 179), false));
    }
    public void Restore()
    {
        if (!cam || !_captured) return;
        HoldCamera();
        StopAllCoroutines();
        _restoring = true;
        StartCoroutine(Move(_originalLocalPosition, _originalLocalRotation, _originalFov, true));
    }
    public void ReEnableDisabled()
    {
        _releaseRequested = true;
        if (!_holding) return;
        if (!_restoring) Restore();
    }
    public void RestoreGameplayView()
    {
        CaptureOriginal();
        HoldCamera();
        if (_follow) _follow.ResetToDefaultView();
        Restore();
        ReEnableDisabled();
    }
    IEnumerator Move(Vector3 position, Quaternion rotation, float fov, bool restore)
    {
        bool local = restore && cam.transform.parent == _originalParent;
        if (restore && !local) { position = _originalWorldPosition; rotation = _originalWorldRotation; }
        Vector3 startPosition = local ? cam.transform.localPosition : cam.transform.position;
        Quaternion startRotation = local ? cam.transform.localRotation : cam.transform.rotation;
        float startFov = cam.fieldOfView;
        float elapsed = 0;
        while (elapsed < moveDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / moveDuration));
            ApplyPose(Vector3.Lerp(startPosition, position, t), Quaternion.Slerp(startRotation, rotation, t), Mathf.Lerp(startFov, fov, t), local);
            yield return null;
        }
        ApplyPose(position, rotation, fov, local);
        if (restore)
        {
            _restoring = false;
            if (_releaseRequested) ReleaseCamera();
        }
    }
    void ApplyPose(Vector3 position, Quaternion rotation, float fov, bool local)
    {
        if (!cam) return;
        if (local) { cam.transform.localPosition = position; cam.transform.localRotation = rotation; }
        else cam.transform.SetPositionAndRotation(position, rotation);
        cam.fieldOfView = fov;
    }
    void ReleaseCamera()
    {
        _holding = _restoring = _releaseRequested = false;
        foreach (var entry in _suspended) if (entry.Key) entry.Key.enabled = entry.Value;
        _suspended.Clear();
        if (_follow) _follow.EndFocus();
    }
    void OnDisable()
    {
        StopAllCoroutines();
        if (!_holding) return;
        if (cam && _captured)
        {
            bool local = cam.transform.parent == _originalParent;
            ApplyPose(local ? _originalLocalPosition : _originalWorldPosition,
                local ? _originalLocalRotation : _originalWorldRotation, _originalFov, local);
        }
        ReleaseCamera();
    }
}
