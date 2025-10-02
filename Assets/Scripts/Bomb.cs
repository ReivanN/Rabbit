using UnityEngine;
using Game.Grid;
using System.Collections;
using DG.Tweening;

public class Bomb : MonoBehaviour
{
    public static System.Action<Bomb, GridCell[]> OnExploded;
    
    [SerializeField] private float explosionRadius = 1f;
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private Transform bombModel;
    [SerializeField] private SpriteRenderer warningIndicator;
    [SerializeField] private float levitationHeight = 0.2f;
    [SerializeField] private float levitationDuration = 1f;
    
    private GridCell currentCell;
    private GridManager grid;
    private float lifetime;
    private Coroutine explosionCoroutine;
    private Sequence bombAnimationSequence;

    public void Init(GridCell cell, GridManager gridManager, float bombLifetime)
    {
        currentCell = cell;
        grid = gridManager;
        lifetime = bombLifetime;
        
        transform.SetParent(cell.transform, false);
        transform.localPosition = Vector3.zero;
        
        // Анимация появления через DoTween
        PlaySpawnAnimation();
        
        // Запускаем основную анимацию бомбы
        StartBombAnimation();
        
        explosionCoroutine = StartCoroutine(ExplosionTimer());
    }
    
    private void PlaySpawnAnimation()
    {
        if (bombModel != null)
        {
            // Анимация появления - прыжок из-под земли
            bombModel.localScale = Vector3.zero;
            bombModel.localPosition = new Vector3(0, -0.5f, 0);
            
            Sequence spawnSequence = DOTween.Sequence();
            spawnSequence
                .Append(bombModel.DOLocalMoveY(0, 0.5f).SetEase(Ease.OutBack))
                .Join(bombModel.DOScale(1f, 0.5f).SetEase(Ease.OutBack))
                .OnComplete(() => {
                    // После появления начинаем основную анимацию
                });
        }
    }
    
    private void StartBombAnimation()
    {
        if (bombModel != null)
        {
            bombAnimationSequence = DOTween.Sequence();
            
            // Основная анимация левитации
            bombAnimationSequence
                .Append(bombModel.DOLocalMoveY(levitationHeight, levitationDuration).SetEase(Ease.InOutQuad))
                .Append(bombModel.DOLocalMoveY(0, levitationDuration).SetEase(Ease.InOutQuad))
                .SetLoops(-1, LoopType.Yoyo);
                
            // Легкое вращение
            bombAnimationSequence
                .Join(bombModel.DOLocalRotate(new Vector3(0, 180, 0), levitationDuration * 2, RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental));
        }
        
        // Запускаем визуальное предупреждение о скором взрыве
        StartWarningAnimation();
    }
    
    private void StartWarningAnimation()
    {
        if (warningIndicator != null)
        {
            // Начинаем мигать за 50% времени до взрыва
            float warningStartTime = lifetime * 0.5f;
            
            DOVirtual.DelayedCall(warningStartTime, () => {
                // Мигание красным цветом
                warningIndicator.material.DOColor(Color.red, 0.2f)
                    .SetLoops(Mathf.RoundToInt((lifetime - warningStartTime) / 0.4f), LoopType.Yoyo)
                    .SetEase(Ease.InOutQuad);
            });
        }
        
        // Увеличение масштаба и ускорение пульсации перед взрывом
        if (bombModel != null)
        {
            float warningStartTime = lifetime * 0.5f;
            
            DOVirtual.DelayedCall(warningStartTime, () => {
                // Увеличиваем масштаб
                bombModel.DOScale(1.3f, lifetime - warningStartTime).SetEase(Ease.OutQuad);
                
                // Ускоряем анимацию левитации
                bombAnimationSequence?.Kill();
                bombAnimationSequence = DOTween.Sequence();
                bombAnimationSequence
                    .Append(bombModel.DOLocalMoveY(levitationHeight * 1.5f, 0.3f).SetEase(Ease.InOutQuad))
                    .Append(bombModel.DOLocalMoveY(0, 0.3f).SetEase(Ease.InOutQuad))
                    .SetLoops(-1, LoopType.Yoyo);
            });
        }
    }
    
    private IEnumerator ExplosionTimer()
    {
        yield return new WaitForSeconds(lifetime);
        Explode();
    }
    
    private void Explode()
    {
        // Находим все клетки в радиусе взрыва (4 клетки вокруг)
        GridCell[] affectedCells = GetAffectedCells();
        
        // Создаем эффекты взрыва во всех пораженных клетках
        CreateExplosionEffects(affectedCells);
        
        // Уведомляем о взрыве
        OnExploded?.Invoke(this, affectedCells);
        
        // Очищаем клетку
        if (currentCell != null)
        {
            currentCell.HasBomb = false;
            currentCell.SpawnedObject = null;
        }
        
        // Анимация исчезновения бомбы
        PlayDestroyAnimation();
    }
    
    private void CreateExplosionEffects(GridCell[] affectedCells)
    {
        if (explosionEffect == null) return;
        
        foreach (GridCell cell in affectedCells)
        {
            if (cell != null)
            {
                // Создаем эффект взрыва в центре каждой пораженной клетки
                Vector3 explosionPosition = cell.transform.position;
                Instantiate(explosionEffect, explosionPosition, Quaternion.identity);
            }
        }
    }
    
    private GridCell[] GetAffectedCells()
    {
        // Взрыв по 4 клеткам вокруг (крестом)
        if (grid == null || currentCell == null)
            return new GridCell[] { currentCell };

        Vector2Int currentCoord = currentCell.Coord;
        var affectedCells = new System.Collections.Generic.List<GridCell>();
        
        // Добавляем текущую клетку
        affectedCells.Add(currentCell);
        
        // Добавляем соседние клетки (вверх, вниз, влево, вправо)
        Vector2Int[] directions = {
            new Vector2Int(0, 1),   // Вверх
            new Vector2Int(0, -1),  // Вниз
            new Vector2Int(-1, 0),  // Влево
            new Vector2Int(1, 0)    // Вправо
        };
        
        foreach (var direction in directions)
        {
            Vector2Int neighborCoord = currentCoord + direction;
            GridCell neighborCell = grid.GetCell(neighborCoord);
            if (neighborCell != null)
            {
                affectedCells.Add(neighborCell);
            }
        }
        
        return affectedCells.ToArray();
    }
    
    private void PlayDestroyAnimation()
    {
        if (bombModel != null)
        {
            // Анимация быстрого увеличения и исчезновения
            Sequence destroySequence = DOTween.Sequence();
            destroySequence
                .Append(bombModel.DOScale(2f, 0.2f).SetEase(Ease.OutQuad))
                .Append(bombModel.DOScale(0f, 0.1f).SetEase(Ease.InQuad))
                .OnComplete(() => {
                    Destroy(gameObject);
                });
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        if (explosionCoroutine != null)
            StopCoroutine(explosionCoroutine);
            
        // Останавливаем все анимации DOTween
        bombAnimationSequence?.Kill();
        if (bombModel != null)
        {
            bombModel.DOKill();
        }
        if (warningIndicator != null)
        {
            warningIndicator.material.DOKill();
        }
    }
}