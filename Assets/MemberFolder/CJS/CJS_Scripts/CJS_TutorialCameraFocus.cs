using System.Collections;
using UnityEngine;

public class CJS_TutorialCameraFocus : MonoBehaviour
{
    public Camera cam;
    public MonoBehaviour[] disableWhileFocusing; // 팔로우/스위처 등 방해되는 스크립트 넣기

    [Header("Focus Tweaks")]
    public float focusFov = 35f;
    public float moveDuration = 0.35f;

    Vector3 _origPos;
    Quaternion _origRot;
    float _origFov;
    bool _captured;

    public void CaptureOriginal()
    {
        if (!cam) cam = Camera.main;
        if (!cam) return;

        _origPos = cam.transform.position;
        _origRot = cam.transform.rotation;
        _origFov = cam.fieldOfView;
        _captured = true;
    }

    public void FocusTo(Transform target)
    {
        if (!cam) cam = Camera.main;
        if (!cam || !target) return;

        if (!_captured) CaptureOriginal();

        StopAllCoroutines();
        StartCoroutine(CoFocus(target.position, target.rotation, focusFov));
    }

    public void Restore()
    {
        if (!cam) cam = Camera.main;
        if (!cam || !_captured) return;

        StopAllCoroutines();
        StartCoroutine(CoFocus(_origPos, _origRot, _origFov));
    }

    IEnumerator CoFocus(Vector3 pos, Quaternion rot, float fov)
    {
        // 방해 스크립트 잠깐 끄기
        if (disableWhileFocusing != null)
            foreach (var b in disableWhileFocusing) if (b) b.enabled = false;

        float t = 0f;
        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;
        float startFov = cam.fieldOfView;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, moveDuration);
            cam.transform.position = Vector3.Lerp(startPos, pos, t);
            cam.transform.rotation = Quaternion.Slerp(startRot, rot, t);
            cam.fieldOfView = Mathf.Lerp(startFov, fov, t);
            yield return null;
        }
    }

    public void ReEnableDisabled()
    {
        if (disableWhileFocusing != null)
            foreach (var b in disableWhileFocusing) if (b) b.enabled = true;
    }
}
