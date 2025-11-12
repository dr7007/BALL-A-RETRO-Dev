using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CJS_Script_LobbyBGM : MonoBehaviour
{
    public static CJS_Script_LobbyBGM I;   // (옵션) 전역 접근용

    [Header("Clip & Options")]
    public AudioClip bgmClip;
    [Range(0f, 1f)] public float volume = 0.6f;
    public bool loop = true;
    public bool playOnAwake = true;

    [Tooltip("체크하면 씬 전환에도 파괴되지 않고 계속 재생됩니다.")]
    public bool persistBetweenScenes = false;

    [Header("Fade (옵션)")]
    public float fadeInSeconds = 0.75f;
    public float fadeOutSeconds = 0.5f;

    private AudioSource src;
    private Coroutine fadeCo;

    void Awake()
    {
        if (persistBetweenScenes)
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        src = GetComponent<AudioSource>();
        src.playOnAwake = false;    // 수동으로 제어 (페이드 인 가능)
        src.loop = loop;
        src.clip = bgmClip;
        src.volume = 0f;
    }

    void Start()
    {
        if (playOnAwake && bgmClip != null)
            Play(withFade: true);
    }

    public void Play(bool withFade = true)
    {
        if (bgmClip == null) return;

        src.loop = loop;
        if (!src.isPlaying) src.Play();

        if (withFade) StartFade(target: volume, duration: fadeInSeconds, stopAfter: false);
        else src.volume = volume;
    }

    public void Stop(bool withFade = true)
    {
        if (!src.isPlaying) return;

        if (withFade) StartFade(target: 0f, duration: fadeOutSeconds, stopAfter: true);
        else src.Stop();
    }

    public void SetVolume(float v, bool withFade = false, float fadeSeconds = 0.3f)
    {
        volume = Mathf.Clamp01(v);
        if (withFade) StartFade(volume, fadeSeconds, stopAfter: false);
        else src.volume = volume;
    }

    private void StartFade(float target, float duration, bool stopAfter)
    {
        if (fadeCo != null) StopCoroutine(fadeCo);
        fadeCo = StartCoroutine(CoFade(target, duration, stopAfter));
    }

    private System.Collections.IEnumerator CoFade(float target, float duration, bool stopAfter)
    {
        float start = src.volume;
        float t = 0f;

        if (!src.isPlaying) src.Play();

        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // Time.timeScale과 무관
            src.volume = Mathf.Lerp(start, target, duration <= 0f ? 1f : t / duration);
            yield return null;
        }

        src.volume = target;
        if (stopAfter) src.Stop();
    }
}
