using UnityEngine;
using UnityEngine.SceneManagement;

public class S15_VisualReceiver : MonoBehaviour
{
    public VisualSettingsSO settingsData;
    
    public enum Category { Background, Obstacle, Platform, Player }
    
    [Header("Object Category")]
    public Category myCategory;

    // Must match the "Default Graphics Values" on S13_ManualSettingsUI
    private const float DefaultBrightness = 1f;
    private const float DefaultContrast = 1f;
    private const float DefaultColor = 1f;

    // Remembers which scene instance already loaded its preset, so the load runs once per scene
    // and later-spawned objects (obstacles, platforms) don't overwrite live slider changes.
    private static int loadedSceneHandle = -1;

    // Settings asset seen by any receiver; lets the sceneLoaded hook work even when the
    // receivers live in a persistent scene that stays enabled between game versions.
    private static VisualSettingsSO cachedData;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        loadedSceneHandle = -1;
        cachedData = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private SpriteRenderer mySprite;

    private void Awake()
    {
        mySprite = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        // 0. Load the preset for the game scene from PlayerPrefs (no visit to Settings required)
        EnsurePresetLoaded(settingsData, gameObject.scene);

        // 1. Subscribe to the event when this object is enabled or spawned in the scene
        settingsData.OnSettingsChanged += UpdateMyVisuals;
        
        // 2. Apply the correct visual adjustments immediately upon spawning
        UpdateMyVisuals(); 
    }

    private void OnDisable()
    {
        // Unsubscribe from the event when disabled or destroyed to prevent memory leaks and missing reference errors
        settingsData.OnSettingsChanged -= UpdateMyVisuals;
    }

    public void UpdateMyVisuals()
    {
        if (mySprite == null || mySprite.material == null) return;

        // Retrieve the correct settings data based on this object's specific category
        VisualSettingsSO.CategorySettings currentSettings = GetMySettings();

        // Apply the saved values directly to the shader material properties
        mySprite.material.SetFloat("_Brightness", currentSettings.brightness);
        mySprite.material.SetFloat("_Contrast", currentSettings.contrast);
        mySprite.material.SetFloat("_Saturation", currentSettings.color);
    }

    private VisualSettingsSO.CategorySettings GetMySettings()
    {
        // Return the corresponding settings from the ScriptableObject
        switch (myCategory)
        {
            case Category.Obstacle: return settingsData.obstacleSettings;
            case Category.Platform: return settingsData.platformSettings;
            case Category.Player: return settingsData.playerSettings;
            case Category.Background: 
            default: return settingsData.backgroundSettings;
        }
    }

    // ---------------------------------------------------------------------
    // AUTO-LOAD OF SAVED PRESETS (mirrors the scene rules in S13_ManualSettingsUI)
    // ---------------------------------------------------------------------

    private static bool IsGameScene(string sceneName)
    {
        return sceneName == "GYM_V2Game" || sceneName == "GYM_V3Game" || sceneName == "GYM_BetaGame";
    }

    // The game version scene is usually loaded ADDITIVELY next to "PersistentGameplay", so
    // SceneManager.GetActiveScene() is not reliable at this point. Find the real game scene instead.
    private static Scene ResolveGameScene(Scene receiverScene)
    {
        if (IsGameScene(receiverScene.name)) return receiverScene;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (IsGameScene(s.name)) return s;
        }
        return SceneManager.GetActiveScene();
    }

    private static void EnsurePresetLoaded(VisualSettingsSO data, Scene receiverScene)
    {
        if (data == null) return;
        cachedData = data;
        LoadForScene(data, ResolveGameScene(receiverScene));
    }

    // Fires after every scene load. Covers receivers that were already enabled (for example in
    // PersistentGameplay) and therefore never run OnEnable again when the next game version loads.
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (cachedData == null || !IsGameScene(scene.name)) return;
        LoadForScene(cachedData, scene);
    }

    private static void LoadForScene(VisualSettingsSO data, Scene scene)
    {
        if (scene.handle == loadedSceneHandle) return;
        loadedSceneHandle = scene.handle;

        int[] allowedModes = GetAllowedModes(scene.name);

        LoadCategory("Background", data.backgroundSettings, allowedModes);
        LoadCategory("Obstacle", data.obstacleSettings, allowedModes);
        LoadCategory("Platform", data.platformSettings, allowedModes);
        LoadCategory("Player", data.playerSettings, allowedModes);

        data.ApplyChanges();
    }

    // null = every mode is allowed (Main Menu / other scenes)
    private static int[] GetAllowedModes(string sceneName)
    {
        if (sceneName == "GYM_V2Game") return new[] { 2 };      // AIPreset #2
        if (sceneName == "GYM_V3Game") return new[] { 3 };      // AIPreset #3
        if (sceneName == "GYM_BetaGame") return new[] { 0, 1 }; // Default, Custom
        return null;
    }

    private static void LoadCategory(string tag, VisualSettingsSO.CategorySettings settings, int[] allowedModes)
    {
        int mode = PlayerPrefs.GetInt(tag + "_GraphicsMode", 0);

        // Same override S13 applies: force the first allowed preset if the saved one isn't valid here
        if (allowedModes != null && System.Array.IndexOf(allowedModes, mode) < 0)
        {
            mode = allowedModes[0];
            PlayerPrefs.SetInt(tag + "_GraphicsMode", mode);
        }

        switch (mode)
        {
            case 1: // Custom
                settings.brightness = PlayerPrefs.GetFloat(tag + "_Brightness", DefaultBrightness);
                settings.contrast = PlayerPrefs.GetFloat(tag + "_Contrast", DefaultContrast);
                settings.color = PlayerPrefs.GetFloat(tag + "_Color", DefaultColor);
                break;
            case 2: // AIPreset #2
                settings.brightness = PlayerPrefs.GetFloat(tag + "_AIPresetV2_Brightness", DefaultBrightness);
                settings.contrast = PlayerPrefs.GetFloat(tag + "_AIPresetV2_Contrast", DefaultContrast);
                settings.color = PlayerPrefs.GetFloat(tag + "_AIPresetV2_Color", DefaultColor);
                break;
            case 3: // AIPreset #3
                settings.brightness = PlayerPrefs.GetFloat(tag + "_AIPresetV3_Brightness", DefaultBrightness);
                settings.contrast = PlayerPrefs.GetFloat(tag + "_AIPresetV3_Contrast", DefaultContrast);
                settings.color = PlayerPrefs.GetFloat(tag + "_AIPresetV3_Color", DefaultColor);
                break;
            default: // 0 = Default
                switch (tag)
                {
                    case "Background":
                        settings.brightness = 2f;
                        settings.contrast = 0.8960935f;
                        settings.color = 0f;
                        break;
                    case "Obstacle":
                        settings.brightness = 1.290718f;
                        settings.contrast = 0.8276349f;
                        settings.color = 0f;
                        break;
                    case "Platform":
                        settings.brightness = 0.5678623f;
                        settings.contrast = 0.8216149f;
                        settings.color = 0f;
                        break;
                    case "Player":
                        settings.brightness = 0.9364931f;
                        settings.contrast = 0.9023179f;
                        settings.color = 0f;
                        break;
                    default:
                        settings.brightness = DefaultBrightness;
                        settings.contrast = DefaultContrast;
                        settings.color = DefaultColor;
                        break;
                }
                break;
        }
    }
}