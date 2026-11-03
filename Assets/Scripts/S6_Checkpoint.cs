using UnityEngine;

public class S6_Checkpoint : MonoBehaviour
{
    public static S6_Checkpoint Instance;

    private static Vector3 checkpointPosition;
    private static Vector3 cameraCheckpoint;

    private static Vector3 initialPlayerCheckpoint;
    private static Vector3 initialCameraCheckpoint;

    [Header("Camera Safety")]
    [Tooltip("Ensures the player is at least this many units above the bottom of the screen when respawning at a checkpoint.")]
    public float bottomBuffer = 2.5f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            initialPlayerCheckpoint = player.transform.position;
            checkpointPosition = initialPlayerCheckpoint;
        }

        if (Camera.main != null)
        {
            initialCameraCheckpoint = Camera.main.transform.position;
            cameraCheckpoint = initialCameraCheckpoint;
        }
    }

    public void SetCheckpoint(Vector3 newPosition)
    {
        checkpointPosition = newPosition;

        if (Camera.main != null)
        {
            Camera cam = Camera.main;
            Vector3 currentCamPos = cam.transform.position;
            float orthoSize = cam.orthographicSize;

            float cameraBottom = currentCamPos.y - orthoSize;

            // If the camera had autonomously scrolled too high when the player touched the checkpoint,
            // adjust the saved camera position down to guarantee a safe bottom buffer upon respawn.
            if (newPosition.y < cameraBottom + bottomBuffer)
            {
                currentCamPos.y = newPosition.y + orthoSize - bottomBuffer;
            }

            cameraCheckpoint = currentCamPos;
        }
    }

    public Vector3 GetCheckpointPosition()
    {
        return checkpointPosition;
    }

    public Vector3 GetCameraCheckpoint()
    {
        return cameraCheckpoint;
    }

    public Vector3 GetInitialPlayerCheckpoint()
    {
        return initialPlayerCheckpoint;
    }

    public Vector3 GetInitialCameraCheckpoint()
    {
        return initialCameraCheckpoint;
    }

    public void ResetToInitialCheckpoint()
    {
        checkpointPosition = initialPlayerCheckpoint;
        cameraCheckpoint = initialCameraCheckpoint;

        Debug.Log("CHECKPOINT RESET TO LEVEL 1 START");
    }
}