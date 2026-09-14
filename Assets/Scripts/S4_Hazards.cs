using UnityEngine;

public class S4_Hazards : MonoBehaviour
{
    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Player hit a hazard!");
        }
    }
}
