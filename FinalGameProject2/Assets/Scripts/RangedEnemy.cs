using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class RangedEnemy : Enemy
{
    [Header("Ranged Attack Settings")]
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint;
    public float fireCooldown = 0.3f;
    public float timeBetweenFire = 1f;
    public float bulletForce = 20f;

    private bool isShooting = false;
    private Coroutine shootCoroutine;

    [Header("Strafing Settings")]
    public float strafeDistance = 2f;
    public float strafeIntervalMin = 1.5f;
    public float strafeIntervalMax = 3.5f;

    private float strafeTimer;

    protected override void Start()
    {
        base.Start();
        ResetStrafeTimer();
    }

    protected override void Update()
    {
        if (isDead || player == null) return;

        // Face the player
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
            agent.isStopped = false;
            SetRunning(true);

            // Start firing if not already
            if (!isShooting)
            {
                shootCoroutine = StartCoroutine(FireRepeatedly());
                isShooting = true;
            }

            // Strafing logic
            strafeTimer -= Time.deltaTime;
            if (strafeTimer <= 0f)
            {
                Strafe();
                ResetStrafeTimer();
            }
        }
        else
        {
            // Stop firing if out of range
            if (isShooting)
            {
                StopCoroutine(shootCoroutine);
                isShooting = false;
            }

            if (!isAttacking)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
                SetRunning(true);
            }
        }
    }

    private void ResetStrafeTimer()
    {
        strafeTimer = Random.Range(strafeIntervalMin, strafeIntervalMax);
    }

    private void Strafe()
    {
        if (player == null) return;

        Vector3 toPlayer = (player.position - transform.position).normalized;
        Vector3 strafeDirection = Vector3.Cross(Vector3.up, toPlayer).normalized;

        if (Random.value > 0.5f)
            strafeDirection *= -1f;

        Vector3 strafeTarget = transform.position + strafeDirection * strafeDistance;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(strafeTarget, out hit, strafeDistance + 1f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            SetRunning(true);
        }
    }

    private IEnumerator FireRepeatedly()
    {
        while (true)
        {
            animator.SetTrigger("Attack");

            yield return new WaitForSeconds(timeBetweenFire);

            FireProjectile();

            yield return new WaitForSeconds(fireCooldown);
        }
    }

    private void FireProjectile()
    {
        if (projectilePrefab != null && projectileSpawnPoint != null)
        {
            GameObject projectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, projectileSpawnPoint.rotation);
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 dir = (player.position + Vector3.up * 1f - projectileSpawnPoint.position).normalized;
                rb.velocity = dir * bulletForce;
            }
        }
    }

    protected override void Die()
    {
        if (isShooting && shootCoroutine != null)
        {
            StopCoroutine(shootCoroutine);
        }

        base.Die();
    }
}
