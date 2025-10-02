using UnityEngine;
using Game.Grid;
using System.Collections;
using System.Collections.Generic;

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
    private Coroutine carrotSpawningCoroutine;
    private Coroutine bombSpawningCoroutine;

    private void Start()
    {
        StartCoroutine(SpawnCarrots());
        bombSpawningCoroutine = StartCoroutine(SpawnBombs());
        Bomb.OnExploded += HandleBombExploded;
        Carrot.OnCollected += HandleCarrotCollected;
        Carrot.OnExpired += HandleCarrotExpired;
    }

    private void OnDestroy()
    {
        if (carrotSpawningCoroutine != null)
            StopCoroutine(carrotSpawningCoroutine);
        if (bombSpawningCoroutine != null)
            StopCoroutine(bombSpawningCoroutine);
        
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

    private IEnumerator SpawnCarrots()
    {
        yield return new WaitForSeconds(1f);

        while (true)
        {
            yield return new WaitForSeconds(carrotInterval);
            
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

    private IEnumerator SpawnBombs()
    {
        yield return new WaitForSeconds(1f);

        while (true)
        {
            float currentInterval = DifficultyManager.Instance.CurrentBombInterval;
            yield return new WaitForSeconds(currentInterval);
            
            int bombsToSpawn = DetermineBombsToSpawn();
            
            for (int i = 0; i < bombsToSpawn; i++)
            {
                if (activeBombs.Count < DifficultyManager.Instance.CurrentMaxBombs)
                {
                    TrySpawnBomb();
                    
                    if (i < bombsToSpawn - 1)
                        yield return new WaitForSeconds(0.1f);
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
        
        if (bombSpawningCoroutine != null)
            StopCoroutine(bombSpawningCoroutine);
        bombSpawningCoroutine = StartCoroutine(SpawnBombs());
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