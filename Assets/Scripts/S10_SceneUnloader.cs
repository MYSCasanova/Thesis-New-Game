using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class S10_SceneUnloader : MonoBehaviour
{
    [SerializeField] private SceneField sceneToUnload;
    private Boolean hasUnloaded = false;

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (hasUnloaded) return;

        hasUnloaded = true;

        Debug.Log("Unloading Previous Room");

        SceneManager.UnloadSceneAsync(sceneToUnload);
    }
}