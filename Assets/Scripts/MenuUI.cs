using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    [Header("UI Refs")]
    public TMP_Text bestText;
    public GameObject settingsPanel;  // assign Panel
    public Toggle cameraFlipToggle;   // assign Toggle

    void Start()
    {
        float best = PlayerPrefs.GetFloat("BestDistance", 0f);
        if (bestText) bestText.text = $"Best: {best:0.0} m";

        if (settingsPanel) settingsPanel.SetActive(false);

        // Initialize toggle from saved Settings
        if (Settings.Instance && cameraFlipToggle)
        {
            cameraFlipToggle.isOn = Settings.Instance.cameraFlipEnabled;
        }
    }

    public void StartGame()
    {
        // Assumes "Game" is added to Build Settings
        SceneManager.LoadScene("Game");
    }

    // Hook to Settings button
    public void OpenSettings()
    {
        if (!settingsPanel) return;
        if (Settings.Instance && cameraFlipToggle)
            cameraFlipToggle.isOn = Settings.Instance.cameraFlipEnabled;
        settingsPanel.SetActive(true);
    }

    // Hook to Back button on panel
    public void CloseSettings()
    {
        if (!settingsPanel) return;
        settingsPanel.SetActive(false);
    }

    // Hook to Toggle.onValueChanged
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
}
