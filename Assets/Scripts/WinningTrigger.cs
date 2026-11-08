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

        // Save leaderboard result
        Debug.Log("WINNING TRIGGER REACHED");

        LeaderboardTimer timer = FindFirstObjectByType<LeaderboardTimer>();
        LeaderboardManager leaderboard = FindFirstObjectByType<LeaderboardManager>();

        Debug.Log("Timer found: " + (timer != null));
        Debug.Log("Leaderboard found: " + (leaderboard != null));

        if (timer != null && leaderboard != null)
        {
            string playerName = PlayerPrefs.GetString("PlayerName", "Unknown Player");

            string version = timer.GetCurrentVersion();
            float completionTime = timer.GetCompletionTime();
            int finalScore = comboSystem.totalScore;

            Debug.Log("Player: " + playerName);
            Debug.Log("Version: " + version);
            Debug.Log("Score: " + finalScore);
            Debug.Log("Time: " + completionTime);

            leaderboard.SaveScore(version, playerName, finalScore, completionTime);

            Debug.Log("Leaderboard result saved: " + version);
        }
        else
        {
            Debug.LogError("LeaderboardTimer or LeaderboardManager not found!");
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