using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Visuals & FX")]
    public GameObject deathEffectPrefab;
    public Animator animator;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;

        if (!animator)
            animator = GetComponent<Animator>();
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log($"Player took {amount} damage. Current health: {currentHealth}");

        // Optional: trigger a hurt animation or flash effect
        // animator.SetTrigger("Hurt");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;

        Debug.Log("Player died.");

        // Optional: play death animation
        if (animator)
            animator.SetTrigger("Die");

        if (deathEffectPrefab)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        // Disable controls (example: PlayerController)
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
            controller.enabled = false;

        // Disable character controller and collider (optional)
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;

        // Optional: add respawn logic here
        Invoke("UnityEngine.SceneManagement.ReloadScene()", 4f);
    }
}
