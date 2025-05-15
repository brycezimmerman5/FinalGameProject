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
    private bool hasStartedFight = false;

    [Header("Intro Animation")]
    public float introDuration = 3f;
    public float introRotationSpeed = 2f;
    public float introHeight = 2f;
    private Vector3 startPosition;
    private Quaternion startRotation;

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
    public float stopDistance = 5f;
    public float circleStrafeSpeed = 2f;
    public float repositionFrequency = 4f;
    private float repositionTimer;
    public float dashSpeed = 15f;
    public float dashDuration = 0.5f;
    public float dashCooldown = 5f;
    private float lastDashTime;
    private bool isDashing = false;
    public float dashDamage = 30f;
    public float dashKnockbackForce = 500f;
    public float teleportCooldown = 10f;
    private float lastTeleportTime;
    public float teleportRange = 20f;

    [Header("Phases")]
    public float phase2Threshold = 0.6f;
    public float phase3Threshold = 0.25f;
    private int currentPhase = 1;
    private bool isRunning = false;
    private float phaseChangeTimer = 0f;
    public float phaseChangeInterval = 15f;

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
    public Renderer eyeRenderer;
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

    [Header("Foot Beam Attack")]
    public GameObject footBeamPrefab;
    public Transform beamSpawnPoint;
    public float footBeamForce = 800f;
    public float beamInterval = 5f;

    [Header("Defensive Abilities")]
    public GameObject shieldPrefab;
    public float shieldDuration = 5f;
    public float shieldCooldown = 15f;
    private float lastShieldTime;
    private bool isShielded = false;
    public GameObject shockwavePrefab;
    public float shockwaveCooldown = 8f;
    private float lastShockwaveTime;
    public float shockwaveRadius = 10f;
    public float shockwaveForce = 1000f;

    private Coroutine footBeamCoroutine;
    private Coroutine currentMovementCoroutine;

    void Start()
    {
        InitializeBoss();
    }

    void InitializeBoss()
    {
        animator.speed = animSpeed;
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        if (!animator) animator = GetComponent<Animator>();
        agent.stoppingDistance = attackRange - 0.5f;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj) player = playerObj.transform;

        EnableRagdoll(false);
        
        // Store initial position and rotation
        startPosition = transform.position;
        startRotation = transform.rotation;
        
        // Start intro sequence
        StartCoroutine(PlayIntroSequence());
    }

    IEnumerator PlayIntroSequence()
    {
        // Disable agent during intro
        agent.enabled = false;
        
        // Play intro animation
        animator.SetTrigger("Intro");
        
        // Wait for animation to complete
        yield return new WaitForSeconds(introDuration);
        
        // Enable agent and start fight
        agent.enabled = true;
        hasStartedFight = true;
    }

    void Update()
    {
        if (isDead || player == null || !hasStartedFight) return;

        // Always rotate to face player
        RotateTowardsPlayer();

        CheckPhase();
        float distance = Vector3.Distance(transform.position, player.position);

        // Phase change timer
        phaseChangeTimer += Time.deltaTime;
        if (phaseChangeTimer >= phaseChangeInterval)
        {
            phaseChangeTimer = 0f;
            RandomizeBehavior();
        }

        // Handle phase-specific behavior
        HandlePhaseBehavior(distance);
    }

    void RotateTowardsPlayer()
    {
        if (player != null)
        {
            Vector3 lookDir = (player.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                Quaternion rot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 5f);
            }
        }
    }

    void HandlePhaseBehavior(float distance)
    {
        switch (currentPhase)
        {
            case 1:
                HandlePhase1Behavior(distance);
                break;
            case 2:
                HandlePhase2Behavior(distance);
                break;
            case 3:
                HandlePhase3Behavior(distance);
                break;
        }
    }

    void HandlePhase1Behavior(float distance)
    {
        if (!isAttacking && !isLasering && !isDashing)
        {
            if (Random.value < 0.4f && Time.time - lastDashTime >= dashCooldown)
            {
                StartCoroutine(PerformDash());
            }
            else
            {
                HandleMovement();
            }
        }
        TryMeleeAttack(distance);
        TryRangedAttack(distance);
    }

    void HandlePhase2Behavior(float distance)
    {
        if (!isAttacking && !isLasering && !isDashing)
        {
            if (Random.value < 0.5f && Time.time - lastDashTime >= dashCooldown)
            {
                StartCoroutine(PerformDash());
            }
            else if (Random.value < 0.3f && Time.time - lastShieldTime >= shieldCooldown)
            {
                ActivateShield();
            }
            else if (Random.value < 0.3f && Time.time - lastShockwaveTime >= shockwaveCooldown)
            {
                PerformShockwave();
            }
            else
            {
                HandleMovement();
            }
        }
        TryOverheat();
        TryMeleeAttack(distance);
        TryRangedAttack(distance);
    }

    void HandlePhase3Behavior(float distance)
    {
        if (!isAttacking && !isLasering && !isDashing)
        {
            if (Random.value < 0.6f && Time.time - lastDashTime >= dashCooldown)
            {
                StartCoroutine(PerformDash());
            }
            else if (Random.value < 0.3f && Time.time - lastShockwaveTime >= shockwaveCooldown)
            {
                PerformShockwave();
            }
            else
            {
                HandleMovement();
            }
        }
        TryRangedAttack(distance);
        TryMeleeAttack(distance);
    }

    void RandomizeBehavior()
    {
        // Randomly change attack patterns and movement with small variations
        float variation = 0.2f; // 20% variation
        
        attackCooldown = Mathf.Clamp(attackCooldown * Random.Range(1f - variation, 1f + variation), 1f, 5f);
        rangedAttackCooldown = Mathf.Clamp(rangedAttackCooldown * Random.Range(1f - variation, 1f + variation), 2f, 8f);
        timeBetweenLasers = Mathf.Clamp(timeBetweenLasers * Random.Range(1f - variation, 1f + variation), 0.1f, 0.5f);
        stopDistance = Mathf.Clamp(stopDistance * Random.Range(1f - variation, 1f + variation), 3f, 8f);
    }

    void TeleportToRandomPosition()
    {
        Vector3 randomDirection = Random.insideUnitSphere * teleportRange;
        randomDirection.y = 0;
        Vector3 targetPosition = player.position + randomDirection;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, teleportRange, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            lastTeleportTime = Time.time;
            Debug.Log("Boss teleported to new position");
        }
    }

    IEnumerator PerformDash()
    {
        isDashing = true;
        lastDashTime = Time.time;
        Vector3 dashDirection = (player.position - transform.position).normalized;
        float elapsed = 0f;

        // Trigger dash animation
        animator.SetTrigger("Dash");

        while (elapsed < dashDuration)
        {
            agent.Move(dashDirection * dashSpeed * Time.deltaTime);
            
            // Check for player collision during dash
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, 2f);
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("Player"))
                {
                    // Apply damage and knockback
                    PlayerHealth playerHealth = hitCollider.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(dashDamage);
                    }

                    Rigidbody playerRb = hitCollider.GetComponent<Rigidbody>();
                    if (playerRb != null)
                    {
                        playerRb.AddForce(dashDirection * dashKnockbackForce);
                    }
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        isDashing = false;
    }

    void ActivateShield()
    {
        if (shieldPrefab != null)
        {
            GameObject shield = Instantiate(shieldPrefab, transform.position, Quaternion.identity);
            shield.transform.parent = transform;
            isShielded = true;
            lastShieldTime = Time.time;
            StartCoroutine(DeactivateShield(shield));
        }
    }

    IEnumerator DeactivateShield(GameObject shield)
    {
        yield return new WaitForSeconds(shieldDuration);
        isShielded = false;
        Destroy(shield);
    }

    void PerformShockwave()
    {
        if (shockwavePrefab != null)
        {
            GameObject shockwave = Instantiate(shockwavePrefab, transform.position, Quaternion.identity);
            lastShockwaveTime = Time.time;

            // Apply force to nearby objects
            Collider[] colliders = Physics.OverlapSphere(transform.position, shockwaveRadius);
            foreach (Collider col in colliders)
            {
                if (col.CompareTag("Player"))
                {
                    Rigidbody rb = col.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        Vector3 direction = (col.transform.position - transform.position).normalized;
                        rb.AddForce(direction * shockwaveForce);
                    }
                }
            }

            Destroy(shockwave, 2f);
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

            Vector3 newTarget = transform.position + sideDir * 3f + (player.position - transform.position).normalized * 2f;
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
        if (isAttacking || isLasering || isDashing) return; // Don't start new attacks if already attacking

        if (distance <= rangedAttackRange && distance > attackRange && Time.time - lastRangedAttackTime >= rangedAttackCooldown)
        {
            SetRunning(false);
            animator.SetTrigger("RangedAttack"+Random.Range(1,numRangedAttacks+1));
            isAttacking = true;
            lastRangedAttackTime = Time.time;
        }
    }

    void TryMeleeAttack(float distance)
    {
        if (isAttacking || isLasering || isDashing) return; // Don't start new attacks if already attacking

        if (distance <= attackRange && Time.time - lastMeleeAttackTime >= attackCooldown)
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
        while ((currentPhase == 2 || currentPhase ==3) && !isDead)
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
        isLasering = false; // Also reset laser state
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
