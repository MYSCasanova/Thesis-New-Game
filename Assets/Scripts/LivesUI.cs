using UnityEngine;
using TMPro;

public class LivesUI : MonoBehaviour
{
    public TextMeshProUGUI livesText;
    public S7_HealthSystem healthSystem;

    void Update()
    {
        livesText.text = "Lives: " + healthSystem.GetCurrentLives();
    }
}