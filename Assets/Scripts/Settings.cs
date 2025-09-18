using UnityEngine;

public class Settings : MonoBehaviour
{
    [Header("SFX")]
    public AudioClip sfxClick;
    public static Settings Instance { get; private set; }

    public bool cameraFlipEnabled = true; // default ON
    public float bgmVolume = 0.5f;        // default
    public float sfxVolume = 1.0f;        // default

    const string KeyFlip = "CameraFlipEnabled";
    const string KeyBGM = "BGMVolume";
    const string KeySFX = "SFXVolume";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        cameraFlipEnabled = PlayerPrefs.GetInt(KeyFlip, 1) == 1;
        bgmVolume = PlayerPrefs.GetFloat(KeyBGM, 0.5f);
        sfxVolume = PlayerPrefs.GetFloat(KeySFX, 1.0f);
    }

    public void SetCameraFlipEnabled(bool enabled)
    {
        cameraFlipEnabled = enabled;
        PlayerPrefs.SetInt(KeyFlip, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetBGMVolume(float v)
    {
        bgmVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(KeyBGM, bgmVolume);
        PlayerPrefs.Save();

        if (AudioManager.Instance) AudioManager.Instance.SetBGMVolume(bgmVolume);
    }

    // Called continuously while dragging slider
    public void SetSFXVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(KeySFX, sfxVolume);
        PlayerPrefs.Save();

        if (AudioManager.Instance) AudioManager.Instance.SetSFXVolume(sfxVolume);
    }

    // Called once on slider release (OnPointerUp)
    public void PlaySFXClickOnce()
    {
        if (AudioManager.Instance && sfxClick)
            AudioManager.Instance.PlaySFX(sfxClick);
    }
}
