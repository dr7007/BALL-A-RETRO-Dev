using UnityEngine;
using System.Collections;

[DefaultExecutionOrder(-10)]
public class CJS_Script_AudioDirector : MonoBehaviour
{
    public static CJS_Script_AudioDirector I { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgmSource;          // BGM (배경음악)
    public AudioSource sfxSource;          // SFX (단발성 효과음)
    public AudioSource sfxLoopSource;      // SFX Loop (공 구르는 소리, 레일 등)

    [Header("Clips")]
    public AudioClip bgmMain;
    public AudioClip sfxBallLaunch;
    public AudioClip sfxObstacleHit;
    public AudioClip sfxClear;
    public AudioClip sfxGameOver;
    public AudioClip sfxFlipperPress;
    public AudioClip sfxFlipperHitBall;
    public AudioClip sfxPortalEnter;
    public AudioClip sfxPortalExit;
    public AudioClip sfxRailRideLoop;

    [Header("Volume Settings")]
    [Range(0, 1)] public float bgmVolume = 0.6f;
    [Range(0, 1)] public float sfxVolume = 1.0f; // 모든 효과음 제어 기준
    public bool playBgmOnStart = true;

    [Header("Loop Fade Options")]
    [SerializeField] private float loopFadeInSeconds = 0.05f;
    [SerializeField] private float loopFadeOutSeconds = 0.12f;

    private float _nextHitTime = 0f;
    private float _nextPortalTime = 0f;
    private float _nextFlipperHitTime = 0f;
    private Coroutine _loopFadeCo;

    // Flipper 관련 변수
    private float flipperPressDecisionWindow = 0.08f;
    private float pressedHitGrace = 0.12f;
    private int _pressedCount = 0;
    private int _pendingPressVersion = 0;
    private Coroutine _pendingPressCo;
    private float _lastPressTime = -999f;
    [SerializeField] private Vector2 flipperHitPitchRange = new Vector2(0.95f, 1.05f);
    [SerializeField] private AnimationCurve flipperHitVolCurve;
    internal float sfxLoopVolume;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        // AudioSource 자동 할당 및 초기화
        if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        if (sfxLoopSource == null) sfxLoopSource = gameObject.AddComponent<AudioSource>();

        bgmSource.loop = true;
        sfxLoopSource.loop = true;
        sfxLoopSource.volume = 0f;
    }

    void Start()
    {
        if (playBgmOnStart && bgmMain != null)
        {
            bgmSource.clip = bgmMain;
            bgmSource.volume = bgmVolume;
            bgmSource.Play();
        }
    }

    // --- UI 브릿지에서 호출할 마스터 동기화 함수 ---
    public void SyncAllVolumes(float newBgmVol, float newSfxVol)
    {
        bgmVolume = newBgmVol;
        sfxVolume = newSfxVol;

        // 1. 배경음 즉시 반영
        if (bgmSource != null) bgmSource.volume = bgmVolume;

        // 2. 단발 효과음 즉시 반영
        if (sfxSource != null) sfxSource.volume = sfxVolume;

        // 3. 루프 효과음 (공 소리) 즉시 반영
        if (sfxLoopSource != null && sfxLoopSource.isPlaying)
        {
            // [중요] 페이드 코루틴이 목표값을 향해 가고 있다면 중단시킴
            if (_loopFadeCo != null) StopCoroutine(_loopFadeCo);
            // 현재 슬라이더 값으로 강제 고정
            sfxLoopSource.volume = sfxVolume;
        }
    }

    #region Loop SFX Control
    public void PlayRailRideLoop(bool restartIfPlaying = false)
    {
        if (sfxRailRideLoop == null || sfxLoopSource == null) return;

        if (sfxLoopSource.isPlaying && sfxLoopSource.clip == sfxRailRideLoop && !restartIfPlaying)
        {
            StartLoopFade(sfxLoopSource, sfxLoopSource.volume, sfxVolume, loopFadeInSeconds);
            return;
        }

        sfxLoopSource.clip = sfxRailRideLoop;
        sfxLoopSource.volume = 0f;
        sfxLoopSource.Play();
        StartLoopFade(sfxLoopSource, 0f, sfxVolume, loopFadeInSeconds);
    }

    public void StopRailRideLoop()
    {
        if (sfxLoopSource == null || !sfxLoopSource.isPlaying) return;
        StartLoopFade(sfxLoopSource, sfxLoopSource.volume, 0f, loopFadeOutSeconds, true);
    }

    private void StartLoopFade(AudioSource src, float from, float to, float seconds, bool stopAfterFade = false)
    {
        if (_loopFadeCo != null) StopCoroutine(_loopFadeCo);
        _loopFadeCo = StartCoroutine(CoFadeVolume(src, from, to, seconds, stopAfterFade));
    }

