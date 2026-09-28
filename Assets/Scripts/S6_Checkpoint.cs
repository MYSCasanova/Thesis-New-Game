using UnityEngine;

public class S6_Checkpoint : MonoBehaviour
{
    public static S6_Checkpoint Instance;
    private static Vector3 checkpointPosition;
    private static Vector3 cameraCheckpoint;
    private static bool hasCheckpoint = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (hasCheckpoint) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            checkpointPosition = player.transform.position;
        }

        if (Camera.main != null)
        {
            cameraCheckpoint = Camera.main.transform.position;
        }

        hasCheckpoint = true;
    }

    public void SetCheckpoint(Vector3 newPosition)
    {
        checkpointPosition = newPosition;

        if (Camera.main != null)
        {
            cameraCheckpoint = Camera.main.transform.position;
        }

        Debug.Log("Checkpoint saved");
        Debug.Log("PLAYER CHECKPOINT: " + checkpointPosition);
        Debug.Log("CAMERA CHECKPOINT: " + cameraCheckpoint);
    }

    public Vector3 GetCheckpointPosition()
    {
        return checkpointPosition;
    }

    public Vector3 GetCameraCheckpoint()
    {
        return cameraCheckpoint;
    }
}