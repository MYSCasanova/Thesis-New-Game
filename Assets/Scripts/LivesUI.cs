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
        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].enabled = i < currentLives;
        }
    }
    // void Update()
    // {
    //     livesText.text = "Lives: " + healthSystem.GetCurrentLives();
    // }
}