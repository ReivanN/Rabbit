using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using Game.Characters;

public class UIManager : MonoBehaviour
{
    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private CanvasGroup gameOverCanvasGroup;
    [SerializeField] private RectTransform gameOverPanelRect;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI gameOverTitleText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private Image backgroundOverlay;

    [Header("Pause UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private CanvasGroup pauseCanvasGroup;
    [SerializeField] private RectTransform pausePanelRect;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseRestartButton;
    [SerializeField] private Button pauseMenuButton;

    [Header("In-Game UI")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private CanvasGroup inGameUICanvasGroup;

    [Header("Animation Settings")]
    [SerializeField] private float panelScaleDuration = 0.5f;
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private float buttonStaggerDelay = 0.2f;
    [SerializeField] private float textTypingDuration = 1f;
    [SerializeField] private Ease scaleEase = Ease.OutBack;
    [SerializeField] private Ease fadeEase = Ease.OutQuad;

    private Vector3 originalPanelScale;
    private Vector3 originalPausePanelScale;
    private Sequence gameOverSequence;
    private Sequence pauseSequence;

    // Ссылка на GameManager для управления паузой
    private GameManager gameManager;

    private void Start()
    {
        InitializeUI();
        SetupButtonListeners();
        gameManager = FindObjectOfType<GameManager>();
    }

    private void InitializeUI()
    {
        // Сохраняем оригинальный scale панелей
        if (gameOverPanelRect != null)
        {
            originalPanelScale = gameOverPanelRect.localScale;
        }

        if (pausePanelRect != null)
        {
            originalPausePanelScale = pausePanelRect.localScale;
        }

        // Скрываем панель Game Over с начальными значениями
        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.alpha = 0f;
            //gameOverCanvasGroup.interactable = false;
            //gameOverCanvasGroup.blocksRaycasts = false;
        }

        if (gameOverPanelRect != null)
        {
            gameOverPanelRect.localScale = Vector3.zero;
        }

        // Скрываем панель Pause с начальными значениями
        if (pauseCanvasGroup != null)
        {
            pauseCanvasGroup.alpha = 0f;
            //pauseCanvasGroup.interactable = false;
            //pauseCanvasGroup.blocksRaycasts = false;
        }

        if (pausePanelRect != null)
        {
            pausePanelRect.localScale = Vector3.zero;
        }

        if (backgroundOverlay != null)
        {
            backgroundOverlay.color = new Color(0, 0, 0, 0);
        }

        // Скрываем панели
        gameOverPanel.SetActive(false);
        pausePanel.SetActive(false);

        // Показываем игровой UI
        if (inGameUICanvasGroup != null)
        {
            inGameUICanvasGroup.alpha = 1f;
        }
    }

    private void SetupButtonListeners()
    {
        // Game Over кнопки
        restartButton.onClick.AddListener(RestartGame);
        menuButton.onClick.AddListener(ReturnToMenu);

        // Pause кнопки
        resumeButton.onClick.AddListener(ResumeGame);
        pauseRestartButton.onClick.AddListener(RestartGame);
        pauseMenuButton.onClick.AddListener(ReturnToMenu);

        // Добавляем анимации наведения на кнопки
        SetupButtonHoverEffects(restartButton);
        SetupButtonHoverEffects(menuButton);
        SetupButtonHoverEffects(resumeButton);
        SetupButtonHoverEffects(pauseRestartButton);
        SetupButtonHoverEffects(pauseMenuButton);
    }

    private void SetupButtonHoverEffects(Button button)
    {
        var buttonTransform = button.GetComponent<RectTransform>();
        var originalScale = buttonTransform.localScale;

        // Анимация при наведении
        button.onClick.AddListener(() => PlayButtonClickAnimation(buttonTransform));
    }

    private void Update()
    {
        // Обработка ввода для паузы
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (gameManager != null && !gameManager.IsGameOver)
            {
                if (gameManager.IsGamePaused)
                {
                    ResumeGame();
                }
                else
                {
                    PauseGame();
                }
            }
        }
    }

    public void ShowGameOver(int finalScore)
    {
        gameOverPanel.SetActive(true);
        
        // Останавливаем предыдущую анимацию если она есть
        gameOverSequence?.Kill();

        // Сбрасываем значения перед анимацией
        ResetUIForAnimation();

        // Создаем последовательность анимаций
        gameOverSequence = DOTween.Sequence();
        gameOverSequence.SetUpdate(true);

        // 1. Затемнение фона
        if (backgroundOverlay != null)
        {
            gameOverSequence.Join(backgroundOverlay.DOFade(0.7f, fadeDuration).SetEase(fadeEase));
        }

        // 2. Появление панели с scale анимацией
        if (gameOverPanelRect != null)
        {
            gameOverSequence.Join(gameOverPanelRect.DOScale(originalPanelScale, panelScaleDuration)
                .SetEase(scaleEase));
        }

        // 3. Fade in всей панели
        if (gameOverCanvasGroup != null)
        {
            gameOverSequence.Join(gameOverCanvasGroup.DOFade(1f, fadeDuration).SetEase(fadeEase));
        }

        // 4. Анимация текста счета (постепенное появление)
        gameOverSequence.AppendCallback(() => StartCoroutine(AnimateScoreText(finalScore)));

        // 5. Анимация кнопок с задержкой
        gameOverSequence.AppendInterval(textTypingDuration + 0.3f);
        gameOverSequence.AppendCallback(() => AnimateButtons());

        // Включаем интерактивность после анимации
        gameOverSequence.OnComplete(() =>
        {
            if (gameOverCanvasGroup != null)
            {
                gameOverCanvasGroup.interactable = true;
                gameOverCanvasGroup.blocksRaycasts = true;
            }
        });

        // Скрываем игровой UI
        if (inGameUICanvasGroup != null)
        {
            inGameUICanvasGroup.DOFade(0f, fadeDuration * 0.5f).SetEase(fadeEase);
        }
    }

    public void ShowPauseMenu()
    {
        pausePanel.SetActive(true);
        
        // Останавливаем предыдущую анимацию если она есть
        pauseSequence?.Kill();

        // Сбрасываем значения перед анимацией
        ResetPauseUIForAnimation();

        // Создаем последовательность анимаций
        pauseSequence = DOTween.Sequence();
        pauseSequence.SetUpdate(true);

        // 1. Затемнение фона
        if (backgroundOverlay != null)
        {
            pauseSequence.Join(backgroundOverlay.DOFade(0.7f, fadeDuration).SetEase(fadeEase));
        }

        // 2. Появление панели с scale анимацией
        if (pausePanelRect != null)
        {
            pauseSequence.Join(pausePanelRect.DOScale(originalPausePanelScale, panelScaleDuration)
                .SetEase(scaleEase));
        }

        // 3. Fade in всей панели
        if (pauseCanvasGroup != null)
        {
            pauseSequence.Join(pauseCanvasGroup.DOFade(1f, fadeDuration).SetEase(fadeEase));
        }

        // 4. Анимация кнопок с задержкой
        pauseSequence.AppendCallback(() => AnimatePauseButtons());

        // Включаем интерактивность после анимации
        pauseSequence.OnComplete(() =>
        {
            if (pauseCanvasGroup != null)
            {
                pauseCanvasGroup.interactable = true;
                pauseCanvasGroup.blocksRaycasts = true;
            }
        });

        // Слегка затемняем игровой UI
        if (inGameUICanvasGroup != null)
        {
            inGameUICanvasGroup.DOFade(0.7f, fadeDuration * 0.5f).SetEase(fadeEase);
        }
    }

    public void HidePauseMenu()
    {
        // Останавливаем анимации
        pauseSequence?.Kill();

        Sequence hideSequence = DOTween.Sequence();
        hideSequence.SetUpdate(true);

        // 1. Исчезновение кнопок
        hideSequence.Join(resumeButton.transform.DOScale(0f, fadeDuration * 0.5f).SetEase(fadeEase));
        hideSequence.Join(pauseRestartButton.transform.DOScale(0f, fadeDuration * 0.5f).SetEase(fadeEase));
        hideSequence.Join(pauseMenuButton.transform.DOScale(0f, fadeDuration * 0.5f).SetEase(fadeEase));

        // 2. Исчезновение панели
        if (pausePanelRect != null)
        {
            hideSequence.Join(pausePanelRect.DOScale(0f, fadeDuration).SetEase(fadeEase));
        }

        if (pauseCanvasGroup != null)
        {
            hideSequence.Join(pauseCanvasGroup.DOFade(0f, fadeDuration).SetEase(fadeEase));
        }

        // 3. Исчезновение фона
        if (backgroundOverlay != null)
        {
            hideSequence.Join(backgroundOverlay.DOFade(0f, fadeDuration).SetEase(fadeEase));
        }

        // 4. Восстанавливаем игровой UI
        if (inGameUICanvasGroup != null)
        {
            inGameUICanvasGroup.DOFade(1f, fadeDuration * 0.5f).SetEase(fadeEase);
        }

        hideSequence.OnComplete(() =>
        {
            pausePanel.SetActive(false);
            if (pauseCanvasGroup != null)
            {
                pauseCanvasGroup.interactable = false;
                pauseCanvasGroup.blocksRaycasts = false;
            }
        });
    }

    private void ResetUIForAnimation()
    {
        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.alpha = 0f;
            gameOverCanvasGroup.interactable = false;
            gameOverCanvasGroup.blocksRaycasts = false;
        }

        if (gameOverPanelRect != null)
        {
            gameOverPanelRect.localScale = Vector3.zero;
        }

        if (backgroundOverlay != null)
        {
            backgroundOverlay.color = new Color(0, 0, 0, 0);
        }

        // Сбрасываем кнопки
        restartButton.transform.localScale = Vector3.zero;
        menuButton.transform.localScale = Vector3.zero;

        // Сбрасываем текст
        finalScoreText.text = "";
        if (gameOverTitleText != null)
        {
            gameOverTitleText.alpha = 0f;
        }
    }

