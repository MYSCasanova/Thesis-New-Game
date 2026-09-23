using UnityEngine;
using UnityEngine.SceneManagement;

public class S9_SceneLoader : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private SceneField currentScene;
    [SerializeField] private SceneField sceneToLoad;

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log("Loading Next Room");

        SceneManager.sceneLoaded += OnSceneLoaded;

        SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);

        SceneManager.UnloadSceneAsync(currentScene);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != sceneToLoad.SceneName) return;

        SceneManager.sceneLoaded -= OnSceneLoaded;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
    }
}