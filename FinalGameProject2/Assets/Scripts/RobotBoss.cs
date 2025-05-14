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
    private float lastMeleeAttackTime;
    private bool isAttacking = false;

    [Header("References")]
    public Animator animator;
    public GameObject deathEffectPrefab;
    public GameObject bloodSplatterPrefab;
    public GameObject blackHolePrefab;
    public Transform attackOrigin;

    private Transform player;
    private NavMeshAgent agent;

    [Header("Phases")]
    public float phase2Threshold = 0.6f;
    public float phase3Threshold = 0.25f;
    private int currentPhase = 1;

    private bool isRunning = false;
    public int numAttacks = 2;

    [Header("Laser Attack")]
    public GameObject laserPrefab;
    public Transform laserSpawnPoint;
    private Coroutine laserCoroutine;
    public float rangedAttackRange = 15f;
    private bool isLasering = false;
    public float rangedAttackCooldown = 4f;
    private float lastRangedAttackTime;
    public float timeBetweenLasers = 0.2f;
    public float laserForce = 1000f;
    [Header("Overheat")]
    public Renderer eyeRenderer; // assign glowing material
    public Color overheatColor = Color.red;
    public float glowIntensity = 5f;
    private bool hasOverheated = false;
    void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        if (!animator) animator = GetComponent<Animator>();
        agent.stoppingDistance = attackRange - 0.5f;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj) player = playerObj.transform;

        EnableRagdoll(false);
    }

    void Update()
    {
        if (isDead || player == null) return;

        CheckPhase();
        float distance = Vector3.Distance(transform.position, player.position);

        if (isLasering && player != null)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            dir.y = 0f;
            Quaternion rot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 5f);
        }

        switch (currentPhase)
        {
            case 1: // Phase 1 – basic melee
                HandleMovement();
                TryMeleeAttack(distance);
                TryRangedAttack(distance);
                break;

            case 2: // Phase 2 – ranged and melee
                HandleMovement();
                TryMeleeAttack(distance);
                TryRangedAttack(distance);
                TryOverheat();
               
                break;

            case 3: // Phase 3 – assume laser/black hole/etc.
                HandleMovement();
                TryRangedAttack(distance);
                TryMeleeAttack(distance);
                break;
        }
    }
    void TryOverheat()
    {
        if (!hasOverheated)
        {
            animator.SetTrigger("Overheat");
            //isAttacking = true;
            hasOverheated = true;
        }
    }
    void HandleMovement()
    {
        if (!isAttacking && !isLasering)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            SetRunning(agent.velocity.magnitude > 0.1f);
        }
        else if (!isLasering)
        {
            agent.SetDestination(player.position);
        }
    }

    void TryRangedAttack(float distance)
    {
        if (!isAttacking && distance <= rangedAttackRange && distance > attackRange && Time.time - lastRangedAttackTime >= rangedAttackCooldown)
        {
            SetRunning(false);
            agent.speed = Mathf.Max(agent.speed - 4f, 0f);
            animator.SetTrigger("Attack2");
            isAttacking = true;
            lastRangedAttackTime = Time.time;
        }
    }

    void TryMeleeAttack(float distance)
    {
        if (!isAttacking && distance <= attackRange && Time.time - lastMeleeAttackTime >= attackCooldown)
        {
            SetRunning(false);
            animator.SetTrigger("Attack1");
            isAttacking = true;
            lastMeleeAttackTime = Time.time;
        }
    }
    public void StartLaserAttack()
    {
        if (laserCoroutine == null)
        {
            laserCoroutine = StartCoroutine(LaserAttackLoop());
            isLasering = true;
            isAttacking = true;
            Debug.Log("Laser attack started.");
        }
    }

    public void StopLaserAttack()
    {
        if (laserCoroutine != null)
        {
            StopCoroutine(laserCoroutine);
            laserCoroutine = null;
        }
        isLasering = false;
        isAttacking = false;
        agent.speed += 4f;
        Debug.Log("Laser attack stopped.");
    }

    private IEnumerator LaserAttackLoop()
    {
        while (true)
        {
            FireLaser();
            yield return new WaitForSeconds(timeBetweenLasers);
        }
    }

    private void FireLaser()
    {
        if (laserPrefab && laserSpawnPoint)
        {
            GameObject laser = Instantiate(laserPrefab, laserSpawnPoint.position, laserSpawnPoint.rotation);
            Rigidbody rb = laser.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(laserSpawnPoint.forward * laserForce);
            }
            Destroy(laser, 5f);
        }
    }

    void CheckPhase()
    {
        float hpPercent = currentHealth / maxHealth;

        if (hpPercent <= phase3Threshold && currentPhase < 3)
        {
            currentPhase = 3;
            Debug.Log("Phase 3 reached. Wait for animation to trigger Black Hole.");
        }
        else if (hpPercent <= phase2Threshold && currentPhase < 2)
        {
            currentPhase = 2;
            Debug.Log("Phase 2 reached. No dash ability now.");
        }
    }
    

    public void TriggerOverheat()
    {
        if (eyeRenderer != null)
        {
            eyeRenderer.material.SetColor("_EmissionColor", overheatColor * glowIntensity);
            Debug.Log("Eyes glowing – overheat triggered.");
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

    public void DealDamage()
    {
        if (player == null || isDead) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= attackRange)
        {
            PlayerHealth hp = player.GetComponent<PlayerHealth>();
            if (hp != null) hp.TakeDamage(attackDamage);
        }

        StartCoroutine(EndAttackCooldown());
    }

    public void TriggerBlackHole()
    {
        if (blackHolePrefab && attackOrigin)
        {
            Instantiate(blackHolePrefab, attackOrigin.position, Quaternion.identity);
            Debug.Log("Black Hole ability triggered.");
        }
    }

    IEnumerator EndAttackCooldown()
    {
        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
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
            if (col.gameObject != gameObject)
                col.enabled = state;

        if (TryGetComponent(out Collider mainCol)) mainCol.enabled = !state;
        if (TryGetComponent(out Rigidbody mainRb)) mainRb.isKinematic = state;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bullet"))
        {
            Bullet b = other.GetComponent<Bullet>();
            if (b != null)
            {
                TakeDamage(b.damage);
                Destroy(other.gameObject);
            }
        }
    }
}
