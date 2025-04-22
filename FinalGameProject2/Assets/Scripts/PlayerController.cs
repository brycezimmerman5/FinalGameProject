using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private CharacterController controller;
    private Animator animator;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float gravity = -9.81f;
    private Vector3 velocity;

    [Header("Mouse Rotation")]
    public LayerMask groundLayer;
    private Camera cam;

    [Header("Gun Settings")]
    public GameObject bulletPrefab;
    public Transform shootPoint;
    public float bulletForce = 700f;
    public float fireRate = 0.25f; // time between shots
    private float lastShotTime = 0f;

    [Header("Ammo Settings")]
    public int maxAmmo = 10;
    public float reloadTime = 2f;
    private int currentAmmo;
    private bool isReloading = false;

    [Header("Dash Settings")]
    public float dashDistance = 5f;
    public float dashDuration = 0.25f;
    public float dashCooldown = 2f;
    private float lastDashTime;
    private bool isDashing = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        cam = Camera.main;
        currentAmmo = maxAmmo;
    }

    void Update()
    {
        

        if (!isDashing)
        {
            HandleMovement();
        }
        RotateTowardMouse();
        if (isReloading) return;
        
        HandleDashInput();
        HandleShootInput();
        HandleReloadInput();
        UpdateAnimationStates();
    }

    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 move = new Vector3(h, 0, v).normalized;
        controller.Move(move * moveSpeed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void RotateTowardMouse()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            Vector3 lookDir = hit.point - transform.position;
            lookDir.y = 0f;

            if (lookDir.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
            }
        }
    }

    void HandleShootInput()
    {
        if (Input.GetMouseButton(0) && Time.time - lastShotTime >= fireRate && currentAmmo > 0)
        {
            Shoot();
        }
        else if (Input.GetMouseButtonDown(0) && currentAmmo <= 0)
        {
            StartCoroutine(Reload());
        }
    }

    void Shoot()
    {
        lastShotTime = Time.time;
        currentAmmo--;

        GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, shootPoint.rotation);
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        rb.AddForce(shootPoint.forward * bulletForce);

        // animator.SetTrigger("Shoot");
        Debug.Log("Shot fired. Ammo remaining: " + currentAmmo);
    }

    void HandleReloadInput()
    {
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < maxAmmo)
        {
            StartCoroutine(Reload());
        }
    }

    System.Collections.IEnumerator Reload()
    {
        isReloading = true;
        Debug.Log("Reloading...");
        // animator.SetTrigger("Reload"); // Optional
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = maxAmmo;
        isReloading = false;
        Debug.Log("Reload complete.");
    }

    void HandleDashInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isDashing && Time.time - lastDashTime >= dashCooldown)
        {
            StartCoroutine(PerformDash());
        }
    }

    System.Collections.IEnumerator PerformDash()
    {
        isDashing = true;
        lastDashTime = Time.time;

        Vector3 dashDir = transform.forward;
        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            controller.Move(dashDir * (dashDistance / dashDuration) * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        isDashing = false;
    }

    void UpdateAnimationStates()
    {
        bool isMoving = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).magnitude > 0.1f;
        animator.SetBool("isRunning", isMoving);
    }
}
