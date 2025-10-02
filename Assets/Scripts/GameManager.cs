using System.Collections;
using UnityEngine;
using Game.Grid;
using System.Collections.Generic;

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
        private Coroutine timerCoroutine;
        private float gameTime = 0f;

        public bool IsGameOver => isGameOver;
        public bool IsGamePaused => isGamePaused;

        private void Start()
        {
            FindAllActiveComponents();
            timerCoroutine = StartCoroutine(GameTimer());
            uiManager.UpdateScore(score);
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

        private IEnumerator GameTimer()
        {
            while (!isGameOver)
            {
                if (!isGamePaused)
                {
                    gameTime += Time.deltaTime;
                    uiManager.UpdateTimer(gameTime);
                }
                yield return null;
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
            
            if (timerCoroutine != null)
                StopCoroutine(timerCoroutine);
            
            DisableAllComponents();
            rabbit.enabled = false;
            uiManager.ShowGameOver(score);
            
            Debug.Log("Game Over! Final Score: " + score);
        }

        public void SetGamePaused(bool paused)
        {
            isGamePaused = paused;
            
            if (paused)
            {
                DisableAllComponents();
                rabbit.enabled = false;
            }
            else
            {
                EnableAllComponents();
                rabbit.enabled = true;
            }
        }

        private void DisableAllComponents()
        {
            foreach (var component in activeComponents)
            {
                if (component != null)
                {
                    component.enabled = false;
                }
            }
        }

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
            
            EnableAllComponents();
            uiManager.HideGameOver();
            uiManager.HidePauseMenu();
            
            if (DifficultyManager.Instance != null)
                DifficultyManager.Instance.ResetDifficulty();
    
            score = 0;
            uiManager.UpdateScore(score);
    
            var itemManager = FindObjectOfType<ItemManager>();
            if (itemManager != null)
                itemManager.ResetGame();
    
            if (rabbit != null)
            {
                rabbit.Respawn();
                rabbit.enabled = true;
            }
            
            FindAllActiveComponents();
            if (timerCoroutine != null)
                StopCoroutine(timerCoroutine);
            timerCoroutine = StartCoroutine(GameTimer());
    
            Debug.Log("Игра перезапущена!");
        }

        private void Update()
        {
            if (isGameOver && Input.GetKeyDown(KeyCode.R))
            {
                RestartGame();
            }
        }
    }
}