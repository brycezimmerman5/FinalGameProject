using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    protected float currentHealth;
    protected bool isDead = false;

    [Header("Attack Settings")]
    public float attackRange = 2f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.5f;
    protected float lastAttackTime;
    protected bool isAttacking = false;

    [Header("References")]
    public Animator animator;
    public GameObject deathEffectPrefab;
    protected Transform player;
    protected NavMeshAgent agent;

    [Header("Drop Settings")]
    public GameObject healthPackPrefab;
    public GameObject maxHealthIncreasePrefab;
    [Range(0f, 1f)] public float healthPackDropChance = 0.2f;
    [Range(0f, 1f)] public float maxHealthIncreaseDropChance = 0.05f;

    public GameObject bloodSplatterPrefab;

    protected bool isRunning = false;

    protected virtual void Start()
    {
        currentHealth = maxHealth;

        if (!animator) animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false; // Disable agent's rotation control

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        EnableRagdoll(false);
    }

    protected virtual void Update()
    {
        if (isDead || player == null) return;

        // Always rotate to face player
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0f;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange)
        {
            agent.isStopped = true;

            if (!isAttacking && Time.time - lastAttackTime >= attackCooldown)
            {
                StartCoroutine(PerformAttack());
            }

            SetRunning(false);
        }
        else
        {
            if (!isAttacking)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
                SetRunning(true);
            }
        }
    }

    protected void SetRunning(bool value)
    {
        if (isRunning != value)
        {
            isRunning = value;
            animator.SetBool("isRunning", isRunning);
        }
    }

    protected virtual System.Collections.IEnumerator PerformAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        animator.SetBool("isRunning", false);
        animator.SetTrigger("Attack");

        yield return new WaitForSeconds(0.5f); // Adjust to match animation

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

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bullet"))
        {
            Bullet bullet = other.GetComponent<Bullet>();
            if (bullet != null)
            {
                TakeDamage(bullet.damage);
                Destroy(other.gameObject);
            }
        }
    }

    public virtual void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log($"{gameObject.name} took {amount} damage. Remaining: {currentHealth}");

        if (bloodSplatterPrefab)
        {
            Instantiate(bloodSplatterPrefab, transform.position + Vector3.up * 1f, Quaternion.identity);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        isDead = true;

        agent.isStopped = true;

        if (animator) animator.enabled = false;

        EnableRagdoll(true);

        if (deathEffectPrefab)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        TryDropping();

        Destroy(gameObject, 30f);
    }

    protected void EnableRagdoll(bool state)
    {
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>();
        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = !state;
        }

        foreach (Collider col in colliders)
        {
            if (col.gameObject != gameObject)
                col.enabled = state;
        }

        if (TryGetComponent(out Collider mainCol)) mainCol.enabled = !state;
        if (TryGetComponent(out Rigidbody mainRb)) mainRb.isKinematic = state;
    }

    protected virtual void TryDropping()
    {
        if (healthPackPrefab != null && Random.value <= healthPackDropChance)
        {
            Instantiate(healthPackPrefab, transform.position, Quaternion.identity);
        }

        if (maxHealthIncreasePrefab != null && Random.value <= maxHealthIncreaseDropChance)
        {
            Instantiate(maxHealthIncreasePrefab, transform.position, Quaternion.identity);
        }
    }
}
