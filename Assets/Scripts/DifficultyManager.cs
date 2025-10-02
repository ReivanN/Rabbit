using UnityEngine;
using System.Collections;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    [Header("Difficulty Settings")]
    [SerializeField] private float initialBombInterval = 5f;
    [SerializeField] private float minBombInterval = 1f;
    [SerializeField] private float difficultyIncreaseInterval = 30f;
    [SerializeField] private float bombIntervalDecrease = 0.5f;
    [SerializeField] private float maxBombLifetime = 3f;
    [SerializeField] private float minBombLifetime = 1f;
    [SerializeField] private float lifetimeDecrease = 0.2f;

    [Header("Bomb Limit")]
    [SerializeField] private int maxBombsOnScreen = 15;
    [SerializeField] private int initialMaxBombs = 3;
    [SerializeField] private int bombLimitIncrease = 1;

    [Header("Group Spawn Settings")]
    [SerializeField] private int initialBombsPerSpawn = 1;
    [SerializeField] private int maxBombsPerSpawn = 5;
    [SerializeField] private int bombsPerSpawnIncrease = 1;
    [SerializeField] private float groupSpawnChance = 0.1f;
    [SerializeField] private float groupSpawnChanceIncrease = 0.1f;

    private float currentBombInterval;
    private float currentBombLifetime;
    private int currentMaxBombs;
    private int currentBombsPerSpawn;
    private float currentGroupSpawnChance;

    public float CurrentBombInterval => currentBombInterval;
    public float CurrentBombLifetime => currentBombLifetime;
    public int CurrentMaxBombs => currentMaxBombs;
    public int CurrentBombsPerSpawn => currentBombsPerSpawn;
    public float CurrentGroupSpawnChance => currentGroupSpawnChance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Инициализация начальных значений
        currentBombInterval = initialBombInterval;
        currentBombLifetime = maxBombLifetime;
        currentMaxBombs = initialMaxBombs;
        currentBombsPerSpawn = initialBombsPerSpawn;
        currentGroupSpawnChance = groupSpawnChance;
    }

    private void Start()
    {
        StartCoroutine(IncreaseDifficultyOverTime());
    }

    private IEnumerator IncreaseDifficultyOverTime()
    {
        while (true)
        {
            yield return new WaitForSeconds(difficultyIncreaseInterval);
            IncreaseDifficulty();
        }
    }

    private void IncreaseDifficulty()
    {
        // Уменьшаем интервал между спавном бомб
        currentBombInterval = Mathf.Max(minBombInterval, currentBombInterval - bombIntervalDecrease);
        
        // Уменьшаем время до взрыва бомб
        currentBombLifetime = Mathf.Max(minBombLifetime, currentBombLifetime - lifetimeDecrease);
        
        // Увеличиваем максимальное количество бомб на экране
        currentMaxBombs = Mathf.Min(maxBombsOnScreen, currentMaxBombs + bombLimitIncrease);
        
        // Увеличиваем количество бомб за спавн (реже чем другие параметры)
        if (Random.Range(0, 2) == 0) // 50% шанс увеличения
        {
            currentBombsPerSpawn = Mathf.Min(maxBombsPerSpawn, currentBombsPerSpawn + bombsPerSpawnIncrease);
        }
        
        // Увеличиваем шанс группового спавна
        currentGroupSpawnChance = Mathf.Min(0.8f, currentGroupSpawnChance + groupSpawnChanceIncrease);

        Debug.Log($"Сложность увеличена! " +
                 $"Интервал: {currentBombInterval:F1}s, " +
                 $"Время взрыва: {currentBombLifetime:F1}s, " +
                 $"Макс бомб: {currentMaxBombs}, " +
                 $"Бомб за спавн: {currentBombsPerSpawn}, " +
                 $"Шанс группы: {currentGroupSpawnChance:P0}");
    }

    public void ResetDifficulty()
    {
        currentBombInterval = initialBombInterval;
        currentBombLifetime = maxBombLifetime;
        currentMaxBombs = initialMaxBombs;
        currentBombsPerSpawn = initialBombsPerSpawn;
        currentGroupSpawnChance = groupSpawnChance;
    }
}