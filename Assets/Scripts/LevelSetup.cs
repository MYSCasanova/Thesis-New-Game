using UnityEngine;

public class LevelSetup : MonoBehaviour
{
    public int levelNumber;

    void Start()
    {
        CameraFollow camera = FindFirstObjectByType<CameraFollow>();

        if (camera != null)
        {
            camera.currentLevel = levelNumber;
        }
    }
}