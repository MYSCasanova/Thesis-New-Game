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

    // Beta Button
    public void PlayBeta()
    {
        // Load the PersistentGameplay scene first to ensure the player and camera are available
        SceneManager.LoadScene("PersistentGameplay",LoadSceneMode.Single);
    }

    // Tutorial Button
    public void OpenTutorialGallery()
    {
        
    }

    // Credits Button
    public void OpenCredits()
    {
        
    }

    // Quit Button
    public void QuitGame()
    {
        Application.Quit();
    }
}
