using UnityEngine;

public class S7_HealthSystem : MonoBehaviour
{
    public int maxLives;
    private int currentLives;

    void Start()
    {
        currentLives = maxLives;
    }

    public void LoseLife()
    {
        currentLives--;

        Debug.Log("Lost a life. Current lives: " + currentLives);

        if(currentLives <= 0)
        {
            Debug.Log("Game Over");
        }

        //game over loggic
    }
    
    public int GetCurrentLives()
    {
        return currentLives;
    }
}
