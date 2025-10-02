using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreUI : MonoBehaviour
{
    public static ScoreUI Instance { get; private set; }
    
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private string scorePrefix = "Score: ";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Подписываемся на событие изменения счёта
        ScoreManager.Instance.OnScoreChanged += UpdateScoreDisplay;
        
        // Инициализируем отображение
        UpdateScoreDisplay(ScoreManager.Instance.CurrentScore);
    }

    private void OnDestroy()
    {
        // Отписываемся от события
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged -= UpdateScoreDisplay;
    }

    public void UpdateScoreDisplay(int currentScore)
    {
        if (scoreText != null)
            scoreText.text = scorePrefix + currentScore.ToString();
    }
}