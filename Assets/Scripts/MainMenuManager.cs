using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Audio;
using UnityEngine.UI; // Required for AudioMixer

public class MainMenuManager : MonoBehaviour
{
    public static string previousSceneName;

    [Header("Settings Data Initialization")]
    public VisualSettingsSO settingsData; 
    public AudioMixer audioMixer;

    [Header("Buttons")]
    public Button v2Button;
    public Button v3Button;

    [Header("Panels")]
    public GameObject menuPanel;
    public GameObject creditsPanel;
    public GameObject leaderboardPanel;
    public GameObject v1Button;

    private void Start()
    {
        ApplySavedSettingsOnLoad();
        UpdateButtons();

        // For debugging
        // PlayerPrefs.SetInt("tutorialCompleted", 1);
        // PlayerPrefs.DeleteKey("tutorialCompleted");
        // PlayerPrefs.DeleteKey("v1Completed");
        // PlayerPrefs.DeleteKey("v2Completed");
        // PlayerPrefs.Save();
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
        
        // 1. Match the exact default values from S13_ManualSettingsUI
        float targetDefaultBrightness = 1f;
        float targetDefaultContrast = 1f;
        float targetDefaultColor = 1f;

        switch (tag)
        {
            case "Background":
                targetDefaultBrightness = 2f;
                targetDefaultContrast = 0.8960935f;
                targetDefaultColor = 0f;
                break;
            case "Obstacle":
                targetDefaultBrightness = 1.290718f;
                targetDefaultContrast = 0.8276349f;
                targetDefaultColor = 0f;
                break;
            case "Platform":
                targetDefaultBrightness = 0.5678623f;
                targetDefaultContrast = 0.8216149f;
                targetDefaultColor = 0f;
                break;
            case "Player":
                targetDefaultBrightness = 0.9364931f;
                targetDefaultContrast = 0.9023179f;
                targetDefaultColor = 0f;
                break;
        }

        int mode = PlayerPrefs.GetInt(tag + "_GraphicsMode", 0);
        
        // 2. Safely load values based on the saved mode
        if (mode == 0) // Default
        {
            category.brightness = targetDefaultBrightness;
            category.contrast = targetDefaultContrast;
            category.color = targetDefaultColor;
        }
        else if (mode == 1) // Custom Mode
        {
            category.brightness = PlayerPrefs.GetFloat(tag + "_Brightness", targetDefaultBrightness); 
            category.contrast = PlayerPrefs.GetFloat(tag + "_Contrast", targetDefaultContrast);
            category.color = PlayerPrefs.GetFloat(tag + "_Color", targetDefaultColor);
        }
        else if (mode == 2) // AIPreset #2
        {
            category.brightness = PlayerPrefs.GetFloat(tag + "_AIPresetV2_Brightness", targetDefaultBrightness);
            category.contrast = PlayerPrefs.GetFloat(tag + "_AIPresetV2_Contrast", targetDefaultContrast);
            category.color = PlayerPrefs.GetFloat(tag + "_AIPresetV2_Color", targetDefaultColor);
        }
        else if (mode == 3) // AIPreset #3
        {
            category.brightness = PlayerPrefs.GetFloat(tag + "_AIPresetV3_Brightness", targetDefaultBrightness);
            category.contrast = PlayerPrefs.GetFloat(tag + "_AIPresetV3_Contrast", targetDefaultContrast);
            category.color = PlayerPrefs.GetFloat(tag + "_AIPresetV3_Color", targetDefaultColor);
        }
    }

    public void PlayBeta()
    {
        // Restore to their Custom/Default settings before loading Beta/Tutorial
        RestoreManualGraphicsModes();

        if (PlayerPrefs.GetInt("tutorialCompleted", 0) == 1)
        {
            S18_LoadingManager.Instance.LoadScenes("PersistentGameplay", "GYM_BetaGame");
        }
        else
        {
            S18_LoadingManager.Instance.LoadScenes("PersistentGameplay", "GYM_TutorialLevel");
        }
    }

    public void PlayVersion2()
    {
        if (PlayerPrefs.GetInt("v1Completed", 0) == 1)
        {
            // Force AIPreset #2 before loading
            ForceGraphicsMode(2);
            S18_LoadingManager.Instance.LoadScenes("PersistentGameplay", "GYM_V2Game");
        }
    }

