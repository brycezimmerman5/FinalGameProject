using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Teleport : MonoBehaviour
{
    private WaveSpawner wavespawner;

    void Start()
    {
        // Automatically find the WaveSpawner in the scene
        wavespawner = FindObjectOfType<WaveSpawner>();
        if (wavespawner == null)
        {
            Debug.LogError("No WaveSpawner found in the scene!");
        }
    }

    void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")){
            if(wavespawner != null && wavespawner.wavesCompleted && wavespawner.activeEnemies <= 0)
            {
                // Load the next scene by incrementing the build index
                int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
                if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
                {
                    SceneManager.LoadScene(nextSceneIndex);
                }
                else
                {
                    Debug.LogWarning("No more scenes in build settings!");
                }
            }
        }
    }
}
