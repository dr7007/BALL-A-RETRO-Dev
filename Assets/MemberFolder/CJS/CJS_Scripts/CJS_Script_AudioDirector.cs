using UnityEngine;

[DefaultExecutionOrder(-10)]
public class CJS_Script_AudioDirector : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource bgmSource;          // Loop on
    public AudioSource sfxSource;          // PlayOneShot용

    [Header("Clips")]
    public AudioClip bgmMain;
    public AudioClip sfxBallLaunch;
    public AudioClip sfxObstacleHit;
    public AudioClip sfxClear;
    public AudioClip sfxGameOver;

    [Header("Flipper Clips (Added)")]
    public AudioClip sfxFlipperPress;      // 플리퍼 누를 때
    public AudioClip sfxFlipperRelease;    // 플리퍼 뗄 때

    [Header("Volumes")]
    [Range(0, 1)] public float bgmVolume = 0.6f;
    [Range(0, 1)] public float sfxVolume = 1.0f;
    public bool playBgmOnStart = true;

    [Header("SFX Tweaks")]
    [Tooltip("충돌/플리퍼 효과음 스팸 방지 쿨타임")]
    [SerializeField] private float hitCooldown = 0.03f;
    [Tooltip("플리퍼 음 높낮이 랜덤 범위")]
    [SerializeField] private Vector2 flipperPitchRange = new Vector2(0.97f, 1.03f);

    private float _nextHitTime = 0f;
    private float _nextFlipTime = 0f;

    void Reset()
    {
        if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        sfxSource.playOnAwake = false;
    }

    void Awake()
    {
        if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;
        sfxSource.volume = sfxVolume;
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

        //  플리퍼 이벤트 구독
        KHS_Script_FliperController.OnAnyFlipperPress += HandleFlipperPress;
        KHS_Script_FliperController.OnAnyFlipperRelease += HandleFlipperRelease;
    }

    void OnDisable()
    {
        KHS_Script_PlungerController.OnBallLaunched -= HandleLaunch;
        KHS_Script_DumpManager.OnBallCollision -= HandleHitSfx;
        KHS_Script_ScoreManager.OnGameClear -= HandleClear;
        KHS_Script_ScoreManager.OnGameOver -= HandleGameOver;
        KHS_Script_BallOutController.BallOutEvt -= HandleGameOver;

        // 구독 해제 
        KHS_Script_FliperController.OnAnyFlipperPress -= HandleFlipperPress;
        KHS_Script_FliperController.OnAnyFlipperRelease -= HandleFlipperRelease;
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

    //  플리퍼 핸들러 
    private void HandleFlipperPress()
    {
        if (Time.time < _nextFlipTime) return;
        _nextFlipTime = Time.time + hitCooldown;
        PlayOneShotWithRandomPitch(sfxFlipperPress, flipperPitchRange);
    }
    private void HandleFlipperRelease()
    {
        if (sfxFlipperRelease == null) return; 
        if (Time.time < _nextFlipTime) return;
        _nextFlipTime = Time.time + hitCooldown;
        PlayOneShotWithRandomPitch(sfxFlipperRelease, flipperPitchRange);
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
}
