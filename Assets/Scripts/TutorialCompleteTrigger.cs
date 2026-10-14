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

        Debug.Log("Tutorial completed!");
    }

    // Link this method to your "Done" Button's OnClick event in the Inspector
    public void OnDoneButtonClicked()
    {
        // Find the SceneTransition script in the scene
        SceneTransition transition = FindFirstObjectByType<SceneTransition>();
        
        if (transition != null)
        {
            transition.FinishTutorialFlow();
        }
        else
        {
            Debug.LogError("SceneTransition script not found in the scene!");
        }
    }
}