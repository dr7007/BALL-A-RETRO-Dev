using System.Collections;
using UnityEngine;

public class CJS_Script_CameraShaker : MonoBehaviour
{
    public enum AmplitudeMode { Linear, Curve }

    [Header("Target")]
    [Tooltip("흔들 대상. 비워두면 이 스크립트가 붙은 Transform을 사용")]
    public Transform target;

    [Header("Basic")]
    [Tooltip("기본 흔들림 지속 시간(초)")]
    [Min(0f)] public float baseDuration = 0.25f;

    [Tooltip("노이즈 주파수(값이 클수록 빠르게 떨림)")]
    [Min(0f)] public float frequency = 30f;

    [Tooltip("전체 강도 스케일(모든 흔들림에 곱해짐)")]
    [Min(0f)] public float globalIntensity = 1f;

    [Tooltip("타임스케일 0에서도 흔들리려면 체크")]
    public bool useUnscaledTime = false;

    [Header("Amplitude Mapping (Score -> Amplitude)")]
    public AmplitudeMode amplitudeMode = AmplitudeMode.Linear;

    [Tooltip("[Linear] 점수/scoreUnit 당 증가량")]
    [Min(0f)] public float amplitudePerUnit = 0.08f;

    [Tooltip("[Linear] 1 유닛으로 간주할 점수")]
    [Min(1f)] public float scoreUnit = 100f;

    [Tooltip("[Linear] 최소/최대 진폭 클램프")]
    [Min(0f)] public float minAmplitude = 0.02f;
    [Min(0f)] public float maxAmplitude = 1.2f;

    [Tooltip("[Curve] 이 점수 이상이면 1.0으로 정규화 (Curve의 X=1)")]
    [Min(1f)] public float curveMaxScore = 2000f;

    [Tooltip("[Curve] X=정규화된 점수(0~1), Y=진폭(0~1). 최종 진폭 = Y * maxAmplitude")]
    public AnimationCurve amplitudeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Falloff")]
    [Tooltip("시간에 따른 감쇠(0~1 구간). Y가 1에서 0으로 감소하도록 설정 권장")]
    public AnimationCurve falloff = new AnimationCurve(
        new Keyframe(0f, 1f, 0f, -3f),
        new Keyframe(1f, 0f, 0f, 0f)
    );

    [Header("Stacking")]
    [Tooltip("쉐이크가 진행 중일 때 새로 호출되면 강도를 누적할지 여부")]
    public bool accumulate = true;

    private Vector3 _baseLocalPos;
    private Coroutine _routine;
    private float _timeSeedX, _timeSeedY;

    // 누적값 (accumulate=true일 때 사용)
    private float _accumulatedAmplitude = 0f;
    private float _accumulatedDuration = 0f;

    void Awake()
    {
        if (target == null) target = transform;
        _baseLocalPos = target.localPosition;

        // Perlin 노이즈 시작 시드
        _timeSeedX = Random.value * 1000f;
        _timeSeedY = Random.value * 1000f;
    }

    void OnDisable()
    {
        // 비활성 시 원위치(플레이 종료 시 흔들림만 정리)
        if (target != null) target.localPosition = _baseLocalPos;
        _routine = null;
        _accumulatedAmplitude = 0f;
        _accumulatedDuration = 0f;
    }

    /// <summary>
    /// 점수에 따라 자동으로 진폭 계산 후 쉐이크
    /// </summary>
    public void OnScored(int points)
    {
        float amp = EvaluateAmplitude(points);
        Shake(amp, baseDuration);
    }

    /// <summary>
    /// 원하는 진폭/지속으로 직접 쉐이크
    /// </summary>
    public void Shake(float amplitude, float duration)
    {
        if (target == null) return;

        amplitude = Mathf.Max(0f, amplitude) * Mathf.Max(0f, globalIntensity);
        duration = Mathf.Max(0f, duration);

        // 매번 현재 위치를 기준점으로 캡처 → 실행 중 카메라가 바뀌어도 정상 복원
        _baseLocalPos = target.localPosition;

        if (_routine == null)
        {
            _routine = StartCoroutine(ShakeRoutine(amplitude, duration));
        }
        else if (accumulate)
        {
            // 진행 중이면 강도/시간을 보강
            _accumulatedAmplitude += amplitude;
            _accumulatedDuration = Mathf.Max(_accumulatedDuration, duration);
        }
        else
        {
            // 새로 시작
            StopCoroutine(_routine);
            target.localPosition = _baseLocalPos;
            _routine = StartCoroutine(ShakeRoutine(amplitude, duration));
        }
    }

    private IEnumerator ShakeRoutine(float amplitude, float duration)
    {
        float t = 0f;
        float useDuration = duration;
        float currentAmp = amplitude;

        while (true)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;

            // 누적 반영(있다면)
            if (_accumulatedAmplitude > 0f)
            {
                currentAmp += _accumulatedAmplitude;
                useDuration = Mathf.Max(useDuration, _accumulatedDuration);
                _accumulatedAmplitude = 0f;
                _accumulatedDuration = 0f;
            }

            float normalized = useDuration > 0f ? Mathf.Clamp01(t / useDuration) : 1f;
            float mul = falloff.Evaluate(normalized);

            // Perlin 노이즈 기반 오프셋
            float timeFactor = (useUnscaledTime ? Time.unscaledTime : Time.time) * frequency;
            float nx = (Mathf.PerlinNoise(_timeSeedX, timeFactor) * 2f - 1f);
            float ny = (Mathf.PerlinNoise(_timeSeedY, timeFactor) * 2f - 1f);

            Vector3 offset = new Vector3(nx, ny, 0f) * (currentAmp * mul);
            target.localPosition = _baseLocalPos + offset;

            if (normalized >= 1f) break;
            yield return null;
        }

        // 끝나면 기준 위치로 복원
        target.localPosition = _baseLocalPos;
        _routine = null;
    }

    private float EvaluateAmplitude(int points)
    {
        points = Mathf.Max(0, points);

        if (amplitudeMode == AmplitudeMode.Linear)
        {
            float units = points / Mathf.Max(1f, scoreUnit);
            float amp = units * Mathf.Max(0f, amplitudePerUnit);
            amp = Mathf.Clamp(amp, minAmplitude, maxAmplitude);
            return amp;
        }
        else // Curve
        {
            float nx = Mathf.Clamp01(points / Mathf.Max(1f, curveMaxScore));
            float amp01 = Mathf.Clamp01(amplitudeCurve.Evaluate(nx));
            float amp = amp01 * maxAmplitude;
            amp = Mathf.Max(minAmplitude, amp);
            return amp;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (target == null) target = transform;

        if (minAmplitude > maxAmplitude) maxAmplitude = minAmplitude;
    }
#endif
}
