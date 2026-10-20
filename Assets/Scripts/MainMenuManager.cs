using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Audio; // Required for AudioMixer

public class MainMenuManager : MonoBehaviour
{
    public static string previousSceneName;

    [Header("Settings Data Initialization")]
    public VisualSettingsSO settingsData; 
    public AudioMixer audioMixer;

    private void Start()
    {
        ApplySavedSettingsOnLoad();
    }

    private void ApplySavedSettingsOnLoad()
    {
        // 1. Initialize Audio Volumes
        if (audioMixer != null)
        {
            float savedMusic = PlayerPrefs.GetFloat("SavedMusicVol", 1f);
            float savedSFX = PlayerPrefs.GetFloat("SavedSFXVol", 1f);
            
            audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Clamp(savedMusic, 0.0001f, 1f)) * 20f);
            audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Clamp(savedSFX, 0.0001f, 1f)) * 20f);
        }

        // 2. Initialize Video Settings
        int savedResIndex = PlayerPrefs.GetInt("SavedResolution", 0);
        bool isFullscreen = PlayerPrefs.GetInt("SavedFullscreen", 1) == 1;
        
        Resolution[] allResolutions = Screen.resolutions;
        System.Array.Reverse(allResolutions);
        
        System.Collections.Generic.List<Resolution> ratioResolutions = new System.Collections.Generic.List<Resolution>();
        float targetAspectRatio = 16f / 9f;
        
        foreach (var res in allResolutions)
        {
            float ratio = (float)res.width / (float)res.height;
            if (Mathf.Abs(ratio - targetAspectRatio) < 0.05f)
            {
                if (!ratioResolutions.Exists(r => r.width == res.width && r.height == res.height))
                {
                    ratioResolutions.Add(res);
                }
            }
        }

        if (ratioResolutions.Count > 0 && savedResIndex < ratioResolutions.Count)
        {
            Resolution res = ratioResolutions[savedResIndex];
            Screen.SetResolution(res.width, res.height, isFullscreen);
        }

        // 3. Initialize Visual Settings
        if (settingsData != null)
        {
            LoadVisualCategory(settingsData.backgroundSettings, "Background");
            LoadVisualCategory(settingsData.obstacleSettings, "Obstacle");
            LoadVisualCategory(settingsData.platformSettings, "Platform");
            LoadVisualCategory(settingsData.playerSettings, "Player");

            settingsData.ApplyChanges();
        }
    }

    private void LoadVisualCategory(VisualSettingsSO.CategorySettings category, string tag)
    {
        if (category == null) return;
        
        int mode = PlayerPrefs.GetInt(tag + "_GraphicsMode", 0);
        if (mode == 1) // Custom Mode
        {
            // Default assumes a value of 1f. Match this to your defaultBrightness if different.
            category.brightness = PlayerPrefs.GetFloat(tag + "_Brightness", 1f); 
            category.contrast = PlayerPrefs.GetFloat(tag + "_Contrast", 1f);
            category.color = PlayerPrefs.GetFloat(tag + "_Color", 1f);
        }
        else // Default Mode
        {
            category.brightness = 1f;
            category.contrast = 1f;
            category.color = 1f;
        }
    }

    public void PlayBeta()
    {
        // Check if they need the tutorial or the real game, then load both flawlessly
        if (PlayerPrefs.GetInt("tutorialCompleted", 0) == 1)
        {
            S18_LoadingManager.Instance.LoadScenes("PersistentGameplay", "GYM_BetaGame");
        }
        else
        {
            S18_LoadingManager.Instance.LoadScenes("PersistentGameplay", "GYM_TutorialLevel");
        }
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

    public void OpenVisualAccessibility()
    {
        SceneManager.LoadScene("GYM_VisualAccessibility", LoadSceneMode.Single);
    }
}