    public void PlayVersion3()
    {
        if (PlayerPrefs.GetInt("v2Completed", 0) == 1)
        {
            // Force AIPreset #3 before loading
            ForceGraphicsMode(3);
            S18_LoadingManager.Instance.LoadScenes("PersistentGameplay", "GYM_V3Game");
        }
    }

    public void OpenTutorialGallery()
    {
        // Restore manual modes so any tutorial levels entered from the gallery use Custom/Default
        RestoreManualGraphicsModes(); 
        SceneManager.LoadScene("GYM_TutorialGallery", LoadSceneMode.Single);
    }

    private void RestoreManualGraphicsModes()
    {
        string[] tags = { "Background", "Obstacle", "Platform", "Player" };
        foreach (string tag in tags)
        {
            // Fetch what they had set before playing V2/V3 (Default=0, Custom=1)
            int lastManualMode = PlayerPrefs.GetInt(tag + "_LastManualMode", 0);
            
            // Failsafe: if it's not 0 or 1, force it to 0
            if (lastManualMode != 0 && lastManualMode != 1) lastManualMode = 0; 
            
            PlayerPrefs.SetInt(tag + "_GraphicsMode", lastManualMode);
        }
        PlayerPrefs.Save();

        // Immediately apply the restored settings to the VisualSettingsSO
        if (settingsData != null)
        {
            LoadVisualCategory(settingsData.backgroundSettings, "Background");
            LoadVisualCategory(settingsData.obstacleSettings, "Obstacle");
            LoadVisualCategory(settingsData.platformSettings, "Platform");
            LoadVisualCategory(settingsData.playerSettings, "Player");
            settingsData.ApplyChanges();
        }
    }

    private void ForceGraphicsMode(int targetMode)
    {
        string[] tags = { "Background", "Obstacle", "Platform", "Player" };
        foreach (string tag in tags)
        {
            int currentMode = PlayerPrefs.GetInt(tag + "_GraphicsMode", 0);
            
            // Backup their manual choice if they are currently using Default/Custom
            if (currentMode == 0 || currentMode == 1)
            {
                PlayerPrefs.SetInt(tag + "_LastManualMode", currentMode);
            }

            PlayerPrefs.SetInt(tag + "_GraphicsMode", targetMode);
        }
        PlayerPrefs.Save();

        // Immediately apply the AI preset settings to the VisualSettingsSO
        if (settingsData != null)
        {
            LoadVisualCategory(settingsData.backgroundSettings, "Background");
            LoadVisualCategory(settingsData.obstacleSettings, "Obstacle");
            LoadVisualCategory(settingsData.platformSettings, "Platform");
            LoadVisualCategory(settingsData.playerSettings, "Player");
            settingsData.ApplyChanges();
        }
    }

    public void BackToMainMenu()
    {
        creditsPanel.SetActive(false);
        leaderboardPanel.SetActive(false);
        menuPanel.SetActive(true);
    }

    public void OpenCredits()
    {
        menuPanel.SetActive(false);
        creditsPanel.SetActive(true);
    }

    public void OpenLeaderboard()
    {
        menuPanel.SetActive(false);
        leaderboardPanel.SetActive(true);
        v1Button.GetComponent<LeaderboardButton>().ShowLeaderboard();
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
        // Only unpause if the player is in the Main Menu.
        // If in-game, leave time at 0 so the Pause Panel remains properly paused.
        if (SceneManager.GetActiveScene().name == "GYM_MainMenu")
        {
            Time.timeScale = 1f;
        }
        
        SceneManager.UnloadSceneAsync("GYM_ManualSettings");
    }

    public void OpenVisualAccessibility()
    {
        SceneManager.LoadScene("GYM_VisualAccessibility", LoadSceneMode.Single);
    }

    private void UpdateButtons()
    {
        // Enable Version 2 Button if Version 1 is completed
        bool v1Completed = PlayerPrefs.GetInt("v1Completed", 0) == 1;

        if (v2Button != null)
        {
            v2Button.interactable = v1Completed;
        }

        // Enable Version 3 Button if Version 2 is completed
        bool v2Completed = PlayerPrefs.GetInt("v2Completed", 0) == 1;

        if (v3Button != null)
        {
            v3Button.interactable = v2Completed;
        }
    }
}