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

    void Start()
    {
        if (gameOverPanel) gameOverPanel.SetActive(false);
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

    void Update()
    {
        // Allow replay via R while the panel is visible
        if (gameOverPanel && gameOverPanel.activeSelf && Input.GetKeyDown(KeyCode.R))
        {
            GameManager.Instance.Replay();
        }
    }
}
