using UnityEngine;

[DefaultExecutionOrder(-10)]
public class CJS_Script_AudioDirector : MonoBehaviour
{
    public static CJS_Script_AudioDirector I { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgmSource;          // Loop on
    public AudioSource sfxSource;          // PlayOneShot용

    [Header("Looping SFX Source")]
    public AudioSource sfxLoopSource;      // 루프 전용(예: 레일 주행 소리)

    [Header("Clips")]
    public AudioClip bgmMain;
    public AudioClip sfxBallLaunch;
    public AudioClip sfxObstacleHit;
    public AudioClip sfxClear;
    public AudioClip sfxGameOver;

    [Header("Flipper Clips")]
    public AudioClip sfxFlipperPress;      // 플리퍼 누를 때

    [Header("Flipper Hit Clip")]
    public AudioClip sfxFlipperHitBall;
    [SerializeField] private Vector2 flipperHitPitchRange = new Vector2(0.95f, 1.05f);
    [SerializeField] private float flipperHitCooldown = 0.03f;
    [SerializeField] private AnimationCurve flipperHitVolCurve;

    [Header("Portal Clips")]
    public AudioClip sfxPortalEnter;       // 포탈 '입구'에서 재생
    public AudioClip sfxPortalExit;        // 포탈 '출구'에서 재생(옵션)

    [Header("Rail Ride Loop Clip")]
    [Tooltip("2층으로 올라갈 때 레일을 타는 동안 루프 재생할 짧은 클립(1초 등)")]
    public AudioClip sfxRailRideLoop;

    [Header("Volumes")]
    [Range(0, 1)] public float bgmVolume = 0.6f;
    [Range(0, 1)] public float sfxVolume = 1.0f;
    public bool playBgmOnStart = true;

    [Range(0, 1)] public float sfxLoopVolume = 0.7f;
    [Range(0.5f, 2f)] public float sfxLoopPitch = 1.0f;
    [SerializeField] private float loopFadeInSeconds = 0.05f;
    [SerializeField] private float loopFadeOutSeconds = 0.12f;

    [Header("SFX Tweaks")]
    [SerializeField] private float hitCooldown = 0.03f;
    [SerializeField] private Vector2 flipperPitchRange = new Vector2(0.97f, 1.03f);

    [SerializeField] private float portalCooldown = 0.03f;
    [SerializeField] private Vector2 portalPitchRange = new Vector2(0.98f, 1.02f);

    private float _nextHitTime = 0f;
    private float _nextPortalTime = 0f;
    private float _nextFlipperHitTime = 0f;

    private Coroutine _loopFadeCo;

    [SerializeField, Tooltip("키다운 후 이 시간 안에 공과 충돌하면 키다운 소리를 건너뛰고 히트만 재생")]
    private float flipperPressDecisionWindow = 0.08f;

    [SerializeField, Tooltip("키다운 후 이 시간 내 발생한 충돌은 히트로 인정")]
    private float pressedHitGrace = 0.12f;

    private int _pressedCount = 0;
    private int _pendingPressVersion = 0;
    private Coroutine _pendingPressCo;
    private float _lastPressTime = -999f;

    void Reset()
    {
        if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        if (sfxLoopSource == null) sfxLoopSource = gameObject.AddComponent<AudioSource>();

        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        sfxSource.playOnAwake = false;

        sfxLoopSource.playOnAwake = false;
        sfxLoopSource.loop = true;
        sfxLoopSource.spatialBlend = 0f;
        sfxLoopSource.dopplerLevel = 0f;
    }

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        if (sfxLoopSource == null) sfxLoopSource = gameObject.AddComponent<AudioSource>();

        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;
        sfxSource.volume = sfxVolume;

        sfxLoopSource.loop = true;
        sfxLoopSource.volume = 0f;
        sfxLoopSource.spatialBlend = 0f;
        sfxLoopSource.dopplerLevel = 0f;
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
        KHS_Script_FliperDumpManager.OffFliperCollision += HandleFliperOffCollision;

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
        KHS_Script_FliperDumpManager.OffFliperCollision -= HandleFliperOffCollision;

        KHS_Script_PortalController.portalEvt -= HandlePortalEnter;
        KHS_Script_PortalController.portalEndEvt -= HandlePortalExit;
    }

    private void HandleLaunch() => PlayOneShot(sfxBallLaunch);
    private void HandleClear() => PlayOneShot(sfxClear);
    private void HandleGameOver() => PlayOneShot(sfxGameOver);

