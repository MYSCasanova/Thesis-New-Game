
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransition : MonoBehaviour
{
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

        GameObject spawnPoint = GameObject.Find("Level1Spawn");

        if (spawnPoint == null)
        {
            Debug.LogError("Level1Spawn not found!");
            yield break;
        }


        Scene persistentScene = SceneManager.GetSceneByName("PersistentGameplay");

        if (!persistentScene.isLoaded)
        {
            Debug.LogError("PersistentGameplay is not loaded!");
            yield break;
        }

        GameObject player = null;
        GameObject gameUI = null;
        CameraFollow cameraFollow = null;

        foreach (GameObject root in persistentScene.GetRootGameObjects())
        {
            if (root.name == "--- PLAYER ---")
                player = root;

            if (root.name == "--- GAME UI ---")
                gameUI = root;

            if (root.name == "Main Camera")
                cameraFollow = root.GetComponent<CameraFollow>();
        }

        if (player == null || gameUI == null || cameraFollow == null)
        {
            Debug.LogError("Could not find Player, Game UI, or CameraFollow on Main Camera in PersistentGameplay.");
            yield break;
        }

        SceneManager.SetActiveScene(betaScene);

        player.transform.position = spawnPoint.transform.position;

        S1_PlayerController playerController = player.GetComponent<S1_PlayerController>();

        if (playerController != null)
        {
            playerController.ResetRun();
        }
        else
        {
            Debug.LogError("S1_PlayerController not found on Player!");
        }

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

        cameraFollow.enabled = true;
        cameraFollow.player = player.transform;
        Vector3 cameraStartPosition = cameraFollow.transform.position;
        cameraStartPosition.y = spawnPoint.transform.position.y  + 5f;
        cameraFollow.ResetCamera(cameraStartPosition);

        // Reset the score and lives before showing the UI
        S2_ComboSystem comboSystem = gameUI.GetComponentInChildren<S2_ComboSystem>(true);

        if (comboSystem != null)
        {
            comboSystem.totalScore = 0;
            comboSystem.currentCombo = 0;
            comboSystem.scoreMultiplier = 1;

            comboSystem.scoreText.text = "Score: 0";
            comboSystem.comboText.text = "Combo: 0";
        }
        else
        {
            Debug.LogError("S2_ComboSystem not found in Game UI!");
        }

        S7_HealthSystem healthSystem = player.GetComponent<S7_HealthSystem>();

        if (healthSystem != null)
        {
            healthSystem.currentLives = healthSystem.maxLives;
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

    public void QuitGame()
    {
        Application.Quit();
    }
}
