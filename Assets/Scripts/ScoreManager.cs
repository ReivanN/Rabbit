using UnityEngine;
using System;

public class ScoreManager : MonoBehaviour
{
    // Singleton
    public static ScoreManager Instance { get; private set; }

    // Событие на изменение счёта
    public event Action<int> OnScoreChanged;

    [SerializeField] private int startingScore = 0;
    private int currentScore;

    public int CurrentScore => currentScore;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        currentScore = startingScore;
    }

    /// <summary>
    /// Добавить очки к текущему счёту
    /// </summary>
    public void AddScore(int amount)
    {
        currentScore += amount;
        OnScoreChanged?.Invoke(currentScore);
        Debug.Log($"ScoreManager: новый счёт = {currentScore}");
    }

    /// <summary>
    /// Сбросить счёт до начального значения
    /// </summary>
    public void ResetScore()
    {
        currentScore = startingScore;
        OnScoreChanged?.Invoke(currentScore);
    }
}