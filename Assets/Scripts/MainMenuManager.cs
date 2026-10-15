using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
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
        // Save the current scene's name before leaving
        previousSceneName = SceneManager.GetActiveScene().name;
        
        // Load the Settings scene in Single mode (automatically unloads the current scene)
        SceneManager.LoadScene("GYM_ManualSettings", LoadSceneMode.Single);
    }

    public void CloseSettings()
    {
        // Check if we have a saved previous scene
        if (!string.IsNullOrEmpty(previousSceneName))
        {
            // Load the previous scene back
            SceneManager.LoadScene(previousSceneName, LoadSceneMode.Single);
        }
        else
        {
            // Fallback in case the game started directly in the settings scene for testing
            SceneManager.LoadScene("GYM_MainMenu", LoadSceneMode.Single);
        }
    }
}