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
        StartCoroutine(LoadMultipleScenesAsync(sceneNames));
    }

    private IEnumerator LoadMultipleScenesAsync(string[] sceneNames)
    {
        Time.timeScale = 1f;
        
        loadingPanel.SetActive(true);
        UpdateProgressBlocks(0f); // Reset blocks to 0

        float totalScenes = sceneNames.Length;

        for (int i = 0; i < sceneNames.Length; i++)
        {
            LoadSceneMode mode = (i == 0)
                ? LoadSceneMode.Single
                : LoadSceneMode.Additive;

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneNames[i], mode);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
            {
                float sceneProgress = op.progress / 0.9f;
                float overallProgress = (i + sceneProgress) / totalScenes;

                UpdateProgressBlocks(overallProgress);

                yield return null;
            }

            op.allowSceneActivation = true;

            while (!op.isDone)
            {
                yield return null;
            }

            Debug.Log("Successfully loaded scene: " + sceneNames[i]);
        }

        // Set the LAST loaded scene as the active scene.
        Scene gameplayScene = SceneManager.GetSceneByName(sceneNames[sceneNames.Length - 1]);

        if (gameplayScene.IsValid() && gameplayScene.isLoaded)
        {
            SceneManager.SetActiveScene(gameplayScene);

            Debug.Log("Active Scene Set To: " + SceneManager.GetActiveScene().name);
        }
        else
        {
            Debug.LogError("Could not set the gameplay scene as active!");
        }

        UpdateProgressBlocks(1f);

        yield return new WaitForSecondsRealtime(0.4f);

        loadingPanel.SetActive(false);

        UpdateProgressBlocks(1f); // Force 100%

        yield return new WaitForSecondsRealtime(0.4f);
        
        loadingPanel.SetActive(false);
    }

    // Helper method to toggle the blocks based on progress percentage
    private void UpdateProgressBlocks(float progress)
    {
        int activeBlocks = Mathf.RoundToInt(progress * progressBlocks.Length);

        for (int i = 0; i < progressBlocks.Length; i++)
        {
            progressBlocks[i].SetActive(i < activeBlocks);
        }
    }
}