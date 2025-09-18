using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Defaults")]
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 1.0f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (bgmSource) bgmSource.volume = bgmVolume;
        if (sfxSource) sfxSource.volume = sfxVolume;
    }

    // ---- BGM ----
    public void PlayBGM(AudioClip clip)
    {
        if (!clip || !bgmSource) return;

        if (!bgmSource.gameObject.activeInHierarchy)
            bgmSource.gameObject.SetActive(true);
        if (!bgmSource.enabled)
            bgmSource.enabled = true;

        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (!bgmSource) return;
        bgmSource.Stop();
        bgmSource.clip = null;
    }

    // ---- SFX ----
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (!clip || !sfxSource) return;

        if (!sfxSource.gameObject.activeInHierarchy)
            sfxSource.gameObject.SetActive(true);
        if (!sfxSource.enabled)
            sfxSource.enabled = true;

        sfxSource.PlayOneShot(clip, sfxVolume * volume);
    }

    public void SetBGMVolume(float v)
    {
        bgmVolume = Mathf.Clamp01(v);
        if (bgmSource) bgmSource.volume = bgmVolume;
    }

    public void SetSFXVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
        if (sfxSource) sfxSource.volume = sfxVolume;
    }
}