    private void HandleHitSfx(Collision _)
    {
        if (Time.time < _nextHitTime) return;
        _nextHitTime = Time.time + hitCooldown;
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

    private void HandleFlipperRelease()
    {
        if (_pressedCount > 0) _pressedCount--;
    }

    private void HandleFliperCollision(Collision c)
    {
        bool treatAsPressed = (_pressedCount > 0) || (Time.time - _lastPressTime <= pressedHitGrace);
        if (!treatAsPressed) return;

        if (sfxFlipperHitBall == null || sfxSource == null) return;
        if (Time.time < _nextFlipperHitTime) return;
        _nextFlipperHitTime = Time.time + flipperHitCooldown;

        _pendingPressVersion++; // 보류된 Press 사운드 무효화

        float mag = c.relativeVelocity.magnitude;
        float strength01 = Mathf.Clamp01(Mathf.InverseLerp(0.5f, 12f, mag));

        float volScale = (flipperHitVolCurve != null)
            ? Mathf.Clamp01(flipperHitVolCurve.Evaluate(strength01))
            : Mathf.Lerp(0.4f, 1f, strength01);

        float pitch = Mathf.Clamp(Random.Range(flipperHitPitchRange.x, flipperHitPitchRange.y), 0.5f, 2f);
        PlayOneShotWithPitchAndVolume(sfxFlipperHitBall, pitch, sfxVolume * volScale);
    }

    private void HandleFliperOffCollision(Collision c)
    {
    }

    private System.Collections.IEnumerator CoPressDecision(int ver)
    {
        float t = 0f;
        while (t < flipperPressDecisionWindow)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (ver == _pendingPressVersion)
        {
            PlayOneShotWithRandomPitch(sfxFlipperPress, flipperPitchRange);
        }
    }

    private void HandlePortalEnter(int index)
    {
        if (Time.time < _nextPortalTime) return;
        _nextPortalTime = Time.time + portalCooldown;
        PlayOneShotWithRandomPitch(sfxPortalEnter, portalPitchRange);
    }

    private void HandlePortalExit()
    {
        if (sfxPortalExit == null) return;
        if (Time.time < _nextPortalTime) return;
        _nextPortalTime = Time.time + portalCooldown;
        PlayOneShotWithRandomPitch(sfxPortalExit, portalPitchRange);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.volume = sfxVolume;
        sfxSource.pitch = 1f;
        sfxSource.PlayOneShot(clip);
    }

    private void PlayOneShotWithRandomPitch(AudioClip clip, Vector2 pitchRange)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.volume = sfxVolume;
        float p = Mathf.Clamp(Random.Range(pitchRange.x, pitchRange.y), 0.5f, 2f);
        float old = sfxSource.pitch;
        sfxSource.pitch = p;
        sfxSource.PlayOneShot(clip);
        sfxSource.pitch = old;
    }

    private void PlayOneShotWithPitchAndVolume(AudioClip clip, float pitch, float volume)
    {
        if (clip == null || sfxSource == null) return;
        float oldPitch = sfxSource.pitch;
        float oldVol = sfxSource.volume;
        sfxSource.pitch = pitch;
        sfxSource.volume = 1f;
        sfxSource.PlayOneShot(clip, volume);
        sfxSource.pitch = oldPitch;
        sfxSource.volume = oldVol;
    }

    public void PlayRailRideLoop(bool restartIfPlaying = false)
    {
        if (sfxRailRideLoop == null || sfxLoopSource == null) return;

        if (sfxLoopSource.isPlaying && sfxLoopSource.clip == sfxRailRideLoop && !restartIfPlaying)
        {
            StartLoopFade(sfxLoopSource, sfxLoopSource.volume, sfxLoopVolume, loopFadeInSeconds);
            return;
        }

        sfxLoopSource.clip = sfxRailRideLoop;
        sfxLoopSource.pitch = sfxLoopPitch;
        sfxLoopSource.volume = 0f;
        sfxLoopSource.loop = true;
        sfxLoopSource.Play();

        StartLoopFade(sfxLoopSource, 0f, sfxLoopVolume, loopFadeInSeconds);
    }

    public void StopRailRideLoop()
    {
        if (sfxLoopSource == null || !sfxLoopSource.isPlaying) return;
        StartLoopFade(sfxLoopSource, sfxLoopSource.volume, 0f, loopFadeOutSeconds, stopAfterFade: true);
    }

    private void StartLoopFade(AudioSource src, float from, float to, float seconds, bool stopAfterFade = false)
    {
        if (_loopFadeCo != null) StopCoroutine(_loopFadeCo);
        _loopFadeCo = StartCoroutine(CoFadeVolume(src, from, to, seconds, stopAfterFade));
    }

    private System.Collections.IEnumerator CoFadeVolume(AudioSource src, float from, float to, float seconds, bool stopAfterFade)
    {
        float t = 0f;
        if (seconds <= 0f)
        {
            src.volume = to;
        }
        else
        {
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(from, to, t / seconds);
                yield return null;
            }
            src.volume = to;
        }

        if (stopAfterFade) src.Stop();
    }
}
