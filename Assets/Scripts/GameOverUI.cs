using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] GameObject gameOverCanvas;

    public void TryAgain()
    {
        StartCoroutine(RestartLevel1());
    }

    private IEnumerator RestartLevel1()
    {
        // Save Level 1 starting position BEFORE unloading anything
        Vector3 level1PlayerStart = S6_Checkpoint.Instance.GetInitialPlayerCheckpoint();
        Vector3 level1CameraStart = S6_Checkpoint.Instance.GetInitialCameraCheckpoint();

        // Unload Level 2, Level 3, etc.
        for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            Scene scene = SceneManager.GetSceneAt(i);

            if (scene.name.StartsWith("GYM_Level") && scene.name != "GYM_Level1")
            {
                Debug.Log("Unloading " + scene.name);

                AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);

                yield return unload;
            }
        }

        // Check if Level 1 is already loaded
        Scene level1Scene = SceneManager.GetSceneByName("GYM_Level1");

        if (!level1Scene.isLoaded)
        {

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync("GYM_Level1", LoadSceneMode.Additive);

            yield return loadOperation;

        }

        // Find persistent player
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            // Reset lives
            S7_HealthSystem health = player.GetComponent<S7_HealthSystem>();

            if (health != null)
            {
                health.ResetLives();
            }

            // Reset checkpoint
            player.transform.position = level1PlayerStart;

            // Stop player movement
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            // Reset player controller
            S1_PlayerController controller = player.GetComponent<S1_PlayerController>();

            if (controller != null)
            {
                controller.StopRespawning();
                controller.ResetMultiplier();
            }
        }

        // Reset camera
        if (Camera.main != null && S6_Checkpoint.Instance != null)
        {
            CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();

            if (cameraFollow != null)
            {
                cameraFollow.ResetCamera(level1CameraStart);
            }
        }

        // Hide Game Over UI
        gameOverCanvas.SetActive(false);

        Debug.Log("RETURNED TO LEVEL 1 START");
    }
}