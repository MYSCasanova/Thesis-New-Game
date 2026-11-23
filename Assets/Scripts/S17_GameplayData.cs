using UnityEngine;

public class S17_GameplayData : MonoBehaviour
{
    public float completionTime ;
    public int errorCount;

    private float gameStartTime;
    private bool gameTimerStarted = false;
    private bool gameCompleted = false;

    private void Awake()
    {
        ResetGameData();

        Debug.Log("Gameplay Data Manager Initialized: " + gameObject.scene.name);
    }

    public void StartGameTimer()
    {
        if (gameTimerStarted || gameCompleted) return;

        gameStartTime = Time.time;
        gameTimerStarted = true;

        Debug.Log("TIMER STARTED: " + gameObject.scene.name);
    }

    public void CompleteGame()
    {
        if (!gameTimerStarted || gameCompleted) return;

        completionTime = Time.time - gameStartTime;
        gameTimerStarted = false;
        gameCompleted = true;

        Debug.Log("TIMER COMPLETED: " + gameObject.scene.name +
                  " | Completion Time: " + completionTime.ToString("F2") +
                  " | Errors: " + errorCount);
    }

    public void ResetGameData()
    {
        completionTime = 0f;
        errorCount = 0;
        gameStartTime = 0f;

        gameTimerStarted = false;
        gameCompleted = false;
    }
}