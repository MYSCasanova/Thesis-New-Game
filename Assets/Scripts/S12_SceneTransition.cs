using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransition : MonoBehaviour
{
    private void Start()
    {
        // Only start the flow from PersistentGameplay.
        if (gameObject.scene.name == "PersistentGameplay")
        {
            StartBetaFlow();
        }
    }

    public void StartBetaFlow()
    {
        bool tutorialCompleted = PlayerPrefs.GetInt("tutorialCompleted", 0) == 1;

        Debug.Log("Tutorial Completed: " + tutorialCompleted);

        if (tutorialCompleted)
        {
            // Returning player: go straight to Beta Game.
            LoadBetaGame();
        }
        else
        {
            // First-time player: load the tutorial.
            StartCoroutine(LoadTutorial());
        }
    }

    public void LoadBetaGame()
    {
        StartCoroutine(TransitionToBetaGame());
    }

    private IEnumerator TransitionToBetaGame()
    {
        Scene betaScene = SceneManager.GetSceneByName("GYM_BetaGame");

        if (!betaScene.isLoaded)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync("GYM_BetaGame", LoadSceneMode.Additive);
            yield return operation;
            betaScene = SceneManager.GetSceneByName("GYM_BetaGame");
        }

        // Find the level spawn point in the BetaGame scene
        GameObject spawnPoint = GameObject.Find("Level1Spawn");

        if (spawnPoint == null)
        {
            Debug.LogError("Level1Spawn not found!");
            yield break;
        }

        // FInd the PersistentGameplay scene and get the player and camera
        Scene persistentScene = SceneManager.GetSceneByName("PersistentGameplay");

        if (!persistentScene.isLoaded)
        {
            Debug.LogError("PersistentGameplay is not loaded!");
            yield break;
        }

        GameObject player = null;
        CameraFollow cameraFollow = null;

        foreach (GameObject root in persistentScene.GetRootGameObjects())
        {
            if (root.name == "--- PLAYER ---")
                player = root;

            S2_ComboSystem foundComboSystem = root.GetComponentInChildren<S2_ComboSystem>(true);

            if (root.name == "Main Camera")
                cameraFollow = root.GetComponent<CameraFollow>();
        }

        // Find the Game UI and ComboSystem in the BetaGame scene
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

        if (player == null || cameraFollow == null || gameUI == null || comboSystem == null || livesUI == null || gameOverPanel == null)
        {
            Debug.LogError(
                "Missing reference. Player: " + (player != null) +
                ", CameraFollow: " + (cameraFollow != null) +
                ", Game UI: " + (gameUI != null) +
                ", ComboSystem: " + (comboSystem != null) +
                ", LivesUI: " + (livesUI != null) +
                ", GameOverPanel: " + (gameOverPanel != null)
                );

            yield break;
        }

        SceneManager.SetActiveScene(betaScene); // Set the active scene to BetaGame

        player.transform.SetParent(null); // Detach player from any parent
        player.transform.position = spawnPoint.transform.position; // Set player position to spawn point

        // Reset player stats
        S1_PlayerController playerController = player.GetComponent<S1_PlayerController>();

        if (playerController != null)
        {
            playerController.comboSystem = comboSystem; // Assign the combo system to the player
            playerController.ResetRun(); // Reset the player's stats
        }
        else
        {
            Debug.LogError("S1_PlayerController not found on Player!");
        }

        comboSystem.player = playerController; // Assign the player controller to the combo system

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (S6_Checkpoint.Instance != null)
        {
            S6_Checkpoint.Instance.SetCheckpoint(spawnPoint.transform.position);
        }
        else
        {
            Debug.LogError("Checkpoint Manager not found!");
        }

        // Reset the camera to follow the player
        cameraFollow.enabled = true;
        cameraFollow.player = player.transform;
        Vector3 cameraStartPosition = cameraFollow.transform.position;
        cameraStartPosition.y = spawnPoint.transform.position.y + 5f;
        cameraFollow.ResetCamera(cameraStartPosition);

        // Reset the score and lives before showing the UI
        comboSystem.scoreText.text = "Score: 0";
        comboSystem.comboText.text = "Combo: 0";

        S7_HealthSystem healthSystem = player.GetComponent<S7_HealthSystem>();

        if (healthSystem != null)
        {
            healthSystem.currentLives = healthSystem.maxLives;
            healthSystem.comboSystem = comboSystem; // Assign the combo system to the health system
            livesUI.healthSystem = healthSystem; // Assign the health system to the LivesUI


                if (gameOverPanel != null)
                {
                    Transform panelTransform = gameOverPanel.transform;
                    Transform scoreTransform = panelTransform.Find("Score");

                    if (scoreTransform != null)
                    {
                        healthSystem.scoreText = scoreTransform.GetComponent<TMPro.TMP_Text>();
                    }
                    else
                    {
                        Debug.LogError("FinalScoreText not found!");
                    }
                }
                else
                {
                    Debug.LogError("GameOverPanel not found!");
                }
        }

        
        else
        {
            Debug.LogError("S7_HealthSystem not found on Player!");
        }

        gameUI.SetActive(true);

        Scene tutorialScene = SceneManager.GetSceneByName("GYM_TutorialLevel");

        if (tutorialScene.isLoaded)
            yield return SceneManager.UnloadSceneAsync(tutorialScene);

        Debug.Log("Transition to BetaGame complete.");
    }

    private IEnumerator LoadTutorial()
    {
        Scene tutorial = SceneManager.GetSceneByName("GYM_TutorialLevel");

        if (!tutorial.isLoaded)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync("GYM_TutorialLevel",LoadSceneMode.Additive);

            yield return operation;

            tutorial = SceneManager.GetSceneByName("GYM_TutorialLevel");
        }

        SceneManager.SetActiveScene(tutorial);

        Debug.Log("Tutorial loaded and set as active scene.");
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("GYM_MainMenu", LoadSceneMode.Single);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
