using UnityEngine;

public class Settings : MonoBehaviour
{
    public static Settings Instance { get; private set; }

    public bool cameraFlipEnabled = true; // default ON

    const string Key = "CameraFlipEnabled";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        cameraFlipEnabled = PlayerPrefs.GetInt(Key, 1) == 1; // default ON
    }

    public void SetCameraFlipEnabled(bool enabled)
    {
        cameraFlipEnabled = enabled;
        PlayerPrefs.SetInt(Key, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }
}
