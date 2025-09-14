using UnityEngine;
using TMPro;

public class GameUI : MonoBehaviour
{
    [Header("HUD")]
    public TMP_Text distanceText;
    public TMP_Text bestText;

    [Header("Popup")]
    public GameObject gameOverPanel;
    public TMP_Text finalDistanceText;
    public TMP_Text finalBestText;

    [Header("Idle Countdown")]
    public TMP_Text idleCountdownText;

    void Start()
    {
        if (gameOverPanel) gameOverPanel.SetActive(false);

        if (idleCountdownText) idleCountdownText.gameObject.SetActive(false);
    }

    public void SetDistance(float d)
    {
        if (distanceText) distanceText.text = $"Distance: {d:0.0} m";
    }

    public void SetBest(float b)
    {
        if (bestText) bestText.text = $"Best before: {b:0.0} m";
    }

    public void ShowGameOver(float final, float best)
    {
        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (finalDistanceText) finalDistanceText.text = $"Distance: {final:0.0} m";
        if (finalBestText) finalBestText.text = $"Best: {best:0.0} m";
    }


    public void SetIdleCountdown(int seconds)
    {
        if (!idleCountdownText) return;
        idleCountdownText.text = seconds.ToString();
        if (!idleCountdownText.gameObject.activeSelf)
            idleCountdownText.gameObject.SetActive(true);
    }

    public void HideIdleCountdown()
    {
        if (idleCountdownText) idleCountdownText.gameObject.SetActive(false);
    }

    public void OnClickReplay()
    {
        GameManager.Instance?.Replay();
    }

    public void OnClickMainMenu()
    {
        GameManager.Instance?.GoToMenu();
    }

    public void OnClickExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }

    void Update()
    {
        if (!gameOverPanel || !gameOverPanel.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            GameManager.Instance.Replay();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
