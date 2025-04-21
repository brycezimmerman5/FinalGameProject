using UnityEngine;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;

    public LayerMask groundLayer;
    public LayerMask enemyLayer;

    [Header("Click Indicator")]
    public GameObject clickIndicatorPrefab;

    [Header("Attack Settings")]
    public float attackRadius = 3f;
    public float attackDamage = 20f;
    public float attackCooldown = 1.0f;
    private float lastAttackTime;
    private bool isAttacking = false;
    public Transform attackPoint;

    [Header("Visual FX")]
    public GameObject aoeEffectPrefab;

    [Header("Dash Settings")]
    public float dashDistance = 5f;
    public float dashDuration = 0.25f;
    public float dashCooldown = 2f;

    private float lastDashTime;
    private bool isDashing = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (!isAttacking)
        {
            HandleMovement();
        }

        HandleAutoAttackInput();
        UpdateAnimationStates();
        HandleDashInput();
    }

    void HandleMovement()
    {
        if (Input.GetMouseButtonDown(1)) // Right-click
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
            {
                agent.SetDestination(hit.point);

                if (clickIndicatorPrefab != null)
                {
                    GameObject indicator = Instantiate(clickIndicatorPrefab, hit.point + Vector3.up * 0.05f, Quaternion.identity);
                    Destroy(indicator, 0.4f);
                }
            }
        }
    }

    void HandleAutoAttackInput()
    {
        if (Input.GetKeyDown(KeyCode.Q) && Time.time - lastAttackTime >= attackCooldown && !isAttacking)
        {
            lastAttackTime = Time.time;
            isAttacking = true;

            // Stop movement during attack
            agent.isStopped = true;

            // 🔁 Play attack animation
            animator.SetTrigger("Attack");

            Debug.Log("Attack triggered — waiting for animation to hit...");
        }
    }
    void HandleDashInput()
    {
        if (Input.GetKeyDown(KeyCode.W) && !isAttacking && !isDashing && Time.time - lastDashTime >= dashCooldown)
        {
            StartCoroutine(PerformDash());
        }
    }
    System.Collections.IEnumerator PerformDash()
    {
        isDashing = true;
        lastDashTime = Time.time;

        agent.isStopped = true; // Prevent movement while dashing

        Vector3 start = transform.position;
        Vector3 direction = transform.forward;
        Vector3 end = start + direction.normalized * dashDistance;

        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            float t = elapsed / dashDuration;
            Vector3 dashPos = Vector3.Lerp(start, end, t);
            agent.Warp(dashPos); // Warp overrides navmesh

            elapsed += Time.deltaTime;
            yield return null;
        }

        agent.Warp(end); // Snap to final position
        agent.isStopped = false;
        isDashing = false;

        Debug.Log("Dash complete");
    }
    void UpdateAnimationStates()
    {
        bool isMoving = agent.velocity.magnitude > 0.1f && !isAttacking;
        animator.SetBool("isRunning", isMoving);
    }

    // 🌀 Called by animation event
    public void Attack()
    {
        if (!isAttacking) return;

        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRadius, enemyLayer);

        /*foreach (Collider enemy in hitEnemies)
        {
            Enemy enemyScript = enemy.GetComponent<Enemy>();
            if (enemyScript != null)
            {
                enemyScript.TakeDamage(attackDamage);
            }
        }*/

        if (aoeEffectPrefab != null)
        {
            Instantiate(aoeEffectPrefab, transform.position, Quaternion.identity);
        }

        Debug.Log($"AOE Damage applied to {hitEnemies.Length} enemies.");

        // ✅ Re-enable movement
        agent.isStopped = false;
        isAttacking = false;
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
    }
}
