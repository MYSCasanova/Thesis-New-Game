using UnityEngine;

public class S17_GameplayData : MonoBehaviour
{
    public float completionTime = 0f;
    public int errorCount = 0;

    private float gameStartTime;
    private bool gameTimerStarted = false;
    private bool gameCompleted = false;

    public void StartGameTimer()
    {
        if (gameTimerStarted || gameCompleted) return;

        gameStartTime = Time.time;
        gameTimerStarted = true;

        Debug.Log("Game timer started.");
    }

    public void CompleteGame()
    {
        if (!gameTimerStarted || gameCompleted) return;

        completionTime = Time.time - gameStartTime;
        gameCompleted = true;

        Debug.Log($"Game completed in {completionTime} seconds with {errorCount} errors.");
    }
}
