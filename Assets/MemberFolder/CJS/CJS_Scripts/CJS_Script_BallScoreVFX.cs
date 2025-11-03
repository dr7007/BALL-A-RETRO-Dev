using UnityEngine;

public class CJS_Script_BallScoreVFX : MonoBehaviour
{
    [Header("Refs")]
    public ParticleSystem burst;           // 간단한 입자. 없으면 생략
    public Renderer ballRenderer;          // 머티리얼에 _EmissionColor 있으면 플래시
    public Color flashColor = Color.yellow;

    [Header("Strength Mapping")]
    public float scoreAtMax = 2000f;       // 이 점수 이상이면 강도 1.0
    public AnimationCurve strengthCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public int emitAt1 = 50;               // 강도 1.0일 때 입자 수
    public float flashAt1 = 2.0f;          // 강도 1.0일 때 Emission 배수
    public float flashDuration = 0.15f;

    private Material _mat;
    private Color _baseEmission;
    private bool _hasEmission;

    void Awake()
    {
        if (ballRenderer != null)
        {
            _mat = ballRenderer.material; // 인스턴스화
            if (_mat.HasProperty("_EmissionColor"))
            {
                _baseEmission = _mat.GetColor("_EmissionColor");
                _hasEmission = true;
            }
        }
    }

    void OnEnable()
    {
        KHS_Script_ScoreManager.OnScoreGained += HandleScore;
    }

    void OnDisable()
    {
        KHS_Script_ScoreManager.OnScoreGained -= HandleScore;
        if (_hasEmission) _mat.SetColor("_EmissionColor", _baseEmission);
    }

    private void HandleScore(int delta)
    {
        if (delta <= 0) return;

        float nx = Mathf.Clamp01(delta / Mathf.Max(1f, scoreAtMax));
        float k = Mathf.Clamp01(strengthCurve.Evaluate(nx));

        // 파티클 버스트
        if (burst != null)
        {
            int cnt = Mathf.RoundToInt(emitAt1 * k);
            if (cnt > 0) burst.Emit(cnt);
        }

        // 머티리얼 플래시
        if (_hasEmission)
        {
            StopAllCoroutines();
            StartCoroutine(CoFlash(k));
        }
    }

    private System.Collections.IEnumerator CoFlash(float k)
    {
        float t = 0f;
        float dur = Mathf.Max(0.01f, flashDuration);
        Color target = flashColor * (flashAt1 * k);

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / dur);
            // Ease-in-out
            float e = n < 0.5f ? (n * 2f) : (2f - n * 2f);
            _mat.SetColor("_EmissionColor", Color.Lerp(_baseEmission, target, e));
            yield return null;
        }

        _mat.SetColor("_EmissionColor", _baseEmission);
    }
}
