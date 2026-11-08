using UnityEngine;

public class LeaderboardTimer : MonoBehaviour
{
    private float elapsedTime;
    private bool isRunning;
    private string currentVersion;

    public void StartTimer(string version)
    {
        currentVersion = version;
        elapsedTime = 0f;
        isRunning = true;
    }

    void Update()
    {
        if (isRunning)
            elapsedTime += Time.deltaTime;
    }

    public float GetCompletionTime()
    {
        isRunning = false;
        return elapsedTime;
    }

    public string GetCurrentVersion()
    {
        return currentVersion;
    }
}