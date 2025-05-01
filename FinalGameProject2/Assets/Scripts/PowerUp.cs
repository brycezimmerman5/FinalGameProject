using UnityEngine;

[CreateAssetMenu(fileName = "NewPowerUp", menuName = "PowerUps/StatModifier")]
public class PowerUp : ScriptableObject
{
    public PowerUpType type;
    public float value; // Modifier value (e.g., +5 ammo, -0.5s reload, etc.)
}
