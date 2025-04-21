using UnityEngine;

public class TopDownCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 15f, -10f);
    public float followSpeed = 5f;
    public Vector3 fixedRotation = new Vector3(60f, 0f, 0f); // Top-down angled view

    void LateUpdate()
    {
        if (!target) return;

        // Smooth follow
        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        // Lock rotation to a fixed top-down angle
        transform.rotation = Quaternion.Euler(fixedRotation);
    }
}
