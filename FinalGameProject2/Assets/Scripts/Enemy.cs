using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    private float currentHealth;
    private bool isDead = false;

    [Header("Attack Settings")]
    public float attackRange = 2f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.5f;
    private float lastAttackTime;
    private bool isAttacking = false;

    [Header("References")]
    public Animator animator;
    public GameObject deathEffectPrefab;
    private Transform player;
    private NavMeshAgent agent;

    //Drops for when an enemy is killed
    [Header("Drop Settings")]
    public GameObject healthPackPrefab; 
    public GameObject maxHealthIncreasePrefab;
    [Range(0f, 1f)]
    public float healthPackDropChance = 0.2f;
    [Range(0f, 1f)]
    public float maxHealthIncreaseDropChance = 0.05f;


    public GameObject bloodSplatterPrefab;

    void Start()
    {
        currentHealth = maxHealth;

        if (!animator) animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        EnableRagdoll(false);
    }

    private bool isRunning = false;

    void Update()
    {
        if (isDead || player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange)
        {
            agent.isStopped = true;

            if (!isAttacking && Time.time - lastAttackTime >= attackCooldown)
            {
                StartCoroutine(PerformAttack());
            }

            SetRunning(false); // Stop running animation
        }
        else
        {
            if (!isAttacking)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
                SetRunning(true); // Start running animation
            }
        }
    }
    void SetRunning(bool value)
    {
        if (isRunning != value)
        {
            isRunning = value;
            animator.SetBool("isRunning", isRunning);
        }
    }
    System.Collections.IEnumerator PerformAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        animator.SetBool("isRunning", false);
        animator.SetTrigger("Attack");

        // Optional: Wait for animation event instead
        yield return new WaitForSeconds(0.5f); // adjust this to match animation timing

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= attackRange)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }

        yield return new WaitForSeconds(attackCooldown - 0.5f);
        isAttacking = false;
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bullet"))
        {
            Bullet bullet = other.GetComponent<Bullet>();
            if (bullet != null)
            {
                TakeDamage(bullet.damage);
                Destroy(other.gameObject); // destroy bullet on impact
            }
        }
    }
    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log($"{gameObject.name} took {amount} damage. Remaining: {currentHealth}");

        // 🩸 Spawn blood splatter
        if (bloodSplatterPrefab)
        {
            Instantiate(bloodSplatterPrefab, transform.position + Vector3.up * 1f, Quaternion.identity);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;

        agent.isStopped = true;

        if (animator) animator.enabled = false;

        EnableRagdoll(true);

        if (deathEffectPrefab)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        // Try dropping a health pack
        TryDropping();

        Destroy(gameObject, 30f); // Destroy after 30 seconds
    }

    void EnableRagdoll(bool state)
    {
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>();
        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = !state;
        }

        foreach (Collider col in colliders)
        {
            if (col.gameObject != gameObject) // avoid disabling main capsule if needed
                col.enabled = state;
        }

        if (GetComponent<Collider>() != null)
            GetComponent<Collider>().enabled = !state;

        if (GetComponent<Rigidbody>() != null)
            GetComponent<Rigidbody>().isKinematic = state;
    }

    void TryDropping()
    {
        //This pickup heals the player
        if (healthPackPrefab != null && Random.value <= healthPackDropChance)
        {
            Instantiate(healthPackPrefab, transform.position, Quaternion.identity);
        }
        //This pickup increases the maximum health of the player
        if (maxHealthIncreasePrefab != null && Random.value <= maxHealthIncreaseDropChance)
        {
            Instantiate(maxHealthIncreasePrefab, transform.position, Quaternion.identity);
        }
    }
}
