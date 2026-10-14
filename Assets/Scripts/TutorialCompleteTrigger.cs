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

        // Set the PlayerPrefs value to indicate that the tutorial has been completed
        PlayerPrefs.SetInt("tutorialCompleted", 1);
        PlayerPrefs.Save();

        Debug.Log(PlayerPrefs.GetInt("tutorialCompleted", 0) == 1);

        Debug.Log("Tutorial completed!");
    }
}