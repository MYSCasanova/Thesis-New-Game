using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; 

public class S13_ManualSettingsUI : MonoBehaviour
{
    public VisualSettingsSO settingsData;

    [Header("Graphics UI Generation")]
    public Transform graphicsContentParent;
    public GameObject categoryPrefab;

    [Header("Default Graphics Values")]
    public float defaultBrightness = 1f; 
    public float defaultContrast = 1f;   
    public float defaultColor = 1f;      

    [Header("Video Settings UI")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullScreenToggle;
    public Toggle windowedToggle;

    [Header("Audio Settings UI")]
    public UnityEngine.Audio.AudioMixer audioMixer;
    public Slider musicSlider;
    public Slider sfxSlider;

    private Resolution[] filteredResolutions; // Only stores 16:9 resolutions
    private float targetAspectRatio = 16f / 9f;
    
    private void Start()
    {
        // Clear existing UI elements
        foreach (Transform child in graphicsContentParent) {
            Destroy(child.gameObject);
        }

        // Generate graphics categories
        GenerateCategory("Adjust Background Visual", "Background", settingsData.backgroundSettings);
        GenerateCategory("Adjust Obstacle Visual", "Obstacle", settingsData.obstacleSettings);
        GenerateCategory("Adjust Platform Visual", "Platform", settingsData.platformSettings);
        GenerateCategory("Adjust Player Visual", "Player", settingsData.playerSettings);

        // Initialize Video Settings
        InitializeVideoSettings();
        // Initialize Audio Settings
        InitializeAudioSettings();
    }

    private void GenerateCategory(string title, string tagToFind, VisualSettingsSO.CategorySettings currentSettings)
    {
        // 1. Spawn a new Prefab inside the Content Parent
        GameObject newCategory = Instantiate(categoryPrefab, graphicsContentParent);
        S14_CategoryUI uiElements = newCategory.GetComponent<S14_CategoryUI>();

        // 2. Change the title text
        if (uiElements.headerText != null) uiElements.headerText.text = title;

        // 3. Dropdown Logic (Default vs Custom)
        if (uiElements.modeDropdown != null)
        {
            uiElements.modeDropdown.ClearOptions();
            uiElements.modeDropdown.AddOptions(new List<string> { "Default", "Custom" });

            // Load saved mode (0 = Default, 1 = Custom)
            int savedMode = PlayerPrefs.GetInt(tagToFind + "_GraphicsMode", 0);
            uiElements.modeDropdown.value = savedMode;

            // Set initial slider interactable state
            bool isCustom = (savedMode == 1);
            uiElements.brightnessSlider.interactable = isCustom;
            uiElements.contrastSlider.interactable = isCustom;
            uiElements.colorSlider.interactable = isCustom;

            // Load initial values based on mode
            if (!isCustom)
            {
                currentSettings.brightness = defaultBrightness;
                currentSettings.contrast = defaultContrast;
                currentSettings.color = defaultColor;
            }
            else
            {
                // FIX: Load saved custom visual values from PlayerPrefs
                currentSettings.brightness = PlayerPrefs.GetFloat(tagToFind + "_Brightness", defaultBrightness);
                currentSettings.contrast = PlayerPrefs.GetFloat(tagToFind + "_Contrast", defaultContrast);
                currentSettings.color = PlayerPrefs.GetFloat(tagToFind + "_Color", defaultColor);
            }

            // Listen for dropdown changes
            uiElements.modeDropdown.onValueChanged.AddListener(val => {
                PlayerPrefs.SetInt(tagToFind + "_GraphicsMode", val);
                bool customSelected = (val == 1);
                
                // Enable or disable sliders based on selection
                uiElements.brightnessSlider.interactable = customSelected;
                uiElements.contrastSlider.interactable = customSelected;
                uiElements.colorSlider.interactable = customSelected;

                if (!customSelected) 
                {
                    uiElements.brightnessSlider.value = defaultBrightness;
                    uiElements.contrastSlider.value = defaultContrast;
                    uiElements.colorSlider.value = defaultColor;
                }
                else
                {
                    // FIX: Restore saved custom values when switching back to Custom
                    uiElements.brightnessSlider.value = PlayerPrefs.GetFloat(tagToFind + "_Brightness", defaultBrightness);
                    uiElements.contrastSlider.value = PlayerPrefs.GetFloat(tagToFind + "_Contrast", defaultContrast);
                    uiElements.colorSlider.value = PlayerPrefs.GetFloat(tagToFind + "_Color", defaultColor);
                }
                
                // Apply defaults or loaded values immediately
                UpdateVisuals(tagToFind, uiElements, currentSettings);
                
                if (S16_AudioManager.instance != null) S16_AudioManager.instance.PlaySFX("UI Buttons Click");
            });
        }

        // 4. Load the saved values into the sliders
        uiElements.brightnessSlider.value = currentSettings.brightness;
        uiElements.contrastSlider.value = currentSettings.contrast;
        uiElements.colorSlider.value = currentSettings.color;

        // 5. Add listeners to update settings when sliders are moved
        // FIX: Pass the tagToFind parameter so it knows what PlayerPrefs key to save to
        uiElements.brightnessSlider.onValueChanged.AddListener(val => UpdateVisuals(tagToFind, uiElements, currentSettings));
        uiElements.contrastSlider.onValueChanged.AddListener(val => UpdateVisuals(tagToFind, uiElements, currentSettings));
        uiElements.colorSlider.onValueChanged.AddListener(val => UpdateVisuals(tagToFind, uiElements, currentSettings));
    }

    // FIX: Add 'string tagToFind' parameter to save values persistently
    private void UpdateVisuals(string tagToFind, S14_CategoryUI ui, VisualSettingsSO.CategorySettings currentSettings)
    {
        currentSettings.brightness = ui.brightnessSlider.value;
        currentSettings.contrast = ui.contrastSlider.value;
        currentSettings.color = ui.colorSlider.value;

        // FIX: Save actual slider values to PlayerPrefs
        PlayerPrefs.SetFloat(tagToFind + "_Brightness", currentSettings.brightness);
        PlayerPrefs.SetFloat(tagToFind + "_Contrast", currentSettings.contrast);
        PlayerPrefs.SetFloat(tagToFind + "_Color", currentSettings.color);

        settingsData.ApplyChanges(); 
    }

    private void ApplyVisualsToTag(string tagToFind, VisualSettingsSO.CategorySettings activeSettings)
    {
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tagToFind);
        foreach (GameObject obj in taggedObjects)
        {
            SpriteRenderer targetSprite = obj.GetComponent<SpriteRenderer>();
            if (targetSprite != null && targetSprite.material != null)
            {
                targetSprite.material.SetFloat("_Brightness", activeSettings.brightness);
                targetSprite.material.SetFloat("_Contrast", activeSettings.contrast);
                targetSprite.material.SetFloat("_Saturation", activeSettings.color);
            }
        }
    }
    
