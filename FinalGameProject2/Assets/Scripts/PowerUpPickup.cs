using UnityEngine;

public class PowerUpPickup : MonoBehaviour
{
    public PowerUp powerUp;

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && powerUp != null)
        {
            player.ApplyPowerUp(powerUp);
            Destroy(gameObject);
        }
    }
}