    private void ResetPauseUIForAnimation()
    {
        if (pauseCanvasGroup != null)
        {
            pauseCanvasGroup.alpha = 0f;
            pauseCanvasGroup.interactable = false;
            pauseCanvasGroup.blocksRaycasts = false;
        }

        if (pausePanelRect != null)
        {
            pausePanelRect.localScale = Vector3.zero;
        }

        if (backgroundOverlay != null)
        {
            backgroundOverlay.color = new Color(0, 0, 0, 0);
        }

        // Сбрасываем кнопки
        resumeButton.transform.localScale = Vector3.zero;
        pauseRestartButton.transform.localScale = Vector3.zero;
        pauseMenuButton.transform.localScale = Vector3.zero;
    }

    private IEnumerator AnimateScoreText(int finalScore)
    {
        // Анимация заголовка
        if (gameOverTitleText != null)
        {
            gameOverTitleText.DOFade(1f, 0.5f).SetEase(fadeEase);
            yield return new WaitForSeconds(0.2f);
        }

        // Анимация набора текста счета
        finalScoreText.text = "0";
        int currentScore = 0;
        float scoreProgress = 0f;

        while (currentScore < finalScore)
        {
            scoreProgress += Time.unscaledDeltaTime / textTypingDuration;
            currentScore = Mathf.FloorToInt(Mathf.Lerp(0, finalScore, scoreProgress));
            finalScoreText.text = $"SCORE: {currentScore}";

            // Добавляем эффект "прыжка" для цифр
            if (currentScore % 5 == 0 || currentScore == finalScore)
            {
                finalScoreText.transform.DOScale(1.1f, 0.1f)
                    .SetLoops(2, LoopType.Yoyo)
                    .SetUpdate(true);
            }

            yield return null;
        }

        // Финальное значение
        finalScoreText.text = $"SCORE: {finalScore}";
    }

