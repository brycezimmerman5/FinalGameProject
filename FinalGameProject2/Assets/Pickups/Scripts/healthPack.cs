using UnityEngine;

public class healthPack : MonoBehaviour
{
    public float healAmount = 20f; 

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null && playerHealth.currentHealth < playerHealth.maxHealth)
            {
                playerHealth.Heal(healAmount); 
                Destroy(gameObject);
            }
        }
    }
}
