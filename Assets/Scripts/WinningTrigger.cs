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

        winningPanel.SetActive(true);
        
        // --- NEW BGM LOGIC ---
        // Play Winner BGM as soon as the panel is activated
        if (S16_AudioManager.instance != null)
        {
            S16_AudioManager.instance.PlayBGM("Winner");
        }

        finalScoreText.text = "Final Score: " + comboSystem.totalScore;
    }
}