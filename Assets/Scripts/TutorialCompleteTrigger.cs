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

        // Disable the CameraFollow script to stop camera movement when the panel appears
        if (Camera.main != null)
        {
            CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
            if (cameraFollow != null)
            {
                cameraFollow.enabled = false;
            }
        }

        Debug.Log("Tutorial completed!");
    }
}