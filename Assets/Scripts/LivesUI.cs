using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class LivesUI : MonoBehaviour
{
    // public TextMeshProUGUI livesText;
    // public S7_HealthSystem healthSystem;
    public Image[] hearts;

    public void UpdateHearts(int currentLives)
    {
        if (hearts == null)
        {
            Debug.LogError("LivesUI: Hearts array is missing!");
            return;
        }

        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] != null)
            {
                hearts[i].enabled = i < currentLives;
            }
            else
            {
                Debug.LogError("LivesUI: Heart at index " + i + " is missing!");
            }
        }
    }
    // void Update()
    // {
    //     livesText.text = "Lives: " + healthSystem.GetCurrentLives();
    // }
}