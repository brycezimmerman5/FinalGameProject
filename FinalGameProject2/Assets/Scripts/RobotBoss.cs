using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class RobotBoss : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 1000f;
    private float currentHealth;
    private bool isDead = false;

    [Header("Attack Settings")]
    public float attackRange = 5f;
    public float attackDamage = 25f;
    public float attackCooldown = 2.5f;
    private float lastAttackTime;
    private bool isAttacking = false;

    [Header("References")]
    public Animator animator;
    public GameObject deathEffectPrefab;
    public GameObject bloodSplatterPrefab;
    public GameObject blackHolePrefab;
    public Transform attackOrigin;

    private Transform player;
    private NavMeshAgent agent;

    [Header("Phase Thresholds")]
    public float phase2Threshold = 0.6f;
    public float phase3Threshold = 0.25f;
    private int currentPhase = 1;

    [Header("Movement")]
    public float circleDistance = 8f;
    public float circleSpeed = 3f;
    public float dashSpeed = 15f;
    public float dashCooldown = 8f;
    private float lastDashTime;
    private bool isDashing = false;
    private Vector3 dashTarget;

    private bool isRunning = false;

    void Start()
    {
        currentHealth = maxHealth;
        if (!animator) animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = true;
        agent.stoppingDistance = attackRange - 0.5f;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj) player = playerObj.transform;

        EnableRagdoll(false);
    }

    void Update()
    {
        if (isDead || player == null) return;

        CheckPhase();

        if (isDashing)
        {
            agent.isStopped = true;
            transform.position = Vector3.MoveTowards(transform.position, dashTarget, dashSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, dashTarget) < 0.5f)
            {
                isDashing = false;
                agent.isStopped = false;
            }
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        // Always move toward player unless attacking or dashing
        if (!isAttacking && !isDashing)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            SetRunning(agent.velocity.magnitude > 0.1f);
        }

        // Attack if in range
        if (!isAttacking && distance <= attackRange && Time.time - lastAttackTime >= attackCooldown)
        {
            agent.isStopped = true;
            SetRunning(false);
            StartCoroutine(PerformAttack());
            lastAttackTime = Time.time;
        }

        if (currentPhase >= 2 && Time.time - lastDashTime > dashCooldown)
        {
            DashToOffset();
            lastDashTime = Time.time;
        }
    }

    void AdvancedMovement()
    {
        Vector3 offset = Quaternion.Euler(0, Time.time * circleSpeed, 0) * Vector3.forward * circleDistance;
        Vector3 target = player.position + offset;
        agent.SetDestination(target);
    }

    void DashToOffset()
    {
        Vector3 dir = (transform.position - player.position).normalized;
        Vector3 sideOffset = Vector3.Cross(dir, Vector3.up) * Random.Range(-6f, 6f);
        dashTarget = player.position + dir * 3f + sideOffset;
        isDashing = true;
        agent.ResetPath();
        Debug.Log("Dash initiated.");
    }

    void SetRunning(bool value)
    {
        if (isRunning != value)
        {
            isRunning = value;
            animator.SetBool("isRunning", isRunning);
        }
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;
        animator.SetBool("isRunning", false);
        animator.SetTrigger("Attack");

        yield return new WaitForSeconds(0.6f);

        if (Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            var playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null) playerHealth.TakeDamage(attackDamage);
        }

        yield return new WaitForSeconds(attackCooldown - 0.6f);
        isAttacking = false;
    }

    void CheckPhase()
    {
        float hpPercent = currentHealth / maxHealth;

        if (hpPercent <= phase3Threshold && currentPhase < 3)
        {
            currentPhase = 3;
            Debug.Log("Phase 3 triggered!");
            if (blackHolePrefab) Instantiate(blackHolePrefab, attackOrigin.position, Quaternion.identity);
        }
        else if (hpPercent <= phase2Threshold && currentPhase < 2)
        {
            currentPhase = 2;
            Debug.Log("Phase 2 triggered!");
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log($"RobotBoss took {amount} damage. Remaining: {currentHealth}");

        if (bloodSplatterPrefab)
            Instantiate(bloodSplatterPrefab, transform.position + Vector3.up * 1f, Quaternion.identity);

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        isDead = true;
        agent.isStopped = true;
        animator.enabled = false;
        EnableRagdoll(true);

        if (deathEffectPrefab)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject, 30f);
    }

    void EnableRagdoll(bool state)
    {
        foreach (var rb in GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = !state;

        foreach (var col in GetComponentsInChildren<Collider>())
            if (col.gameObject != gameObject) col.enabled = state;

        if (TryGetComponent(out Collider mainCol)) mainCol.enabled = !state;
        if (TryGetComponent(out Rigidbody mainRb)) mainRb.isKinematic = state;
    }

    void OnTriggerEnter(Collider other)
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
}
