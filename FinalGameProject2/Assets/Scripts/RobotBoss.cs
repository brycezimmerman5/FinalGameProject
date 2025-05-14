using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.UI;

public class RobotBoss : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 1000f;
    private float currentHealth;
    private bool isDead = false;
    public float animSpeed = 1f;
    public int numRangedAttacks;
    public int numAttacks = 2;

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

    [Header("Movement Behavior")]
    public float stopDistance = 5f;         // How far to stay from player
    public float circleStrafeSpeed = 2f;    // Circle speed when within range
    public float repositionFrequency = 4f;  // How often to reposition
    private float repositionTimer;

    [Header("Phases")]
    public float phase2Threshold = 0.6f;
    public float phase3Threshold = 0.25f;
    private int currentPhase = 1;
    private bool isRunning = false;

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

    [Header("Boulder Attack")]
    public GameObject boulderPrefab;
    public Transform boulderSpawnPoint;
    public int bouldersToSpawn = 5;
    public float boulderForce = 500f;
    public GameObject groundImpactEffect;

    [Header("Summon Ability")]
    public GameObject enemyPrefab;
    public float summonRadius = 10f;
    public int maxSummonCount = 5;

    [Header("Health UI")]
    public Image healthBarFill;
    public Canvas healthCanvas;

    [Header("Foot Beam Attack (Phase 2 Loop)")]
    public GameObject footBeamPrefab;
    public Transform beamSpawnPoint;
    public float footBeamForce = 800f;
    public float beamInterval = 5f;

    private Coroutine footBeamCoroutine;



    void Start()
    {
        animator.speed = animSpeed;
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
                TryOverheat();
                HandleMovement();
                TryMeleeAttack(distance);
                TryRangedAttack(distance);
                
               
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
        if (!hasOverheated && !isLasering && !isAttacking)
        {
            animator.SetTrigger("Overheat");
            isAttacking = true;
            hasOverheated = true;
            AddStats();
        }
    }
    public void SummonMinions()
    {
        SpawnEnemiesInRadius(maxSummonCount, summonRadius);
    }

    public void SpawnEnemiesInRadius(int count, float radius)
    {
        if (enemyPrefab == null) return;

        int spawned = 0;
        int attempts = 0;

        while (spawned < count && attempts < count * 5)
        {
            attempts++;

            // Random direction on XZ plane
            Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(radius * 0.5f, radius);
            Vector3 randomPos = transform.position + new Vector3(circle.x, 0, circle.y);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPos, out hit, 2f, NavMesh.AllAreas))
            {
                Instantiate(enemyPrefab, hit.position, Quaternion.identity);
                spawned++;
            }
        }

        Debug.Log($"Boss summoned {spawned} enemies in radius {radius}.");
        isAttacking = false;
        isLasering = false;

    }

    void AddStats()
    {
        animSpeed += 0.5f;
        animator.speed = animSpeed;
        attackCooldown -= 0.5f;
        rangedAttackCooldown -= 2f;
        timeBetweenLasers -= 0.1f;
    }
    void HandleMovement()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // Always rotate to face the player
        Vector3 lookDir = (player.position - transform.position).normalized;
        lookDir.y = 0;
        if (lookDir != Vector3.zero)
        {
            Quaternion rot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 5f);
        }

        // Stop if attacking or lasering
        if (isAttacking || isLasering)
        {
            agent.isStopped = true;
            return;
        }

        // If far from player, path toward them
        if (distance > stopDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            SetRunning(agent.velocity.magnitude > 0.1f);
            return;
        }

        // Within stop distance: do circle-strafe movement
        repositionTimer -= Time.deltaTime;
        if (repositionTimer <= 0)
        {
            repositionTimer = repositionFrequency;

            // Pick a new lateral direction (left or right) with slight forward bias
            Vector3 sideDir = Vector3.Cross(Vector3.up, player.position - transform.position).normalized;
            if (Random.value > 0.5f) sideDir *= -1f;

            Vector3 newTarget = transform.position + sideDir * 3f + lookDir * 2f;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(newTarget, out hit, 3f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                agent.isStopped = false;
            }
        }

        SetRunning(agent.velocity.magnitude > 0.1f);
    }


    void TryRangedAttack(float distance)
    {
        if (!isAttacking && distance <= rangedAttackRange && distance > attackRange && Time.time - lastRangedAttackTime >= rangedAttackCooldown)
        {
            SetRunning(false);
            //agent.speed = Mathf.Max(agent.speed - 4f, 0f);
            animator.SetTrigger("RangedAttack"+Random.Range(1,numRangedAttacks+1));
            isAttacking = true;
            lastRangedAttackTime = Time.time;
        }
    }

    void TryMeleeAttack(float distance)
    {
        if (!isAttacking && distance <= attackRange && Time.time - lastMeleeAttackTime >= attackCooldown)
        {
            SetRunning(false);
            animator.SetTrigger("Attack" + Random.Range(1, numAttacks + 1));
            isAttacking = true;
            lastMeleeAttackTime = Time.time;
        }
    }
    public void StartLaserAttack()
    {
        if (laserCoroutine == null)
        {
            isLasering = true;
            isAttacking = true;
            laserCoroutine = StartCoroutine(LaserAttackLoop());
            
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
        //agent.speed += 4f;
        Debug.Log("Laser attack stopped.");
    }

    private IEnumerator LaserAttackLoop()
    {
        while (isLasering && !isDead && player != null)
        {
            FireLaser();
            yield return new WaitForSeconds(timeBetweenLasers);
        }

        // Ensure clean exit
        laserCoroutine = null;
        isLasering = false;
        isAttacking = false;
        Debug.Log("Laser loop exited safely.");
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
    private void ShootFootBeams()
    {
        if (footBeamPrefab == null || beamSpawnPoint == null) return;

        Vector3[] directions = new Vector3[]
        {
        transform.forward,
        -transform.forward,
        transform.right,
        -transform.right
        };

        foreach (var dir in directions)
        {
            GameObject beam = Instantiate(footBeamPrefab, beamSpawnPoint.position, Quaternion.LookRotation(dir));
            Rigidbody rb = beam.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = dir.normalized * footBeamForce;
            }

            Destroy(beam, 6f);
        }

        Debug.Log("Boss fired 4 foot beams.");
    }

    private IEnumerator FootBeamLoop()
    {
        while (currentPhase == 2 && !isDead)
        {
            ShootFootBeams();
            yield return new WaitForSeconds(beamInterval);
        }

        footBeamCoroutine = null;
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
            if (footBeamCoroutine == null)
            {
                footBeamCoroutine = StartCoroutine(FootBeamLoop());
            }
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
        //LaunchBoulders(); // ← call it here
        StartCoroutine(EndAttackCooldown());
    }
    private void LaunchBoulders()
    {
        if (boulderPrefab == null || boulderSpawnPoint == null) return;
        if (groundImpactEffect != null)
        {
            Instantiate(groundImpactEffect, boulderSpawnPoint.position, Quaternion.identity);
        }
        float angleStep = 360f / bouldersToSpawn;

        for (int i = 0; i < bouldersToSpawn; i++)
        {
            float angle = i * angleStep;
            Vector3 horizontalDir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            Vector3 launchDir = (horizontalDir + Vector3.up * 0.75f).normalized; // upward arc

            GameObject boulder = Instantiate(boulderPrefab, boulderSpawnPoint.position, Quaternion.identity);
            Rigidbody rb = boulder.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.AddForce(launchDir * boulderForce);
            }

            Destroy(boulder, 10f);
        }

        Debug.Log($"{bouldersToSpawn} boulders launched into the air.");
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
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = currentHealth / maxHealth;
        }

        Debug.Log($"RobotBoss took {amount} damage. Remaining: {currentHealth}");

        if (bloodSplatterPrefab)
        {
            GameObject go = Instantiate(bloodSplatterPrefab, transform.position + Vector3.up * 1f, Quaternion.identity);
            Destroy(go, 2);
        }

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
        if (footBeamCoroutine != null)
        {
            StopCoroutine(footBeamCoroutine);
            footBeamCoroutine = null;
        }
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
