using UnityEngine;
using UnityEngine.SceneManagement; // <-- ADD THIS LINE

public class S17_GameplayData : MonoBehaviour
{
    public static S17_GameplayData Instance; // <-- ADD THIS
    public float completionTime = 0f;
    public int errorCount = 0;

    private float gameStartTime;
    private bool gameTimerStarted = false;
    private bool gameCompleted = false;

    private void Awake()
    {
        ResetGameData();
        Debug.Log("Gameplay Data Manager Initialized: " + gameObject.scene.name);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GYM_BetaGame" || scene.name == "GYM_V2Game" || scene.name == "GYM_V3Game")
        {
            StartGameTimer();
        }
    }

    private void Update()
    {
        if (gameTimerStarted && !gameCompleted)
        {
            completionTime += Time.deltaTime;
        }
    }

    public void StartGameTimer()
    {
        if (gameTimerStarted || gameCompleted) return;
        //gameStartTime = Time.time;
        gameTimerStarted = true;

        Debug.Log("TIMER STARTED: " + gameObject.scene.name);
    }

    public void CompleteGame()
    {
        if (gameCompleted) return;

        gameTimerStarted = false;
        gameCompleted = true;

        // Save V1 metrics so they survive closing the game scene
        PlayerPrefs.SetFloat("V1_CompletionTime", completionTime);
        PlayerPrefs.SetInt("V1_ErrorCount", errorCount);
        PlayerPrefs.Save();

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