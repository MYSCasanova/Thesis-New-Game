using UnityEngine;

public class S6_Checkpoint : MonoBehaviour
{
    public static S6_Checkpoint Instance;

    private static Vector3 checkpointPosition;
    private static Vector3 cameraCheckpoint;

    private static Vector3 initialPlayerCheckpoint;
    private static Vector3 initialCameraCheckpoint;

    private static bool initialCheckpointSaved = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Save the starting position of Level 1 only once
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "GYM_Level1" && !initialCheckpointSaved)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                initialPlayerCheckpoint = player.transform.position;
                checkpointPosition = player.transform.position;
            }

            if (Camera.main != null)
            {
                initialCameraCheckpoint = Camera.main.transform.position;
                cameraCheckpoint = Camera.main.transform.position;
            }

            initialCheckpointSaved = true;

            Debug.Log("LEVEL 1 START SAVED");
            Debug.Log("PLAYER START: " + initialPlayerCheckpoint);
            Debug.Log("CAMERA START: " + initialCameraCheckpoint);
        }
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