using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using Game.Characters;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private CanvasGroup gameOverCanvasGroup;
    [SerializeField] private RectTransform gameOverPanelRect;
    [SerializeField] private Text finalScoreText;
    [SerializeField] private Text gameOverTitleText;
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
    [SerializeField] private Text scoreText;
    [SerializeField] private Text timerText;
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

    private GameManager gameManager;
    private bool isPauseMenuShowing = false;
    private bool isGameOverShowing = false;

    // Компоненты для принудительного включения
    private CanvasScaler gameOverCanvasScaler;
    private GraphicRaycaster gameOverRaycaster;
    private CanvasScaler pauseCanvasScaler;
    private GraphicRaycaster pauseRaycaster;

    private void Start()
    {
        InitializeUI();
        SetupButtonListeners();
        gameManager = FindObjectOfType<GameManager>();
        
        // Получаем компоненты для принудительного включения
        gameOverCanvasScaler = gameOverPanel.GetComponent<CanvasScaler>();
        gameOverRaycaster = gameOverPanel.GetComponent<GraphicRaycaster>();
        pauseCanvasScaler = pausePanel.GetComponent<CanvasScaler>();
        pauseRaycaster = pausePanel.GetComponent<GraphicRaycaster>();
        
        // Гарантируем, что игровой UI интерактивен
        if (inGameUICanvasGroup != null)
        {
            inGameUICanvasGroup.blocksRaycasts = true;
            inGameUICanvasGroup.interactable = true;
        }
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

        // Сбрасываем scale панелей
        if (gameOverPanelRect != null)
        {
            gameOverPanelRect.localScale = Vector3.zero;
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
            inGameUICanvasGroup.blocksRaycasts = true;
            inGameUICanvasGroup.interactable = true;
        }
    }

    private void SetupButtonListeners()
    {
        // Game Over кнопки
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
        if (menuButton != null)
            menuButton.onClick.AddListener(ReturnToMenu);

        // Pause кнопки
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);
        if (pauseRestartButton != null)
            pauseRestartButton.onClick.AddListener(RestartGame);
        if (pauseMenuButton != null)
            pauseMenuButton.onClick.AddListener(ReturnToMenu);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (gameManager != null && !gameManager.IsGameOver && !isGameOverShowing)
            {
                if (isPauseMenuShowing)
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
        if (isGameOverShowing) return;
        
        isGameOverShowing = true;
        
        // Скрываем паузу если она была показана
        if (isPauseMenuShowing)
        {
            HidePauseMenu();
        }

        // Включаем блокировку raycast'ов ДО активации панели
        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.blocksRaycasts = true;
            gameOverCanvasGroup.interactable = true;
        }
        
        // ПРИНУДИТЕЛЬНО включаем компоненты
        ForceEnableCanvasComponents(gameOverPanel, gameOverCanvasScaler, gameOverRaycaster);
        
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
            gameOverSequence.Join(backgroundOverlay.DOFade(0.7f, fadeDuration)
                .SetEase(fadeEase)
                .SetUpdate(true));
        }

        // 2. Появление панели с scale анимацией
        if (gameOverPanelRect != null)
        {
            gameOverSequence.Join(gameOverPanelRect.DOScale(originalPanelScale, panelScaleDuration)
                .SetEase(scaleEase)
                .SetUpdate(true));
        }

        // 3. Fade in всей панели
        if (gameOverCanvasGroup != null)
        {
            gameOverSequence.Join(gameOverCanvasGroup.DOFade(1f, fadeDuration)
                .SetEase(fadeEase)
                .SetUpdate(true));
        }

        // 4. Анимация текста счета (постепенное появление)
        gameOverSequence.AppendCallback(() => StartCoroutine(AnimateScoreText(finalScore)));

        // 5. Анимация кнопок с задержкой
        gameOverSequence.AppendInterval(textTypingDuration + 0.3f);
        gameOverSequence.AppendCallback(() => AnimateButtons());

        // Скрываем игровой UI
        /*if (inGameUICanvasGroup != null)
        {
            inGameUICanvasGroup.DOFade(0f, fadeDuration * 0.5f)
                .SetEase(fadeEase)
                .SetUpdate(true);
        }*/
    }

    public void ShowPauseMenu()
    {
        if (isPauseMenuShowing || isGameOverShowing) return;
        
        isPauseMenuShowing = true;
        
        // Включаем блокировку raycast'ов ДО активации панели
        if (pauseCanvasGroup != null)
        {
            pauseCanvasGroup.blocksRaycasts = true;
            pauseCanvasGroup.interactable = true;
        }
        
        // ПРИНУДИТЕЛЬНО включаем компоненты
        ForceEnableCanvasComponents(pausePanel, pauseCanvasScaler, pauseRaycaster);
        
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
            pauseSequence.Join(backgroundOverlay.DOFade(0.7f, fadeDuration)
                .SetEase(fadeEase)
                .SetUpdate(true));
        }

        // 2. Появление панели с scale анимацией
        if (pausePanelRect != null)
        {
            pauseSequence.Join(pausePanelRect.DOScale(originalPausePanelScale, panelScaleDuration)
                .SetEase(scaleEase)
                .SetUpdate(true));
        }

        // 3. Fade in всей панели
        if (pauseCanvasGroup != null)
        {
            pauseSequence.Join(pauseCanvasGroup.DOFade(1f, fadeDuration)
                .SetEase(fadeEase)
                .SetUpdate(true));
        }

        // 4. Анимация кнопок с задержкой
        pauseSequence.AppendCallback(() => AnimatePauseButtons());

        // Слегка затемняем игровой UI
        if (inGameUICanvasGroup != null)
        {
            inGameUICanvasGroup.DOFade(0.7f, fadeDuration * 0.5f)
                .SetEase(fadeEase)
                .SetUpdate(true);
        }
    }

    // ВАЖНЫЙ МЕТОД: Принудительное включение компонентов Canvas
    private void ForceEnableCanvasComponents(GameObject panel, CanvasScaler scaler, GraphicRaycaster raycaster)
    {
        // Принудительно активируем панель и все её дочерние элементы
        panel.SetActive(true);
        
        // Включаем и принудительно обновляем все компоненты Canvas
        Canvas canvas = panel.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.enabled = false;
            canvas.enabled = true;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 1000; // Высокий приоритет
        }
        
        if (scaler != null)
        {
            scaler.enabled = false;
            scaler.enabled = true;
        }
        
        if (raycaster != null)
        {
            raycaster.enabled = false;
            raycaster.enabled = true;
        }
        
        // Принудительно включаем все кнопки и делаем их интерактивными
        Button[] buttons = panel.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            button.enabled = false;
            button.enabled = true;
            button.interactable = true;
            
            // Принудительно обновляем навигацию
            Navigation nav = button.navigation;
            button.navigation = nav;
        }

        // Принудительно обновляем все layout'ы
        LayoutGroup[] layouts = panel.GetComponentsInChildren<LayoutGroup>(true);
        foreach (LayoutGroup layout in layouts)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(layout.GetComponent<RectTransform>());
        }

        // Принудительное обновление EventSystem
        StartCoroutine(ForceUpdateEventSystem());
    }

    private IEnumerator ForceUpdateEventSystem()
    {
        yield return null;
        
        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem != null)
        {
            eventSystem.UpdateModules();
            eventSystem.SetSelectedGameObject(null);
        }
        
        Canvas.ForceUpdateCanvases();
    }

    public void HidePauseMenu()
    {
        if (!isPauseMenuShowing) return;
        
        // Останавливаем анимации
        pauseSequence?.Kill();

        // Отключаем блокировку raycast'ов
        if (pauseCanvasGroup != null)
        {
            pauseCanvasGroup.blocksRaycasts = false;
            pauseCanvasGroup.interactable = false;
        }

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
            isPauseMenuShowing = false;
            
            // Восстанавливаем игровой UI
            if (inGameUICanvasGroup != null)
            {
                inGameUICanvasGroup.blocksRaycasts = true;
                inGameUICanvasGroup.interactable = true;
            }
        });
    }

    private void ResetUIForAnimation()
    {
        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.alpha = 0f;
            gameOverCanvasGroup.blocksRaycasts = true;
            gameOverCanvasGroup.interactable = true;
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
        if (restartButton != null)
            restartButton.transform.localScale = Vector3.zero;
        if (menuButton != null)
            menuButton.transform.localScale = Vector3.zero;

        // Сбрасываем текст
        if (finalScoreText != null)
            finalScoreText.text = "";
        if (gameOverTitleText != null)
        {
            var color = gameOverTitleText.color;
            color.a = 0f;
            gameOverTitleText.color = color;
        }
    }

    private void ResetPauseUIForAnimation()
    {
        if (pauseCanvasGroup != null)
        {
            pauseCanvasGroup.alpha = 0f;
            pauseCanvasGroup.blocksRaycasts = true;
            pauseCanvasGroup.interactable = true;
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
        if (resumeButton != null)
            resumeButton.transform.localScale = Vector3.zero;
        if (pauseRestartButton != null)
            pauseRestartButton.transform.localScale = Vector3.zero;
        if (pauseMenuButton != null)
            pauseMenuButton.transform.localScale = Vector3.zero;
    }

    private IEnumerator AnimateScoreText(int finalScore)
    {
        // Анимация заголовка
        if (gameOverTitleText != null)
        {
            gameOverTitleText.DOFade(1f, 0.5f)
                .SetEase(fadeEase)
                .SetUpdate(true);
            yield return new WaitForSecondsRealtime(0.2f);
        }

        // Анимация набора текста счета
        if (finalScoreText != null)
        {
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
    }

    private void AnimateButtons()
    {
        // Анимация кнопки рестарта
        if (restartButton != null)
        {
            restartButton.transform.DOScale(Vector3.one, panelScaleDuration * 0.7f)
                .SetEase(scaleEase)
                .SetUpdate(true);
        }

        // Анимация кнопки меню с задержкой
        if (menuButton != null)
        {
            menuButton.transform.DOScale(Vector3.one, panelScaleDuration * 0.7f)
                .SetDelay(buttonStaggerDelay)
                .SetEase(scaleEase)
                .SetUpdate(true);
        }
    }

    private void AnimatePauseButtons()
    {
        // Анимация кнопок паузы с задержкой между ними
        if (resumeButton != null)
        {
            resumeButton.transform.DOScale(Vector3.one, panelScaleDuration * 0.7f)
                .SetEase(scaleEase)
                .SetUpdate(true);
        }

        if (pauseRestartButton != null)
        {
            pauseRestartButton.transform.DOScale(Vector3.one, panelScaleDuration * 0.7f)
                .SetDelay(buttonStaggerDelay)
                .SetEase(scaleEase)
                .SetUpdate(true);
        }

        if (pauseMenuButton != null)
        {
            pauseMenuButton.transform.DOScale(Vector3.one, panelScaleDuration * 0.7f)
                .SetDelay(buttonStaggerDelay * 2)
                .SetEase(scaleEase)
                .SetUpdate(true);
        }
    }

    public void HideGameOver()
    {
        if (!isGameOverShowing) return;
        
        // Останавливаем анимации
        gameOverSequence?.Kill();

        // Отключаем блокировку raycast'ов
        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.blocksRaycasts = false;
            gameOverCanvasGroup.interactable = false;
        }

        Sequence hideSequence = DOTween.Sequence();
        hideSequence.SetUpdate(true);

        // 1. Исчезновение кнопок
        if (restartButton != null)
            hideSequence.Join(restartButton.transform.DOScale(0f, fadeDuration * 0.5f).SetEase(fadeEase));
        if (menuButton != null)
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
            isGameOverShowing = false;
            
            // Восстанавливаем игровой UI
            if (inGameUICanvasGroup != null)
            {
                inGameUICanvasGroup.blocksRaycasts = true;
                inGameUICanvasGroup.interactable = true;
            }
        });
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = $": {score}";
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

    // ЕДИНЫЙ метод для паузы - вызывается и из кнопки, и из клавиши Esc
    public void PauseGame()
    {
        if (gameManager != null && !isPauseMenuShowing && !isGameOverShowing)
        {
            gameManager.SetGamePaused(true);
            ShowPauseMenu();
        }
    }

    // ЕДИНЫЙ метод для возобновления - вызывается и из кнопки, и из клавиши Esc
    public void ResumeGame()
    {
        if (gameManager != null && isPauseMenuShowing)
        {
            gameManager.SetGamePaused(false);
            HidePauseMenu();
        }
    }

    private void RestartGame()
    {
        // Скрываем все UI перед рестартом
        if (isGameOverShowing)
        {
            HideGameOver();
        }
        if (isPauseMenuShowing)
        {
            HidePauseMenu();
        }
        
        // Небольшая задержка для завершения анимаций
        StartCoroutine(DelayedRestart());
    }

    private IEnumerator DelayedRestart()
    {
        yield return new WaitForSecondsRealtime(0.3f);
        
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.RestartGame();
        }
        
        // Сбрасываем флаги
        isGameOverShowing = false;
        isPauseMenuShowing = false;
    }

    private void ReturnToMenu()
    {
        SceneManager.LoadScene(0);
    }

    private void OnDestroy()
    {
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