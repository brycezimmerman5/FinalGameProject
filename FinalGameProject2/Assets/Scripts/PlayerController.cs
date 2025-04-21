using UnityEngine;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
    private NavMeshAgent agent;
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

    [Header("Visual FX")]
    public GameObject aoeEffectPrefab;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        HandleMovement();
        HandleAutoAttackInput();
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
        if (Input.GetKeyDown(KeyCode.Space) && Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            isAttacking = true;

            // 🔁 Play attack animation here
            // GetComponent<Animator>().SetTrigger("Attack");

            Debug.Log("Attack triggered — waiting for animation to hit...");
        }
    }

    // 🌀 This method gets called from an Animation Event
    public void PerformAOEAttack()
    {
        if (!isAttacking) return;

        Collider[] hitEnemies = Physics.OverlapSphere(transform.position, attackRadius, enemyLayer);

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
        isAttacking = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}
