using UnityEngine;

//FOR DEBUGGING

public class CheckPlayerScene : MonoBehaviour
{
    void Start()
    {
        Debug.Log("PLAYER SCENE: " + gameObject.scene.name);
    }

    void OnDestroy()
    {
        Debug.Log("PLAYER WAS DESTROYED!");
    }
}