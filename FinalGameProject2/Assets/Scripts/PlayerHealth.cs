using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Visuals & FX")]
    public GameObject deathEffectPrefab;
    public Animator animator;

    [Header("Audio")]
    public AudioClip hitSound;
    private AudioSource audioSource;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;

        if (!animator)
            animator = GetComponent<Animator>();

        audioSource = GetComponent<AudioSource>();
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        
        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound); // play sound
        }

        //For UI
        UIhealth.Instance.TakeDamage(amount / 10);
        Debug.Log($"Player took {amount} damage. Current health: {currentHealth}");

        // Optional: trigger a hurt animation or flash effect
        // animator.SetTrigger("Hurt");

        if (currentHealth <= 0)
        {
            Die();
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Projectile"))
        {
            Bullet b = other.GetComponent<Bullet>();
            if (b != null)
            {
                TakeDamage(b.damage);
                Destroy(other.gameObject);
            }
        }
    }
    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth += amount;
        //currentHealth = Mathf.Min(currentHealth, maxHealth); // Don't exceed maxHealth
        Debug.Log($"Player healed {amount}. Current health: {currentHealth}");

        // Update UI
        UIhealth.Instance.Heal(amount / 10);
    }

    //INCREASES MAX HEALTH
    public void AddHealth(float amount)
    {
        if (isDead) return;

        // Calculate the percentage of current health before increasing max
        float healthPercentage = currentHealth / maxHealth;
        
        // Increase max health
        maxHealth += amount;
        
        // Set current health to maintain the same percentage
        currentHealth = maxHealth * healthPercentage;

        // Update UI
        UIhealth.Instance.AddHealth();
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
        Invoke("ReloadScene", 3f);
    }
    // Method to reload the scene
    void ReloadScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
