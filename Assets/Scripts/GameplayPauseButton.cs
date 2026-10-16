using UnityEngine;

public class GameplayPauseButton : MonoBehaviour
{
    public void Pause()
    {
        PauseManager pauseManager = FindFirstObjectByType<PauseManager>();

        if (pauseManager != null)
        {
            pauseManager.PauseGame();
        }
        else
        {
            Debug.LogError("PauseManager not found!");
        }
    }
}
