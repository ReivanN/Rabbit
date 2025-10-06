using System.Collections.Generic;
using UnityEngine;
using Game.Grid;
using Cysharp.Threading.Tasks;
using System.Threading;
using YG;

namespace Game.Characters
{
    public class GameManager : MonoBehaviour
    {
        private int score;
        private bool isGameOver = false;
        private bool isGamePaused = false;

        [SerializeField] private RabbitController rabbit;
        [SerializeField] private UIManager uiManager;

        private List<MonoBehaviour> activeComponents = new List<MonoBehaviour>();
        private CancellationTokenSource gameTimerCancellationTokenSource;
        private float gameTime = 0f;

        public bool IsGameOver => isGameOver;
        public bool IsGamePaused => isGamePaused;
        
        private async UniTaskVoid PreloadExplosions()
        {
            var pool = ExplosionEffectPool.Instance;
            // Активируем и сразу деактивируем все эффекты для предзагрузки
            for (int i = 0; i < 5; i++)
            {
                var effect = pool.GetExplosionEffect(Vector3.zero);
                await UniTask.DelayFrame(1);
                pool.ReturnExplosionEffect(effect);
            }
        }
        private void Start()
        {
            FindAllActiveComponents();
            gameTimerCancellationTokenSource = new CancellationTokenSource();
            GameTimerAsync(gameTimerCancellationTokenSource.Token).Forget();
            uiManager.UpdateScore(score);
            PreloadExplosions().Forget();
        }

        private void OnEnable()
        {
            Carrot.OnCollected += HandleCarrotCollected;
            Bomb.OnExploded += HandleBombExploded;
        }

        private void OnDisable()
        {
            Carrot.OnCollected -= HandleCarrotCollected;
            Bomb.OnExploded -= HandleBombExploded;
        }

        private async UniTaskVoid GameTimerAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && !isGameOver)
            {
                if (!isGamePaused)
                {
                    gameTime += Time.deltaTime;
                    uiManager.UpdateTimer(gameTime);
                }
                
                // Ждем один кадр, учитывая паузу
                await UniTask.NextFrame(cancellationToken);
            }
        }

        private void FindAllActiveComponents()
        {
            activeComponents.Clear();
            
            var itemManagers = FindObjectsOfType<ItemManager>();
            activeComponents.AddRange(itemManagers);
            
            var bombs = FindObjectsOfType<Bomb>();
            activeComponents.AddRange(bombs);
            
            var movingObjects = FindObjectsOfType<MonoBehaviour>();
            foreach (var obj in movingObjects)
            {
                if (obj != this && obj != uiManager && obj is not RabbitController)
                {
                    activeComponents.Add(obj);
                }
            }
        }

        private void HandleCarrotCollected(Carrot carrot)
        {
            if (isGameOver || isGamePaused) return;
            
            score++;
            uiManager.UpdateScore(score);
            Debug.Log($"Score: {score}");
        }

        private void HandleBombExploded(Bomb bomb, GridCell[] affected)
        {
            if (isGameOver) return;

            foreach (var cell in affected)
            {
                if (cell == null) continue;
                if (cell == rabbit.CurrentCell)
                {
                    Debug.Log("Game Over!");
                    GameOver();
                    return;
                }
            }
        }

        private void GameOver()
        {
            isGameOver = true;
            
            // Отменяем таймер игры
            gameTimerCancellationTokenSource?.Cancel();
            
            //DisableAllComponents();
            rabbit.enabled = false;
            uiManager.ShowGameOver(score);
            YG2.InterstitialAdvShow();
            Debug.Log("Game Over!  Final Score: " + score);
        }

        public void SetGamePaused(bool paused)
        {
            isGamePaused = paused;
            
            if (paused)
            {
                //DisableAllComponents();
                rabbit.enabled = false;
                
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = 1f;
                EnableAllComponents();
                rabbit.enabled = true;
            }
        }

        /*private void DisableAllComponents()
        {
            foreach (var component in activeComponents)
            {
                if (component != null)
                {
                    component.enabled = false;
                }
            }
        }*/

        private void EnableAllComponents()
        {
            foreach (var component in activeComponents)
            {
                if (component != null)
                {
                    component.enabled = true;
                }
            }
        }

        public void RestartGame()
        {
            isGameOver = false;
            isGamePaused = false;
            Time.timeScale = 1f;
    
            gameTimerCancellationTokenSource?.Cancel();
            gameTimerCancellationTokenSource?.Dispose();
            gameTimerCancellationTokenSource = new CancellationTokenSource();
    
            EnableAllComponents();
            uiManager.HideGameOver();
            uiManager.HidePauseMenu();
    
            if (DifficultyManager.Instance != null)
                DifficultyManager.Instance.ResetDifficulty();

            score = 0;
            gameTime = 0f;
            uiManager.UpdateScore(score);
            uiManager.UpdateTimer(gameTime);

            var itemManager = FindObjectOfType<ItemManager>();
            if (itemManager != null)
                itemManager.ResetGame();

            // Добавьте сброс сетки
            var gridManager = FindObjectOfType<GridManager>();
            if (gridManager != null)
                gridManager.ResetGrid();

            if (rabbit != null)
            {
                rabbit.Respawn();
                rabbit.enabled = true;
            }
    
            FindAllActiveComponents();
            GameTimerAsync(gameTimerCancellationTokenSource.Token).Forget();

            Debug.Log("Игра перезапущена!");
        }

        private void Update()
        {
            if (isGameOver && Input.GetKeyDown(KeyCode.R))
            {
                RestartGame();
            }
        }

        private void OnDestroy()
        {
            gameTimerCancellationTokenSource?.Cancel();
            gameTimerCancellationTokenSource?.Dispose();
        }
    }
}