    private void AnimateButtons()
    {
        // Анимация кнопки рестарта
        restartButton.transform.DOScale(Vector3.one, panelScaleDuration * 0.7f)
            .SetEase(scaleEase)
            .SetUpdate(true);

        // Анимация кнопки меню с задержкой
        menuButton.transform.DOScale(Vector3.one, panelScaleDuration * 0.7f)
            .SetDelay(buttonStaggerDelay)
            .SetEase(scaleEase)
            .SetUpdate(true);
    }

    private void AnimatePauseButtons()
    {
        // Анимация кнопок паузы с задержкой между ними
        resumeButton.transform.DOScale(Vector3.one, panelScaleDuration * 0.7f)
            .SetEase(scaleEase)
            .SetUpdate(true);

        pauseRestartButton.transform.DOScale(Vector3.one, panelScaleDuration * 0.7f)
            .SetDelay(buttonStaggerDelay)
            .SetEase(scaleEase)
            .SetUpdate(true);

        pauseMenuButton.transform.DOScale(Vector3.one, panelScaleDuration * 0.7f)
            .SetDelay(buttonStaggerDelay * 2)
            .SetEase(scaleEase)
            .SetUpdate(true);
    }

    private void PlayButtonClickAnimation(RectTransform buttonTransform)
    {
        Sequence clickSequence = DOTween.Sequence();
        clickSequence.Append(buttonTransform.DOScale(0.9f, 0.1f).SetUpdate(true));
        clickSequence.Append(buttonTransform.DOScale(1f, 0.1f).SetUpdate(true));
    }

