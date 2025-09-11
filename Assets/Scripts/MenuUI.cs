using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuUI : MonoBehaviour
{
    [Header("UI Refs")]
    public TMP_Text bestText;

    void Start()
    {
        float best = PlayerPrefs.GetFloat("BestDistance", 0f);
        if (bestText) bestText.text = $"Best: {best:0.0} m";
    }

    public void StartGame()
    {
        // Assumes "Game" is added to Build Settings
        SceneManager.LoadScene("Game");
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
