using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private MenuButton[] buttons;
    [SerializeField] private float animationDelay = 0.2f;
    [SerializeField] private Ease easeType = Ease.OutBack;

    private void Start()
    {
        InitializeButtons();
        AnimateButtonsEntrance();
    }

    private void InitializeButtons()
    {
        foreach (var button in buttons)
        {
            button.Button.onClick.AddListener(() => OnButtonClick(button));
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

    private void LoadGameScene()
    {
        // Анимация исчезновения перед загрузкой
        foreach (var button in buttons)
        {
            button.CanvasGroup.DOFade(0, 0.3f);
            button.RectTransform.DOScale(0, 0.3f).SetEase(Ease.InBack);
        }
        
        Invoke(nameof(LoadScene), 0.5f);
    }

    private void LoadScene() => SceneManager.LoadScene(1);

    private void OpenOptions()
    {
        // Реализация открытия настроек
        Debug.Log("Opening options...");
    }

    private void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    private void OnDestroy()
    {
        foreach (var button in buttons)
        {
            button.Button.onClick.RemoveAllListeners();
        }
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