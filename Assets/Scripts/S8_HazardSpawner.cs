using UnityEngine;

public class S8_HazardSpawner : MonoBehaviour
{
    public GameObject fallingObject;

    public float minX = -5f;
    public float maxX = 5f;

    public float minSpawnTime = 1f;
    public float maxSpawnTime = 3f;

    public float objectLifetime = 3f;

    void Start()
    {
        Invoke(nameof(SpawnObject), Random.Range(minSpawnTime, maxSpawnTime));
    }

    void SpawnObject()
    {
        float randomX = Random.Range(minX, maxX);

        Vector3 spawnPosition = new Vector3(
            randomX,
            transform.position.y,
            transform.position.z
        );

        GameObject spawnedObject = Instantiate(fallingObject, spawnPosition, Quaternion.identity);

        Destroy(spawnedObject, objectLifetime);

        Invoke(nameof(SpawnObject), Random.Range(minSpawnTime, maxSpawnTime));
    }
}