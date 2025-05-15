using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float damage = 20f;
    public float lifetime = 5f;

    void Start()
    {
        Destroy(gameObject, lifetime); // destroy after a few seconds if it doesn't hit anything
    }

    void OnTriggerEnter(Collider other)
    {
        // Damage is handled in Enemy's OnTriggerEnter
        // Optional: add particle FX or impact VFX here
        
    }
}
