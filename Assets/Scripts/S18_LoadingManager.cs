using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class S18_LoadingManager : MonoBehaviour
{
    public static S18_LoadingManager Instance;

    [Header("Loading UI Elements")]
    public GameObject loadingPanel;
    public Slider progressBar;
    public TMP_Text progressText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null)
        {
            GameObject prefab = Resources.Load<GameObject>("LoadingManager");
            if (prefab != null) Instantiate(prefab);
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            loadingPanel.SetActive(false); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Accepts 1 scene, or multiple scenes separated by commas
    public void LoadScenes(params string[] sceneNames)
    {
        StartCoroutine(LoadMultipleScenesAsync(sceneNames));
    }

    private IEnumerator LoadMultipleScenesAsync(string[] sceneNames)
    {
        Time.timeScale = 1f;
        
        // Lock the screen with the loading overlay
        loadingPanel.SetActive(true);
        progressBar.value = 0f;
        if (progressText != null) progressText.text = "Loading: 0%";

        float totalScenes = sceneNames.Length;

        // Loop through and load every requested scene behind the overlay
        for (int i = 0; i < sceneNames.Length; i++)
        {
            // The first scene is loaded Single (wiping the menu), the rest are Additive
            LoadSceneMode mode = (i == 0) ? LoadSceneMode.Single : LoadSceneMode.Additive;
            
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneNames[i], mode);
            op.allowSceneActivation = false; // Prevent it from rendering immediately

            while (op.progress < 0.9f)
            {
                // Calculate progress smoothly across all scenes
                float sceneProgress = op.progress / 0.9f;
                float overallProgress = (i + sceneProgress) / totalScenes;
                
                progressBar.value = overallProgress;
                if (progressText != null) progressText.text = "Loading: " + Mathf.RoundToInt(overallProgress * 100f) + "%";

                yield return null;
            }

            // Let this specific scene activate silently in the background
            op.allowSceneActivation = true;
            
            // Wait for Unity to confirm it is fully injected before moving to the next
            while (!op.isDone)
            {
                yield return null;
            }
        }

        // Force UI to 100% just in case of rounding errors
        progressBar.value = 1f;
        if (progressText != null) progressText.text = "Loading: " + "100%";

        // Brief delay so the player registers the load is complete before the screen flashes away
        yield return new WaitForSecondsRealtime(0.4f);
        
        // Remove the overlay, revealing the fully loaded game
        loadingPanel.SetActive(false);
    }
}