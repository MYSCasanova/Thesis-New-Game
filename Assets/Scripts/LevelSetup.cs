using UnityEngine;

public class LevelSetup : MonoBehaviour
{
    public int levelNumber;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        CameraFollow camera = FindFirstObjectByType<CameraFollow>();

        if (camera != null)
        {
            camera.currentLevel = levelNumber;
        }
    }
}