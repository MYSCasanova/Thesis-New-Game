
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [Header("Pause UI")]
    public GameObject pausePanel;

    private bool isPaused = false;

    private void Start()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // --- NEW BGM LOGIC ---
            // Play the UI click sound when pressing ESC
            if (S16_AudioManager.instance != null)
            {
                S16_AudioManager.instance.PlaySFX("Click");
            }

            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void ResumeGame()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void RestartGame()
    {
        // Resume time before resetting gameplay
        Time.timeScale = 1f;
        isPaused = false;

        // Get the initial player and camera checkpoints
        if (S6_Checkpoint.Instance == null)
        {
            Debug.LogError("S6_Checkpoint instance not found!");
            return;
        }

        Vector3 startPosition = S6_Checkpoint.Instance.GetInitialPlayerCheckpoint();

        Vector3 cameraCheckpoint = S6_Checkpoint.Instance.GetInitialCameraCheckpoint();

        // Find the player
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("Player not found!");
            return;
        }

        // Reset lives
        S7_HealthSystem healthSystem = player.GetComponent<S7_HealthSystem>();

        if (healthSystem != null)
        {
            healthSystem.ResetLives();
        }

        // Reset score
        S2_ComboSystem comboSystem = FindFirstObjectByType<S2_ComboSystem>();

        if (comboSystem != null)
        {
            comboSystem.ResetScore();
        }

        // Reset player position
        player.transform.position = startPosition;

        // Reset player velocity
        Rigidbody2D playerRigidbody = player.GetComponent<Rigidbody2D>();

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
        }

        // Reset player controller
        S1_PlayerController playerController = player.GetComponent<S1_PlayerController>();

        if (playerController != null)
        {
            playerController.StopRespawning();
            playerController.ResetMultiplier();
            playerController.ResetRun();
        }

        // Reset camera position
        if (Camera.main != null)
        {
            CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();

            if (cameraFollow != null)
            {
                cameraFollow.enabled = true;
                cameraFollow.ResetCamera(cameraCheckpoint);
            }
        }

        // Close pause panel
        pausePanel.SetActive(false);

        Debug.Log("Game restarted from the initial checkpoint.");
    }
}
