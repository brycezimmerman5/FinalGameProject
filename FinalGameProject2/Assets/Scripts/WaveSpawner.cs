using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WaveSpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemyEntry
    {
        public GameObject enemyPrefab;
        public int count;
        public float spawnRate = 1f; // Time in seconds between spawns
    }

    [System.Serializable]
    public class Wave
    {
        public string waveName;
        public EnemyEntry[] enemies;
    }

    public Wave[] waves;

    [Header("NavMesh Spawn Area")]
    public Vector3 centerPoint = Vector3.zero;
    public float spawnRadius = 30f;

    private int currentWaveIndex = 0;

    [Header("Wave Settings")]
    public float timeBetweenWaves = 5f;

    void Start()
    {
        StartCoroutine(BeginWaves());
    }

    IEnumerator BeginWaves()
    {
        yield return new WaitForSeconds(2f); // initial delay

        while (currentWaveIndex < waves.Length)
        {
            yield return StartCoroutine(SpawnWave(waves[currentWaveIndex]));
            currentWaveIndex++;
            yield return new WaitForSeconds(timeBetweenWaves);
        }

        Debug.Log("All waves completed.");
    }

    IEnumerator SpawnWave(Wave wave)
    {
        Debug.Log("Spawning Wave: " + wave.waveName);

        // Create a list to track remaining enemies for each type
        List<EnemyEntry> remainingEnemies = new List<EnemyEntry>();
        foreach (var entry in wave.enemies)
        {
            remainingEnemies.Add(new EnemyEntry
            {
                enemyPrefab = entry.enemyPrefab,
                count = entry.count,
                spawnRate = entry.spawnRate
            });
        }

        // Track last spawn time for each enemy type
        Dictionary<GameObject, float> lastSpawnTimes = new Dictionary<GameObject, float>();

        // Continue until all enemies are spawned
        while (remainingEnemies.Count > 0)
        {
            float currentTime = Time.time;

            // Check each enemy type
            for (int i = remainingEnemies.Count - 1; i >= 0; i--)
            {
                var entry = remainingEnemies[i];
                
                // Initialize last spawn time if not set
                if (!lastSpawnTimes.ContainsKey(entry.enemyPrefab))
                {
                    lastSpawnTimes[entry.enemyPrefab] = -entry.spawnRate;
                }

                // Check if it's time to spawn this enemy type
                if (currentTime - lastSpawnTimes[entry.enemyPrefab] >= entry.spawnRate)
                {
                    SpawnEnemy(entry.enemyPrefab);
                    lastSpawnTimes[entry.enemyPrefab] = currentTime;
                    entry.count--;

                    // Remove entry if all enemies of this type are spawned
                    if (entry.count <= 0)
                    {
                        remainingEnemies.RemoveAt(i);
                    }
                }
            }

            yield return null; // Wait for next frame
        }
    }

    void SpawnEnemy(GameObject enemyPrefab)
    {
        Vector3 spawnPos;

        if (GetRandomPointOnNavMesh(centerPoint, spawnRadius, out spawnPos))
        {
            Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("Failed to find NavMesh position for enemy spawn.");
        }
    }

    bool GetRandomPointOnNavMesh(Vector3 center, float radius, out Vector3 result)
    {
        for (int i = 0; i < 30; i++) // try up to 30 times
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * radius;
            randomPoint.y = center.y;

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }
}