    // --- VIDEO SETTINGS LOGIC ---

    private void InitializeVideoSettings()
    {
        if (resolutionDropdown == null || fullScreenToggle == null || windowedToggle == null) return;

        Resolution[] allResolutions = Screen.resolutions;
        
        // REVERSE ORDER: Highest resolution at the top (index 0)
        System.Array.Reverse(allResolutions); 

        List<Resolution> ratioResolutions = new List<Resolution>();
        List<string> options = new List<string>();

        for (int i = 0; i < allResolutions.Length; i++)
        {
            float ratio = (float)allResolutions[i].width / (float)allResolutions[i].height;
            if (Mathf.Abs(ratio - targetAspectRatio) < 0.05f)
            {
                string option = allResolutions[i].width + " x " + allResolutions[i].height;
                if (!options.Contains(option))
                {
                    ratioResolutions.Add(allResolutions[i]);
                    options.Add(option);
                }
            }
        }

        filteredResolutions = ratioResolutions.ToArray();
        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);

        // --- LOADING SETTINGS OR DEFAULT ---
        // Retrieve saved data. If none exists (first launch), default to 0 (Highest Res) and 1 (Fullscreen)
        int savedResIndex = PlayerPrefs.GetInt("SavedResolution", 0); 
        bool isFullscreen = PlayerPrefs.GetInt("SavedFullscreen", 1) == 1;

