using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI winText;
    public float winMessageDuration = 5f;

    [Header("Scene Settings")]
    public float delayBeforeNextScene = 3f;
    public string nextSceneName;

    private int totalEnemies;
    private int deadEnemies;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Hide win text at start
        if (winText != null)
        {
            winText.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        // Find all enemies in the scene
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        totalEnemies = enemies.Length;
        deadEnemies = 0;

        // Subscribe to each enemy's death event
        foreach (Enemy enemy in enemies)
        {
            enemy.OnEnemyDeath += HandleEnemyDeath;
        }

        Debug.Log($"Total enemies: {totalEnemies}");
    }

    void HandleEnemyDeath()
    {
        deadEnemies++;
        Debug.Log($"Enemy died. {deadEnemies}/{totalEnemies} enemies defeated");

        if (deadEnemies >= totalEnemies)
        {
            ShowWinMessage();
        }
    }

    void ShowWinMessage()
    {
        if (winText != null)
        {
            winText.gameObject.SetActive(true);
            winText.text = "You Win!";
            
            // If there's a next scene, load it after delay
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                Invoke(nameof(LoadNextScene), delayBeforeNextScene);
            }
        }
    }

    void LoadNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }

    void OnDestroy()
    {
        // Unsubscribe from all enemy death events
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        foreach (Enemy enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.OnEnemyDeath -= HandleEnemyDeath;
            }
        }
    }
} 