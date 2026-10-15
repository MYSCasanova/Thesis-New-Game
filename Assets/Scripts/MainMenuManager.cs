using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    // Static variable to remember where we came from across different scenes
    public static string previousSceneName;
    // Reset key for testing purposes
    // Uncomment when resetting
    
    // private void Start()
    // {
    //     PlayerPrefs.DeleteKey("tutorialCompleted");
    //     PlayerPrefs.Save();

    //     Debug.Log("Tutorial completion reset for testing.");
    // }

    public void PlayBeta()
    {
        // Load the PersistentGameplay scene first to ensure the player and camera are available
        SceneManager.LoadScene("PersistentGameplay", LoadSceneMode.Single);
    }

    public void OpenTutorialGallery()
    {
        SceneManager.LoadScene("GYM_TutorialGallery", LoadSceneMode.Single);
    }

    public void OpenCredits()
    {
        
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void OpenSettings()
    {
        // Unpause the game
        Time.timeScale = 0f;
        SceneManager.LoadScene("GYM_ManualSettings", LoadSceneMode.Additive);
    }

    public void CloseSettings()
    {
        // Unpause the game
        Time.timeScale = 1f;
        SceneManager.UnloadSceneAsync("GYM_ManualSettings");
    }
}