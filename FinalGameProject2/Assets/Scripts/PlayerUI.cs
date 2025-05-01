using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    public PlayerController player;

    public TextMeshProUGUI statsText;

    void Update()
    {
        if (player == null || statsText == null) return;

        statsText.text = $"Ammo: {player.GetCurrentAmmo()} / {player.maxAmmo}\n" +
                         $"Fire Rate: {player.fireRate:F2}s\n" +
                         $"Accuracy: {player.accuracy * 100:F0}%\n" +
                         $"Reload Time: {player.reloadTime:F1}s\n" +
                         //$"Dash Distance: {player.dashDistance}m\n" +
                         //$"Dash Duration: {player.dashDuration:F2}s\n" +
                         //$"Dash Cooldown: {player.dashCooldown}s\n" +
                         $"Bullet Force: {player.bulletForce}N";
    }
}
