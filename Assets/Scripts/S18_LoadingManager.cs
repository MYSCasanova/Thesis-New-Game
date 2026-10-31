using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class S18_LoadingManager : MonoBehaviour
{
    public static S18_LoadingManager Instance;

    [Header("Loading UI Elements")]
    public GameObject loadingPanel;
    
    // Kept exactly as is for your 26 blocks
    public GameObject[] progressBlocks; 

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

    public void LoadScenes(params string[] sceneNames)
    {
        // Standard loads start at 0%
        StartCoroutine(LoadMultipleScenesAsync(0f, sceneNames));
    }

    // --- NEW METHOD ---
    public void LoadScenesFromProgress(float startProgress, params string[] sceneNames)
    {
        // Continuous loads start from whatever percentage we tell it
        StartCoroutine(LoadMultipleScenesAsync(startProgress, sceneNames));
    }

    private IEnumerator LoadMultipleScenesAsync(float startProgress, string[] sceneNames)
    {
        Time.timeScale = 1f;
        
        loadingPanel.SetActive(true);
        
        // Only reset to 0 if this is a fresh, standard load
        if (startProgress <= 0f) UpdateProgressBlocks(0f); 

        float totalScenes = sceneNames.Length;
        float remainingProgress = 1f - startProgress; // How much of the bar is left to fill

        for (int i = 0; i < sceneNames.Length; i++)
        {
            LoadSceneMode mode = (i == 0) ? LoadSceneMode.Single : LoadSceneMode.Additive;
            
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneNames[i], mode);
            op.allowSceneActivation = false; 

            while (op.progress < 0.9f)
            {
                float sceneProgress = op.progress / 0.9f;
                float overallProgress = (i + sceneProgress) / totalScenes;
                
                // Scale the scene loading progress into the remaining visual space
                UpdateProgressBlocks(startProgress + (overallProgress * remainingProgress)); 

                yield return null;
            }

            op.allowSceneActivation = true;
            
            while (!op.isDone)
            {
                yield return null;
            }
        }

        UpdateProgressBlocks(1f); // Force 100%

        yield return new WaitForSecondsRealtime(0.4f);
        
        loadingPanel.SetActive(false);
    }

    private IEnumerator LoadMultipleScenesAsync(string[] sceneNames)
    {
        Time.timeScale = 1f;
        
        loadingPanel.SetActive(true);
        UpdateProgressBlocks(0f); // Reset blocks to 0

        float totalScenes = sceneNames.Length;

        for (int i = 0; i < sceneNames.Length; i++)
        {
            LoadSceneMode mode = (i == 0) ? LoadSceneMode.Single : LoadSceneMode.Additive;
            
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneNames[i], mode);
            op.allowSceneActivation = false; 

            while (op.progress < 0.9f)
            {
                float sceneProgress = op.progress / 0.9f;
                float overallProgress = (i + sceneProgress) / totalScenes;
                
                UpdateProgressBlocks(overallProgress); // Update the array

                yield return null;
            }

            op.allowSceneActivation = true;
            
            while (!op.isDone)
            {
                yield return null;
            }
        }

        UpdateProgressBlocks(1f); // Force 100%

        yield return new WaitForSecondsRealtime(0.4f);
        
        loadingPanel.SetActive(false);
    }

    // Helper method to toggle the blocks based on progress percentage
    // Change this from private to public
    public void UpdateProgressBlocks(float progress)
    {
        int activeBlocks = Mathf.RoundToInt(progress * progressBlocks.Length);

        for (int i = 0; i < progressBlocks.Length; i++)
        {
            progressBlocks[i].SetActive(i < activeBlocks);
        }
    }
}