        // Fallback in case the player changed monitors and the previous resolution index is out of bounds
        if (savedResIndex >= filteredResolutions.Length) savedResIndex = 0;

        // Update UI to match the loaded settings
        resolutionDropdown.value = savedResIndex;
        resolutionDropdown.RefreshShownValue();
        fullScreenToggle.isOn = isFullscreen;
        windowedToggle.isOn = !isFullscreen;

        // Apply video settings immediately upon opening the game
        ApplyVideoSettings(savedResIndex, isFullscreen);

        // --- LISTENERS ---
        resolutionDropdown.onValueChanged.AddListener(val => {
            ApplyVideoSettings(val, fullScreenToggle.isOn);
            if (S16_AudioManager.instance != null) S16_AudioManager.instance.PlaySFX("UI Buttons Click");
        });
        
        fullScreenToggle.onValueChanged.AddListener(delegate {
            if (fullScreenToggle.isOn)
            {
                windowedToggle.isOn = false;
                ApplyVideoSettings(resolutionDropdown.value, true);
                if (S16_AudioManager.instance != null) S16_AudioManager.instance.PlaySFX("UI Buttons Click");
            }
        });

        windowedToggle.onValueChanged.AddListener(delegate {
            if (windowedToggle.isOn)
            {
                fullScreenToggle.isOn = false;
                ApplyVideoSettings(resolutionDropdown.value, false);
                if (S16_AudioManager.instance != null) S16_AudioManager.instance.PlaySFX("UI Buttons Click");
            }
        });
    }

    private void ApplyVideoSettings(int resIndex, bool isFullscreen)
    {
        if (filteredResolutions == null || filteredResolutions.Length == 0) return;
        
        Resolution resolution = filteredResolutions[resIndex];
        Screen.SetResolution(resolution.width, resolution.height, isFullscreen);
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = filteredResolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        
        // Optional: Re-apply the current resolution when switching screen modes 
        // to ensure the window resizes correctly to the 16:9 ratio.
        Resolution resolution = filteredResolutions[resolutionDropdown.value];
        Screen.SetResolution(resolution.width, resolution.height, isFullscreen);
    }
    
    // --- SAVE BUTTON LOGIC ---
    public void SaveAllSettings()
    {
        // Video Settings
        PlayerPrefs.SetInt("SavedResolution", resolutionDropdown.value);
        PlayerPrefs.SetInt("SavedFullscreen", fullScreenToggle.isOn ? 1 : 0);
        
        // Audio Settings
        if (musicSlider != null && sfxSlider != null)
        {
            PlayerPrefs.SetFloat("SavedMusicVol", musicSlider.value);
            PlayerPrefs.SetFloat("SavedSFXVol", sfxSlider.value);
        }
        
        PlayerPrefs.Save();

        if (settingsData != null) settingsData.ApplyChanges();
        
        if (S16_AudioManager.instance != null) S16_AudioManager.instance.PlaySFX("UI Buttons Click");
        
        Debug.Log("All settings have been saved!");
    }
    
    // --- AUDIO SETTINGS LOGIC ---
    private void InitializeAudioSettings()
    {
        if (musicSlider == null || sfxSlider == null || audioMixer == null) return;

        // Retrieve saved values (default to 1 for max volume)
        float savedMusic = PlayerPrefs.GetFloat("SavedMusicVol", 1f);
        float savedSFX = PlayerPrefs.GetFloat("SavedSFXVol", 1f);

        musicSlider.value = savedMusic;
        sfxSlider.value = savedSFX;

        SetMusicVolume(savedMusic);
        SetSFXVolume(savedSFX);

        // Add listeners
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    private void SetMusicVolume(float volume)
    {
        // Convert linear slider value to logarithmic decibel scale
        float dbVolume = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f;
        audioMixer.SetFloat("MusicVolume", dbVolume);
    }

    private void SetSFXVolume(float volume)
    {
        float dbVolume = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f;
        audioMixer.SetFloat("SFXVolume", dbVolume);
    }
    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("GYM_MainMenu", LoadSceneMode.Single);
    }
}