using UnityEngine;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] GameObject gameOverCanvas;

    public void TryAgain()
    {
        Vector3 startPosition = S6_Checkpoint.Instance.GetInitialPlayerCheckpoint();
        Vector3 cameraCheckpoint = S6_Checkpoint.Instance.GetInitialCameraCheckpoint();

        // Find Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            // Reset Lives
            S7_HealthSystem healthSystem = player.GetComponent<S7_HealthSystem>();

            if (healthSystem != null)
            {
                healthSystem.ResetLives();
            }

            // Reset Score
            S2_ComboSystem comboSystem = FindFirstObjectByType<S2_ComboSystem>();

            if (comboSystem != null)
            {
                comboSystem.ResetScore();
            }

            // Reset Player position to the checkpoint
            player.transform.position = startPosition;

            // Reset Player's Rigidbody2D velocity to zero
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

            // Reset camera position to the checkpoint
            if (Camera.main != null && S6_Checkpoint.Instance != null)
            {
                CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
                if (cameraFollow != null)
                {
                    cameraFollow.ResetCamera(cameraCheckpoint);
                }
            }

            // Hide Game Over UI
            gameOverCanvas.SetActive(false);
        }
    }
}