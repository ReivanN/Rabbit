using UnityEngine;
using Game.Grid;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

public class ItemManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject carrotPrefab;
    [SerializeField] private GameObject bombPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float carrotInterval = 2f;
    [SerializeField] private int maxCarrots = 3;

    [SerializeField] private GridManager grid;

    private List<Bomb> activeBombs = new List<Bomb>();
    private List<Carrot> activeCarrots = new List<Carrot>();
    private CancellationTokenSource carrotSpawningCancellationTokenSource;
    private CancellationTokenSource bombSpawningCancellationTokenSource;

    private void Start()
    {
        carrotSpawningCancellationTokenSource = new CancellationTokenSource();
        bombSpawningCancellationTokenSource = new CancellationTokenSource();
        
        SpawnCarrotsAsync(carrotSpawningCancellationTokenSource.Token).Forget();
        SpawnBombsAsync(bombSpawningCancellationTokenSource.Token).Forget();
        
        Bomb.OnExploded += HandleBombExploded;
        Carrot.OnCollected += HandleCarrotCollected;
        Carrot.OnExpired += HandleCarrotExpired;
    }

    private void OnDestroy()
    {
        // Отменяем все UniTask'и
        carrotSpawningCancellationTokenSource?.Cancel();
        carrotSpawningCancellationTokenSource?.Dispose();
        bombSpawningCancellationTokenSource?.Cancel();
        bombSpawningCancellationTokenSource?.Dispose();
        
        Bomb.OnExploded -= HandleBombExploded;
        Carrot.OnCollected -= HandleCarrotCollected;
        Carrot.OnExpired -= HandleCarrotExpired;
    }

    private void HandleCarrotCollected(Carrot carrot)
    {
        if (activeCarrots.Contains(carrot))
        {
            activeCarrots.Remove(carrot);
        }
    }

    private void HandleCarrotExpired(Carrot carrot)
    {
        if (activeCarrots.Contains(carrot))
        {
            activeCarrots.Remove(carrot);
        }
    }

    private async UniTaskVoid SpawnCarrotsAsync(CancellationToken cancellationToken)
    {
        // Начальная задержка
        await UniTask.Delay(1000, cancellationToken: cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            await UniTask.Delay((int)(carrotInterval * 1000), 
                cancellationToken: cancellationToken,
                ignoreTimeScale: false);
            
            if (cancellationToken.IsCancellationRequested) break;
            
            if (activeCarrots.Count < maxCarrots)
            {
                TrySpawnCarrot();
            }
            else
            {
                activeCarrots.RemoveAll(carrot => carrot == null);
            }
        }
    }

    private async UniTaskVoid SpawnBombsAsync(CancellationToken cancellationToken)
    {
        // Начальная задержка
        await UniTask.Delay(1000, cancellationToken: cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            float currentInterval = DifficultyManager.Instance.CurrentBombInterval;
            await UniTask.Delay((int)(currentInterval * 1000), 
                cancellationToken: cancellationToken,
                ignoreTimeScale: false);
            
            if (cancellationToken.IsCancellationRequested) break;
            
            int bombsToSpawn = DetermineBombsToSpawn();
            
            for (int i = 0; i < bombsToSpawn; i++)
            {
                if (activeBombs.Count < DifficultyManager.Instance.CurrentMaxBombs)
                {
                    TrySpawnBomb();
                    
                    if (i < bombsToSpawn - 1)
                    {
                        await UniTask.Delay(100, cancellationToken: cancellationToken);
                    }
                }
            }
        }
    }

    private int DetermineBombsToSpawn()
    {
        int baseSpawnCount = DifficultyManager.Instance.CurrentBombsPerSpawn;
        float groupChance = DifficultyManager.Instance.CurrentGroupSpawnChance;
        
        if (Random.value <= groupChance)
        {
            int extraBombs = Random.Range(1, 3);
            return Mathf.Min(baseSpawnCount + extraBombs, DifficultyManager.Instance.CurrentMaxBombs - activeBombs.Count);
        }
        else
        {
            return Mathf.Min(baseSpawnCount, DifficultyManager.Instance.CurrentMaxBombs - activeBombs.Count);
        }
    }

    private void HandleBombExploded(Bomb bomb, GridCell[] affected)
    {
        if (activeBombs.Contains(bomb))
        {
            activeBombs.Remove(bomb);
        }
    }

    private void TrySpawnCarrot()
    {
        if (activeCarrots.Count >= maxCarrots)
            return;

        var cell = GetRandomFreeCell();
        if (cell == null) return;

        var go = Instantiate(carrotPrefab);
        var carrot = go.GetComponent<Carrot>();
        carrot.Init(cell);
        cell.SpawnedObject = go;
        cell.HasCarrot = true;
        
        activeCarrots.Add(carrot);
    }

    private void TrySpawnBomb()
    {
        var cell = GetRandomFreeCell();
        if (cell == null) return;

        var go = Instantiate(bombPrefab);
        var bomb = go.GetComponent<Bomb>();
        
        float lifetime = DifficultyManager.Instance.CurrentBombLifetime;
        bomb.Init(cell, grid, lifetime);
        
        cell.HasBomb = true;
        cell.SpawnedObject = go;
        activeBombs.Add(bomb);
    }

    private GridCell GetRandomFreeCell()
    {
        for (int i = 0; i < 10; i++)
        {
            int x = Random.Range(0, grid.Width);
            int y = Random.Range(0, grid.Height);
            var cell = grid.GetCell(new Vector2Int(x, y));
            if (cell != null && cell.IsWalkable && !cell.IsOccupied && !cell.HasBomb && !cell.HasCarrot)
                return cell;
        }
        
        for (int i = 0; i < grid.Width * grid.Height; i++)
        {
            int x = Random.Range(0, grid.Width);
            int y = Random.Range(0, grid.Height);
            var cell = grid.GetCell(new Vector2Int(x, y));
            if (cell != null && cell.IsWalkable && !cell.IsOccupied && !cell.HasBomb && !cell.HasCarrot)
                return cell;
        }
        
        return null;
    }

    public void ResetGame()
    {
        // Отменяем текущие таски спавна
        carrotSpawningCancellationTokenSource?.Cancel();
        bombSpawningCancellationTokenSource?.Cancel();
        
        // Пересоздаем CancellationTokenSource для новых тасков
        carrotSpawningCancellationTokenSource?.Dispose();
        bombSpawningCancellationTokenSource?.Dispose();
        
        carrotSpawningCancellationTokenSource = new CancellationTokenSource();
        bombSpawningCancellationTokenSource = new CancellationTokenSource();

        foreach (var bomb in activeBombs)
        {
            if (bomb != null)
                Destroy(bomb.gameObject);
        }
        activeBombs.Clear();
        
        foreach (var carrot in activeCarrots)
        {
            if (carrot != null)
                Destroy(carrot.gameObject);
        }
        activeCarrots.Clear();
        
        // Перезапускаем спавн
        SpawnCarrotsAsync(carrotSpawningCancellationTokenSource.Token).Forget();
        SpawnBombsAsync(bombSpawningCancellationTokenSource.Token).Forget();
    }

    public int GetCurrentCarrotCount()
    {
        return activeCarrots.Count;
    }

    public int GetMaxCarrotCount()
    {
        return maxCarrots;
    }
}