    private IEnumerator CoFadeVolume(AudioSource src, float from, float to, float seconds, bool stopAfterFade)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime; // 일시정지 중에도 작동
            // 실시간 변수인 sfxVolume을 목표값(to) 대신 사용하여 슬라이더 반응성 확보
            src.volume = Mathf.Lerp(from, sfxVolume, t / seconds);
            yield return null;
        }
        src.volume = sfxVolume;
        if (stopAfterFade) src.Stop();
    }
    #endregion

    #region Event Handlers (기존 로직 유지)
    void OnEnable()
    {
        KHS_Script_PlungerController.OnBallLaunched += HandleLaunch;
        KHS_Script_DumpManager.OnBallCollision += HandleHitSfx;
        KHS_Script_ScoreManager.OnGameClear += HandleClear;
        KHS_Script_ScoreManager.OnGameOver += HandleGameOver;
        KHS_Script_BallOutController.BallOutEvt += HandleGameOver;
        KHS_Script_FliperController.OnAnyFlipperPress += HandleFlipperPress;
        KHS_Script_FliperController.OnAnyFlipperRelease += HandleFlipperRelease;
        KHS_Script_FliperDumpManager.OnFliperCollision += HandleFliperCollision;
        KHS_Script_PortalController.portalEvt += HandlePortalEnter;
        KHS_Script_PortalController.portalEndEvt += HandlePortalExit;
    }

    void OnDisable()
    {
        KHS_Script_PlungerController.OnBallLaunched -= HandleLaunch;
        KHS_Script_DumpManager.OnBallCollision -= HandleHitSfx;
        KHS_Script_ScoreManager.OnGameClear -= HandleClear;
        KHS_Script_ScoreManager.OnGameOver -= HandleGameOver;
        KHS_Script_BallOutController.BallOutEvt -= HandleGameOver;
        KHS_Script_FliperController.OnAnyFlipperPress -= HandleFlipperPress;
        KHS_Script_FliperController.OnAnyFlipperRelease -= HandleFlipperRelease;
        KHS_Script_FliperDumpManager.OnFliperCollision -= HandleFliperCollision;
        KHS_Script_PortalController.portalEvt -= HandlePortalEnter;
        KHS_Script_PortalController.portalEndEvt -= HandlePortalExit;
    }

    private void HandleLaunch() => PlayOneShot(sfxBallLaunch);
    private void HandleClear() => PlayOneShot(sfxClear);
    private void HandleGameOver() => PlayOneShot(sfxGameOver);

    private void HandleHitSfx(Collision _)
    {
        if (Time.time < _nextHitTime) return;
        _nextHitTime = Time.time + 0.03f;
        PlayOneShot(sfxObstacleHit);
    }

    private void HandleFlipperPress()
    {
        _pressedCount++;
        _lastPressTime = Time.time;
        int ver = ++_pendingPressVersion;
        if (_pendingPressCo != null) StopCoroutine(_pendingPressCo);
        _pendingPressCo = StartCoroutine(CoPressDecision(ver));
    }

    private void HandleFlipperRelease() { if (_pressedCount > 0) _pressedCount--; }

    private void HandleFliperCollision(Collision c)
    {
        bool treatAsPressed = (_pressedCount > 0) || (Time.time - _lastPressTime <= pressedHitGrace);
        if (!treatAsPressed) return;
        if (sfxFlipperHitBall == null || sfxSource == null) return;
        if (Time.time < _nextFlipperHitTime) return;
        _nextFlipperHitTime = Time.time + 0.03f;
        _pendingPressVersion++; 

        float mag = c.relativeVelocity.magnitude;
        float strength01 = Mathf.Clamp01(Mathf.InverseLerp(0.5f, 12f, mag));
        float volScale = (flipperHitVolCurve != null) ? flipperHitVolCurve.Evaluate(strength01) : Mathf.Lerp(0.4f, 1f, strength01);
        float pitch = Random.Range(flipperHitPitchRange.x, flipperHitPitchRange.y);
        PlayOneShotWithPitchAndVolume(sfxFlipperHitBall, pitch, sfxVolume * volScale);
    }

    private IEnumerator CoPressDecision(int ver)
    {
        yield return new WaitForSecondsRealtime(flipperPressDecisionWindow);
        if (ver == _pendingPressVersion) PlayOneShotWithRandomPitch(sfxFlipperPress, new Vector2(0.97f, 1.03f));
    }

    private void HandlePortalEnter(int index)
    {
        if (Time.time < _nextPortalTime) return;
        _nextPortalTime = Time.time + 0.03f;
        PlayOneShotWithRandomPitch(sfxPortalEnter, new Vector2(0.98f, 1.02f));
    }

    private void HandlePortalExit() => PlayOneShotWithRandomPitch(sfxPortalExit, new Vector2(0.98f, 1.02f));

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    private void PlayOneShotWithRandomPitch(AudioClip clip, Vector2 pitchRange)
    {
        if (clip == null || sfxSource == null) return;
        float oldPitch = sfxSource.pitch;
        sfxSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        sfxSource.PlayOneShot(clip, sfxVolume);
        sfxSource.pitch = oldPitch;
    }

    private void PlayOneShotWithPitchAndVolume(AudioClip clip, float pitch, float volume)
    {
        if (clip == null || sfxSource == null) return;
        float oldPitch = sfxSource.pitch;
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, volume);
        sfxSource.pitch = oldPitch;
    }
    #endregion
}