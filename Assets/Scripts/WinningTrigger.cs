using UnityEngine;
using TMPro;

public class WinningTrigger : MonoBehaviour
{
    public GameObject winningPanel;
    public TMP_Text finalScoreText;
    public S2_ComboSystem comboSystem;

    private bool hasWon = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasWon || !collision.CompareTag("Player"))
            return;

        hasWon = true;

        S17_GameplayData gameplayData = FindFirstObjectByType<S17_GameplayData>();

        if (gameplayData != null)
        {
            gameplayData.CompleteGame();
        }

        // Stop the player from moving or processing gameplay input.
        S1_PlayerController playerController = collision.GetComponent<S1_PlayerController>();

        Rigidbody2D playerRb = collision.GetComponent<Rigidbody2D>();

        if (playerController != null)
            playerController.enabled = false;

        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
            playerRb.angularVelocity = 0f;
            playerRb.simulated = false;
        }

        // Stop the camera from following the player
        CameraFollow cameraFollow =
            Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;

        if (cameraFollow != null)
            cameraFollow.enabled = false;

        // Stop further hazard damage
        S7_HealthSystem healthSystem = collision.GetComponent<S7_HealthSystem>();

        if (healthSystem != null)
            healthSystem.enabled = false;

        // Show the winning panel
        if (winningPanel != null) 
            winningPanel.SetActive(true);
        
        // --- NEW BGM LOGIC ---
        // Play Winner BGM as soon as the panel is activated
        if (S16_AudioManager.instance != null)
        {
            S16_AudioManager.instance.PlayBGM("Winner");
        }

        // Display the player's final score
        finalScoreText.text = comboSystem.totalScore.ToString();
        Debug.Log("Final Score: " +comboSystem.totalScore.ToString());
    }
}