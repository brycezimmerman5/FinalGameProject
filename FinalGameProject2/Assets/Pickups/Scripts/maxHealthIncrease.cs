using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class maxHealthIncrease : MonoBehaviour
{
    public float healthIncreaseAmount = 10f; 

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                //Increase Max Health
                playerHealth.AddHealth(healthIncreaseAmount); 
                Destroy(gameObject);
            }
        }
    }
}
