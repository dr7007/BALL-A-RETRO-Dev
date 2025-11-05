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

    [Header("Volumes")]
    [Range(0, 1)] public float bgmVolume = 0.6f;
    [Range(0, 1)] public float sfxVolume = 1.0f;
    public bool playBgmOnStart = true;

    private float _hitCool = 0.03f;
    private float _nextHitTime = 0f;

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
    }

    void OnDisable()
    {
        KHS_Script_PlungerController.OnBallLaunched -= HandleLaunch;
        KHS_Script_DumpManager.OnBallCollision -= HandleHitSfx;
        KHS_Script_ScoreManager.OnGameClear -= HandleClear;
        KHS_Script_ScoreManager.OnGameOver -= HandleGameOver;
        KHS_Script_BallOutController.BallOutEvt -= HandleGameOver;
    }

    private void HandleLaunch() => PlayOneShot(sfxBallLaunch);
    private void HandleClear() => PlayOneShot(sfxClear);
    private void HandleGameOver() => PlayOneShot(sfxGameOver);

    private void HandleHitSfx(Collision _)
    {
        if (Time.time < _nextHitTime) return;
        _nextHitTime = Time.time + _hitCool;
        PlayOneShot(sfxObstacleHit);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.volume = sfxVolume;
        sfxSource.PlayOneShot(clip);
    }
}