    public void HideGameOver()
    {
        // Останавливаем анимации
        gameOverSequence?.Kill();

        Sequence hideSequence = DOTween.Sequence();

        // 1. Исчезновение кнопок
        hideSequence.Join(restartButton.transform.DOScale(0f, fadeDuration * 0.5f).SetEase(fadeEase));
        hideSequence.Join(menuButton.transform.DOScale(0f, fadeDuration * 0.5f).SetEase(fadeEase));

        // 2. Исчезновение панели
        if (gameOverPanelRect != null)
        {
            hideSequence.Join(gameOverPanelRect.DOScale(0f, fadeDuration).SetEase(fadeEase));
        }

        if (gameOverCanvasGroup != null)
        {
            hideSequence.Join(gameOverCanvasGroup.DOFade(0f, fadeDuration).SetEase(fadeEase));
        }

        // 3. Исчезновение фона
        if (backgroundOverlay != null)
        {
            hideSequence.Join(backgroundOverlay.DOFade(0f, fadeDuration).SetEase(fadeEase));
        }

        // 4. Показ игрового UI
        if (inGameUICanvasGroup != null)
        {
            inGameUICanvasGroup.DOFade(1f, fadeDuration * 0.5f).SetEase(fadeEase);
        }

        hideSequence.OnComplete(() =>
        {
            gameOverPanel.SetActive(false);
            if (gameOverCanvasGroup != null)
            {
                gameOverCanvasGroup.interactable = false;
                gameOverCanvasGroup.blocksRaycasts = false;
            }
        });
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            // Легкая анимация при обновлении счета
            scoreText.text = $"CARROTS: {score}";
            scoreText.transform.DOScale(1.1f, 0.2f)
                .SetLoops(2, LoopType.Yoyo);
        }
    }

    public void UpdateTimer(float time)
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    private void PauseGame()
    {
        if (gameManager != null)
        {
            gameManager.SetGamePaused(true);
            ShowPauseMenu();
        }
    }

    private void ResumeGame()
    {
        PlayButtonClickAnimation(resumeButton.GetComponent<RectTransform>());
        
        // Небольшая задержка для анимации клика
        DOTween.Sequence()
            .AppendInterval(0.2f)
            .OnComplete(() =>
            {
                if (gameManager != null)
                {
                    gameManager.SetGamePaused(false);
                    HidePauseMenu();
                }
            })
            .SetUpdate(true);
    }

    private void RestartGame()
    {
        PlayButtonClickAnimation(restartButton.GetComponent<RectTransform>());
        
        // Скрываем все UI перед рестартом
        HideGameOver();
        HidePauseMenu();
        
        // Небольшая задержка для анимации клика
        DOTween.Sequence()
            .AppendInterval(0.3f)
            .OnComplete(() =>
            {
                GameManager gameManager = FindObjectOfType<GameManager>();
                if (gameManager != null)
                {
                    gameManager.RestartGame();
                }
            })
            .SetUpdate(true);
    }

    private void ReturnToMenu()
    {
        PlayButtonClickAnimation(menuButton.GetComponent<RectTransform>());
        
        // Здесь можно добавить переход в главное меню
        Debug.Log("Return to menu pressed");
        // SceneManager.LoadScene("MainMenu");
    }

    private void OnDestroy()
    {
        // Очищаем все твины при уничтожении объекта
        DOTween.KillAll();
        
        if (restartButton != null)
            restartButton.onClick.RemoveAllListeners();
        if (menuButton != null)
            menuButton.onClick.RemoveAllListeners();
        if (resumeButton != null)
            resumeButton.onClick.RemoveAllListeners();
        if (pauseRestartButton != null)
            pauseRestartButton.onClick.RemoveAllListeners();
        if (pauseMenuButton != null)
            pauseMenuButton.onClick.RemoveAllListeners();
    }
}