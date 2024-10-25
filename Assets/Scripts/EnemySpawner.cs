using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float minSpawnRadius = 5f;
    public float maxSpawnRadius = 10f;
    public float spawnRate = 1f; // Enemies per second

    private float timeToNextSpawn;

    private void Update()
    {
        float spawnInterval = 1f / spawnRate; // Calculate the interval between spawns dynamically
        timeToNextSpawn -= Time.deltaTime;

        if (timeToNextSpawn <= 0)
        {
            SpawnEnemy();
            timeToNextSpawn = spawnInterval; // Reset the timer based on the spawn interval
        }
    }

    void SpawnEnemy()
    {
        float distance = Random.Range(minSpawnRadius, maxSpawnRadius);
        float angle = Random.Range(0f, 2f * Mathf.PI);

        Vector3 spawnPos = new Vector3(
            transform.position.x + distance * Mathf.Cos(angle),
            transform.position.y,
            transform.position.z + distance * Mathf.Sin(angle)
        );

        Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
    }

    // Draw the spawn area in the Scene view
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        // Draw a circle to represent the spawn area
        Gizmos.DrawWireSphere(transform.position, maxSpawnRadius);
        Gizmos.DrawWireSphere(transform.position, minSpawnRadius);
    }
}
