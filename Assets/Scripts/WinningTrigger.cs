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

        // Stop the completion timer and save the final gameplay data
        S17_GameplayData gameplayData = FindFirstObjectByType<S17_GameplayData>();

        if (gameplayData != null)
        {
            gameplayData.CompleteGame();
        }

        // Display the winning panel
        winningPanel.SetActive(true);

        // Display the player's final score
        finalScoreText.text = "Final Score: " + comboSystem.totalScore;
    }
}
