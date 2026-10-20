using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransition : MonoBehaviour
{
    public static bool isFromGallery = false;

    private void Start()
    {
        // Only run logic if we are the persistent base scene
        if (gameObject.scene.name == "PersistentGameplay")
        {
            // Subscribe to the sceneLoaded event so we know when the LoadingManager finishes loading the additive scenes
            SceneManager.sceneLoaded += OnAdditiveSceneLoaded;
        }
    }

    private void OnDestroy()
    {
        // Clean up the listener
        SceneManager.sceneLoaded -= OnAdditiveSceneLoaded;
    }

    private void OnAdditiveSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive)
        {
            if (scene.name == "GYM_BetaGame")
            {
                StartCoroutine(LinkBetaGame(scene));
            }
            else if (scene.name == "GYM_TutorialLevel")
            {
                StartCoroutine(LinkTutorial(scene));
            }
        }
    }

    public void FinishTutorialFlow()
    {
        if (isFromGallery)
        {
            isFromGallery = false;
            // Tell the new Loading Manager to transition smoothly
            S18_LoadingManager.Instance.LoadScenes("GYM_TutorialGallery");
        }
        else
        {
            // If playing normally, use the Loading Manager to switch out the Tutorial and load the Beta
            S18_LoadingManager.Instance.LoadScenes("PersistentGameplay", "GYM_BetaGame");
        }
    }

    private IEnumerator LinkBetaGame(Scene betaScene)
    {
        GameObject spawnPoint = GameObject.Find("Level1Spawn");
        if (spawnPoint == null)
        {
            Debug.LogError("Level1Spawn not found!");
            yield break;
        }

        Scene persistentScene = SceneManager.GetSceneByName("PersistentGameplay");
        GameObject player = null;
        CameraFollow cameraFollow = null;

        foreach (GameObject root in persistentScene.GetRootGameObjects())
        {
            if (root.name == "--- PLAYER ---") player = root;
            if (root.name == "Main Camera") cameraFollow = root.GetComponent<CameraFollow>();
        }

        GameObject gameUI = null;
        S2_ComboSystem comboSystem = null;
        LivesUI livesUI = null;
        GameObject gameOverPanel = null;

        foreach (GameObject root in betaScene.GetRootGameObjects())
        {
            if (root.name == "--- GAME UI ---")
            {
                gameUI = root;
                comboSystem = root.GetComponent<S2_ComboSystem>();
                livesUI = root.GetComponent<LivesUI>();
                gameOverPanel = root.transform.Find("GameOverPanel")?.gameObject;
            }
        }

        if (player == null || cameraFollow == null || gameUI == null) yield break;

        SceneManager.SetActiveScene(betaScene); 

        player.transform.SetParent(null); 
        player.transform.position = spawnPoint.transform.position; 

        S1_PlayerController playerController = player.GetComponent<S1_PlayerController>();
        if (playerController != null)
        {
            playerController.comboSystem = comboSystem; 
            playerController.ResetRun(); 
        }

        comboSystem.player = playerController; 

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (S6_Checkpoint.Instance != null)
        {
            S6_Checkpoint.Instance.SetCheckpoint(spawnPoint.transform.position);
        }

        cameraFollow.enabled = true;
        cameraFollow.player = player.transform;
        Vector3 cameraStartPosition = cameraFollow.transform.position;
        cameraStartPosition.y = spawnPoint.transform.position.y + 5f;
        cameraFollow.ResetCamera(cameraStartPosition);

        comboSystem.scoreText.text = "0";
        
        S7_HealthSystem healthSystem = player.GetComponent<S7_HealthSystem>();
        if (healthSystem != null)
        {
            healthSystem.currentLives = healthSystem.maxLives;
            healthSystem.comboSystem = comboSystem; 
            healthSystem.livesUI = livesUI;
            livesUI.UpdateHearts(healthSystem.currentLives);

            if (gameOverPanel != null)
            {
                Transform scoreTransform = gameOverPanel.transform.Find("ScoreNumber");
                if (scoreTransform != null) healthSystem.scoreText = scoreTransform.GetComponent<TMPro.TMP_Text>();
            }
        }

        gameUI.SetActive(true);
        Debug.Log("Link to BetaGame complete.");
    }
    public void ReturnToMainMenu()
    {
        // Unpause time in case the player clicks this from the Pause Menu
        Time.timeScale = 1f;
        
        // Use the new Loading Screen
        S18_LoadingManager.Instance.LoadScenes("GYM_MainMenu");
    }

    private IEnumerator LinkTutorial(Scene tutorialScene)
    {
        // Freeze time instantly so gameplay doesn't start while UI loads
        Time.timeScale = 0f;

        // Since LoadingManager loads things sequentially, we just need to load the UI on top
        Scene basicUI = SceneManager.GetSceneByName("GYM_BasicUI");
        if (!basicUI.isLoaded)
        {
            AsyncOperation uiOp = SceneManager.LoadSceneAsync("GYM_BasicUI", LoadSceneMode.Additive);
            yield return uiOp;
            basicUI = SceneManager.GetSceneByName("GYM_BasicUI");
        }

        SceneManager.SetActiveScene(basicUI);
        Debug.Log("Tutorial Link Complete. BasicUI active.");
    }
}