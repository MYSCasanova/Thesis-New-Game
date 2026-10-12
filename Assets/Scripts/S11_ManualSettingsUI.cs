using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class S11_ManualSettingsUI : MonoBehaviour
{
    public VisualSettingsSO settingsData;

    [System.Serializable]
    public class GraphicsCategoryUI
    {
        public string categoryTag; // Ex: "Background", "Hazard", "Platform", "Player"
        public Slider brightnessSlider;
        public Slider contrastSlider;
        public Slider colorSlider;
    }

    [Header("Graphics Settings")]
    public GraphicsCategoryUI backgroundUI = new GraphicsCategoryUI { categoryTag = "Background" };
    public GraphicsCategoryUI hazardUI = new GraphicsCategoryUI { categoryTag = "Hazard" };
    public GraphicsCategoryUI platformUI = new GraphicsCategoryUI { categoryTag = "Platform" };
    public GraphicsCategoryUI playerUI = new GraphicsCategoryUI { categoryTag = "Player" };

    [Header("Volume Settings")]
    public AudioMixer audioMixer; // Assign your AudioMixer here
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Resolution Settings")]
    public Dropdown resolutionDropdown; // Use TMP_Dropdown if you are using TextMeshPro
    public Toggle fullScreenToggle;

    private Resolution[] resolutions;

    private void Start()
    {
        InitializeGraphics();
        InitializeVolume();
        InitializeResolution();
    }

    // --- GRAPHICS TAB LOGIC ---
    private void InitializeGraphics()
    {
        SetupGraphicsCategory(backgroundUI, settingsData.backgroundSettings);
        SetupGraphicsCategory(hazardUI, settingsData.hazardSettings);
        SetupGraphicsCategory(platformUI, settingsData.platformSettings);
        SetupGraphicsCategory(playerUI, settingsData.playerSettings);
    }

    private void SetupGraphicsCategory(GraphicsCategoryUI ui, VisualSettingsSO.CategorySettings currentSettings)
    {
        if (ui.brightnessSlider == null) return;

        // Load saved values
        ui.brightnessSlider.value = currentSettings.brightness;
        ui.contrastSlider.value = currentSettings.contrast;
        ui.colorSlider.value = currentSettings.color;

        // Add listeners
        ui.brightnessSlider.onValueChanged.AddListener(val => UpdateGraphicsCategory(ui, currentSettings));
        ui.contrastSlider.onValueChanged.AddListener(val => UpdateGraphicsCategory(ui, currentSettings));
        ui.colorSlider.onValueChanged.AddListener(val => UpdateGraphicsCategory(ui, currentSettings));

        // Apply initially
        ApplyVisualsToTag(ui.categoryTag, currentSettings);
    }

    private void UpdateGraphicsCategory(GraphicsCategoryUI ui, VisualSettingsSO.CategorySettings currentSettings)
    {
        currentSettings.brightness = ui.brightnessSlider.value;
        currentSettings.contrast = ui.contrastSlider.value;
        currentSettings.color = ui.colorSlider.value;

        ApplyVisualsToTag(ui.categoryTag, currentSettings);
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

    // --- VOLUME TAB LOGIC ---
    private void InitializeVolume()
    {
        if (musicSlider != null)
        {
            float currentMusicVol;
            if (audioMixer != null && audioMixer.GetFloat("MusicVolume", out currentMusicVol))
                musicSlider.value = currentMusicVol;
                
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (sfxSlider != null)
        {
            float currentSFXVol;
            if (audioMixer != null && audioMixer.GetFloat("SFXVolume", out currentSFXVol))
                sfxSlider.value = currentSFXVol;

            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }
    }

    public void SetMusicVolume(float volume)
    {
        if (audioMixer != null) audioMixer.SetFloat("MusicVolume", volume);
    }

    public void SetSFXVolume(float volume)
    {
        if (audioMixer != null) audioMixer.SetFloat("SFXVolume", volume);
    }

    // --- RESOLUTION TAB LOGIC ---
    private void InitializeResolution()
    {
        if (resolutionDropdown == null || fullScreenToggle == null) return;

        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            // Check against current resolution or fallback to 1920x1080 default
            if (resolutions[i].width == Screen.currentResolution.width && 
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        resolutionDropdown.onValueChanged.AddListener(SetResolution);

        fullScreenToggle.isOn = Screen.fullScreen;
        fullScreenToggle.onValueChanged.AddListener(SetFullscreen);
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
}