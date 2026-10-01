using UnityEngine;

public class S6_Checkpoint : MonoBehaviour
{
    public static S6_Checkpoint Instance;

    private static Vector3 checkpointPosition;
    private static Vector3 cameraCheckpoint;

    private static Vector3 initialPlayerCheckpoint;
    private static Vector3 initialCameraCheckpoint;

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

            Debug.Log("Initial Player Checkpoint: " + initialPlayerCheckpoint);
            Debug.Log("Initial Camera Checkpoint: " + initialCameraCheckpoint);
    }

    public void SetCheckpoint(Vector3 newPosition)
    {
        checkpointPosition = newPosition;

        if (Camera.main != null)
        {
            cameraCheckpoint = Camera.main.transform.position;
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