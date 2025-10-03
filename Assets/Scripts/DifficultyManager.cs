using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

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
    private CancellationTokenSource difficultyCancellationTokenSource;

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

        currentBombInterval = initialBombInterval;
        currentBombLifetime = maxBombLifetime;
        currentMaxBombs = initialMaxBombs;
        currentBombsPerSpawn = initialBombsPerSpawn;
        currentGroupSpawnChance = groupSpawnChance;
    }

    private void Start()
    {
        difficultyCancellationTokenSource = new CancellationTokenSource();
        IncreaseDifficultyOverTimeAsync(difficultyCancellationTokenSource.Token).Forget();
    }

    private async UniTaskVoid IncreaseDifficultyOverTimeAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // Ждем интервал, учитывая паузу
            await UniTask.Delay((int)(difficultyIncreaseInterval * 1000), 
                cancellationToken: cancellationToken,
                ignoreTimeScale: false);
            
            if (!cancellationToken.IsCancellationRequested)
            {
                IncreaseDifficulty();
            }
        }
    }

    private void IncreaseDifficulty()
    {
        currentBombInterval = Mathf.Max(minBombInterval, currentBombInterval - bombIntervalDecrease);
        currentBombLifetime = Mathf.Max(minBombLifetime, currentBombLifetime - lifetimeDecrease);
        currentMaxBombs = Mathf.Min(maxBombsOnScreen, currentMaxBombs + bombLimitIncrease);
        
        if (Random.Range(0, 2) == 0)
        {
            currentBombsPerSpawn = Mathf.Min(maxBombsPerSpawn, currentBombsPerSpawn + bombsPerSpawnIncrease);
        }
        
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

    private void OnDestroy()
    {
        difficultyCancellationTokenSource?.Cancel();
        difficultyCancellationTokenSource?.Dispose();
    }
}