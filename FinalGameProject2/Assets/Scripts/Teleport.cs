using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Teleport : MonoBehaviour
{
    //CHANGE THIS TO WHATEVER SCENE/LEVEL TO BE LOADED AFTER FINISHING THIS ONE
    public string sceneName = "City";
    public bool gameEndFlag = false; // turn this true if you want to move to game end scene
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
        // Find out what hit the goal
        Debug.Log("Entered trigger with: " + other.name);
        if (other.CompareTag("Player")){
            // Load the next level       
            if (gameEndFlag)
            {
                //Not implemented Yet
                //GameManager.isGameOver = true;
            }
            if(wavespawner != null && wavespawner.wavesCompleted && wavespawner.activeEnemies <= 0)
            {
                SceneManager.LoadScene(sceneName);
            }
        }
    }
}
