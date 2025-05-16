/*
 *  Author: ariel oliveira [o.arielg@gmail.com]
 */

using UnityEngine;

public class UIhealth : MonoBehaviour
{
    public delegate void OnHealthChangedDelegate();
    public OnHealthChangedDelegate onHealthChangedCallback;

    #region Sigleton
    private static UIhealth instance;
    public static UIhealth Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<UIhealth>();
            return instance;
        }
    }
    #endregion

    [SerializeField]
    private float health;
    [SerializeField]
    private float maxHealth;
    [SerializeField]
    private float maxTotalHealth;

    public float Health { get { return health; } }
    public float MaxHealth { get { return maxHealth; } }
    public float MaxTotalHealth { get { return maxTotalHealth; } }

    public void Heal(float health)
    {
        this.health += health;
        ClampHealth();
    }

    public void TakeDamage(float dmg)
    {
        health -= dmg;
        ClampHealth();
    }

    //INCREASES MAX HEALTH
    //INCREASES MAX HEALTH
    public void AddHealth()
    {
        if (maxHealth < maxTotalHealth)
        {
            // Calculate current health percentage
            float healthPercentage = health / maxHealth;

            // Increase max health
            maxHealth += 1;

            // Set health to maintain the same percentage
            health = maxHealth * healthPercentage;

            if (onHealthChangedCallback != null)
                onHealthChangedCallback.Invoke();
        }
    }

    void ClampHealth()
    {
        health = Mathf.Clamp(health, 0, maxHealth);

        if (onHealthChangedCallback != null)
            onHealthChangedCallback.Invoke();
    }
}
