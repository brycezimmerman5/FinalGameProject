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
    }

    [System.Serializable]
    public class Wave
    {
        public string waveName;
        public EnemyEntry[] enemies;
        public float spawnRate;
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

        foreach (var entry in wave.enemies)
        {
            for (int i = 0; i < entry.count; i++)
            {
                SpawnEnemy(entry.enemyPrefab);
                yield return new WaitForSeconds(1f / wave.spawnRate);
            }
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
