using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [Header("Main Menu")]
    [SerializeField] private MenuButton[] buttons;
    [SerializeField] private float animationDelay = 0.2f;
    [SerializeField] private Ease easeType = Ease.OutBack;

    [Header("Options Panel")]
    [SerializeField] private RectTransform optionsPanel;
    [SerializeField] private CanvasGroup optionsCanvasGroup;
    [SerializeField] private Button optionsBackButton;
    [SerializeField] private float panelAnimationDuration = 0.5f;

    [Header("Audio Settings UI")]
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Toggle sfxToggle;

    private bool optionsOpen = false;

    private void Start()
    {
        InitializeButtons();
        InitializeOptionsPanel();
        InitializeAudioSettingsUI();
        AnimateButtonsEntrance();
    }

    private void InitializeButtons()
    {
        foreach (var button in buttons)
        {
            button.Button.onClick.AddListener(() => OnButtonClick(button));
        }
    }

    private void InitializeOptionsPanel()
    {
        // Скрываем панель настроек в начале
        if (optionsPanel != null)
        {
            optionsPanel.localScale = Vector3.zero;
            optionsCanvasGroup.alpha = 0;
            optionsCanvasGroup.interactable = false;
            optionsCanvasGroup.blocksRaycasts = false;
        }

        // Настраиваем кнопку назад
        if (optionsBackButton != null)
        {
            optionsBackButton.onClick.AddListener(CloseOptions);
        }
    }

    private void InitializeAudioSettingsUI()
    {
        if (AudioManager.Instance != null)
        {
            var settings = AudioManager.Instance.GetCurrentSettings();
            
            musicToggle.isOn = settings.musicEnabled;
            sfxToggle.isOn = settings.sfxEnabled;
            
            musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
            sfxToggle.onValueChanged.AddListener(OnSFXToggleChanged);
        }
    }

    private void AnimateButtonsEntrance()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].CanvasGroup.alpha = 0;
            buttons[i].RectTransform.localScale = Vector3.zero;
            
            buttons[i].CanvasGroup.DOFade(1, 0.3f)
                .SetDelay(i * animationDelay);
            
            buttons[i].RectTransform.DOScale(1, 0.5f)
                .SetDelay(i * animationDelay)
                .SetEase(easeType);
        }
    }

    private void OnButtonClick(MenuButton clickedButton)
    {
        // Анимация нажатия
        clickedButton.RectTransform.DOScale(1.1f, 0.1f)
            .OnComplete(() =>
            {
                clickedButton.RectTransform.DOScale(1f, 0.1f);
                
                // Выполняем действие кнопки
                switch (clickedButton.ButtonType)
                {
                    case ButtonType.StartGame:
                        LoadGameScene();
                        break;
                    case ButtonType.Options:
                        OpenOptions();
                        break;
                    case ButtonType.Quit:
                        QuitGame();
                        break;
                }
            });
    }

    private void OpenOptions()
    {
        if (optionsOpen || optionsPanel == null) return;
        
        optionsOpen = true;
        
        // Анимация исчезновения основных кнопок
        foreach (var button in buttons)
        {
            button.CanvasGroup.interactable = false;
            button.CanvasGroup.DOFade(0, 0.3f);
            button.RectTransform.DOScale(0, 0.3f).SetEase(Ease.InBack);
        }

        // Анимация появления панели настроек
        optionsCanvasGroup.interactable = false;
        optionsCanvasGroup.blocksRaycasts = true;
        
        Sequence optionsSequence = DOTween.Sequence();
        optionsSequence
            .AppendCallback(() => optionsPanel.gameObject.SetActive(true))
            .Append(optionsCanvasGroup.DOFade(1, panelAnimationDuration))
            .Join(optionsPanel.DOScale(1, panelAnimationDuration).SetEase(Ease.OutBack))
            .OnComplete(() => {
                optionsCanvasGroup.interactable = true;
            });
    }

    private void CloseOptions()
    {
        if (!optionsOpen || optionsPanel == null) return;
        
        optionsOpen = false;
        
        // Анимация исчезновения панели настроек
        optionsCanvasGroup.interactable = false;
        
        Sequence closeSequence = DOTween.Sequence();
        closeSequence
            .Append(optionsCanvasGroup.DOFade(0, panelAnimationDuration * 0.7f))
            .Join(optionsPanel.DOScale(0, panelAnimationDuration * 0.7f).SetEase(Ease.InBack))
            .OnComplete(() => {
                optionsCanvasGroup.blocksRaycasts = false;
                optionsPanel.gameObject.SetActive(false);
                ShowMainMenuButtons();
            });
    }

    private void ShowMainMenuButtons()
    {
        // Анимация появления основных кнопок
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].CanvasGroup.alpha = 0;
            buttons[i].RectTransform.localScale = Vector3.zero;
            
            buttons[i].CanvasGroup.DOFade(1, 0.3f)
                .SetDelay(i * animationDelay);
            
            buttons[i].RectTransform.DOScale(1, 0.5f)
                .SetDelay(i * animationDelay)
                .SetEase(easeType)
                .OnComplete(() => {
                    foreach (var button in buttons)
                    {
                        button.CanvasGroup.interactable = true;
                    }
                });
        }
    }

    private void LoadGameScene()
    {
        // Анимация исчезновения перед загрузкой
        foreach (var button in buttons)
        {
            button.CanvasGroup.interactable = false;
            button.CanvasGroup.DOFade(0, 0.3f);
            button.RectTransform.DOScale(0, 0.3f).SetEase(Ease.InBack);
        }
        
        // Если открыты настройки - скрываем их тоже
        if (optionsOpen)
        {
            optionsCanvasGroup.DOFade(0, 0.3f);
            optionsPanel.DOScale(0, 0.3f).SetEase(Ease.InBack);
        }
        
        Invoke(nameof(LoadScene), 0.5f);
    }

    private void LoadScene() => SceneManager.LoadScene(1);

    #region Audio Settings Handlers

    private void OnMasterVolumeChanged(float value)
    {
        AudioManager.Instance.SetMasterVolume(value);
    }

    private void OnMusicVolumeChanged(float value)
    {
        AudioManager.Instance.SetMusicVolume(value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        AudioManager.Instance.SetSFXVolume(value);
    }

    private void OnMusicToggleChanged(bool enabled)
    {
        AudioManager.Instance.SetMusicEnabled(enabled);
    }

    private void OnSFXToggleChanged(bool enabled)
    {
        AudioManager.Instance.SetSFXEnabled(enabled);
    }
    

    #endregion

    private void QuitGame()
    {
        // Анимация перед выходом
        foreach (var button in buttons)
        {
            button.CanvasGroup.interactable = false;
        }

        Sequence quitSequence = DOTween.Sequence();
        quitSequence
            .AppendInterval(0.3f)
            .OnComplete(() => {
                Application.Quit();
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #endif
            });
    }

    private void OnDestroy()
    {
        // Отписываемся от всех событий
        foreach (var button in buttons)
        {
            button.Button.onClick.RemoveAllListeners();
        }

        if (optionsBackButton != null)
            optionsBackButton.onClick.RemoveAllListeners();

        if (musicToggle != null)
            musicToggle.onValueChanged.RemoveAllListeners();
        if (sfxToggle != null)
            sfxToggle.onValueChanged.RemoveAllListeners();
    }
}

[System.Serializable]
public class MenuButton
{
    public Button Button;
    public RectTransform RectTransform;
    public CanvasGroup CanvasGroup;
    public ButtonType ButtonType;
}

public enum ButtonType
{
    StartGame,
    Options,
    Quit
}