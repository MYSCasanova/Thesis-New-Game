using UnityEngine;

public class TutorialCompleteTrigger : MonoBehaviour
{
    public GameObject instructionText;
    public GameObject tutorialCompletePanel;

    private bool triggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (triggered || !collision.CompareTag("Player"))
            return;

        triggered = true;

        instructionText.SetActive(false);
        tutorialCompletePanel.SetActive(true);

        CameraFollow cameraFollowScript = FindFirstObjectByType<CameraFollow>();

        if (cameraFollowScript != null)
        {
            cameraFollowScript.enabled = false; // Disable the camera follow script
        }

        Debug.Log("Tutorial completed!");
    }
}