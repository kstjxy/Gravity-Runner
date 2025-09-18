using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    [Header("UI Refs")]
    public TMP_Text bestText;
    public GameObject settingsPanel;   // panel contains BGMSlider + SFXSlider
    public Toggle cameraFlipToggle;

    [Header("Volume Sliders (inside Settings Panel)")]
    public Slider bgmSlider;
    public Slider sfxSlider;

    private bool listenersHooked = false;

    void Start()
    {
        float best = PlayerPrefs.GetFloat("BestDistance", 0f);
        if (bestText) bestText.text = $"Best: {best:0.0} m";

        if (settingsPanel) settingsPanel.SetActive(false);

        // Ensure AudioManager reflects saved volumes on boot
        if (Settings.Instance && AudioManager.Instance)
        {
            AudioManager.Instance.SetBGMVolume(Settings.Instance.bgmVolume);
            AudioManager.Instance.SetSFXVolume(Settings.Instance.sfxVolume);
        }

        // Hook listeners once (sliders might be inactive initially since panel is closed)
        HookSliderListeners();
    }

    void OnDestroy()
    {
        UnhookSliderListeners();
    }

    public void StartGame() => SceneManager.LoadScene("Game");

    public void OpenSettings()
    {
        if (!settingsPanel) return;

        // Sync current values into UI every time the panel is shown
        if (Settings.Instance)
        {
            if (cameraFlipToggle)
                cameraFlipToggle.isOn = Settings.Instance.cameraFlipEnabled;

            if (bgmSlider)
                bgmSlider.value = Settings.Instance.bgmVolume;

            if (sfxSlider)
                sfxSlider.value = Settings.Instance.sfxVolume;
        }

        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (!settingsPanel) return;
        settingsPanel.SetActive(false);
    }

    public void OnCameraFlipToggleChanged(bool isOn)
    {
        if (Settings.Instance)
            Settings.Instance.SetCameraFlipEnabled(isOn);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // --- internal ---

    void HookSliderListeners()
    {
        if (listenersHooked) return;
        if (!Settings.Instance) return;

        if (bgmSlider)
            bgmSlider.onValueChanged.AddListener(Settings.Instance.SetBGMVolume);

        if (sfxSlider)
            sfxSlider.onValueChanged.AddListener(Settings.Instance.SetSFXVolume);

        listenersHooked = true;
    }

    void UnhookSliderListeners()
    {
        if (!listenersHooked) return;
        if (!Settings.Instance) return;

        if (bgmSlider)
            bgmSlider.onValueChanged.RemoveListener(Settings.Instance.SetBGMVolume);

        if (sfxSlider)
            sfxSlider.onValueChanged.RemoveListener(Settings.Instance.SetSFXVolume);

        listenersHooked = false;
    }
}
