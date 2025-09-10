using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Refs")]
    public Transform player;      // assign your Player (capsule) in inspector
    public CameraFollow camFollow; // assign Main Camera's CameraFollow
    public GameUI gameUI;          // assign the UI script on your Canvas

    public float distance { get; private set; }
    public float bestDistance { get; private set; }
    public bool isRunning { get; private set; } = true;

    private float startZ;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;

        bestDistance = PlayerPrefs.GetFloat("BestDistance", 0f);
        startZ = player ? player.position.z : 0f;
        isRunning = true;

        if (gameUI) gameUI.SetBest(bestDistance);
    }

    void Update()
    {
        if (!isRunning || !player) return;

        // Distance is how far along +Z we've moved since start
        float dz = player.position.z - startZ;
        if (dz > distance) distance = dz;

        if (gameUI) gameUI.SetDistance(distance);

        // Replay on R when game is over (handled there), but also allow here as a convenience
        if (!isRunning && Input.GetKeyDown(KeyCode.R))
        {
            Replay();
        }
    }

    public void GameOver()
    {
        if (!isRunning) return;
        isRunning = false;

        // Freeze camera follow immediately
        if (camFollow) camFollow.enabled = false;

        // Save best distance
        if (distance > bestDistance)
        {
            bestDistance = distance;
            PlayerPrefs.SetFloat("BestDistance", bestDistance);
            PlayerPrefs.Save();
        }

        // Show UI popup
        if (gameUI) gameUI.ShowGameOver(distance, bestDistance);

        var rb = player ? player.GetComponent<Rigidbody>() : null;
        if (rb) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
    }

    public void Replay()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
