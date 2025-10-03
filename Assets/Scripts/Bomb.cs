using UnityEngine;
using Game.Grid;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;

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
    private CancellationTokenSource explosionCancellationTokenSource;
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
        
        // Запускаем таймер взрыва через UniTask
        explosionCancellationTokenSource = new CancellationTokenSource();
        ExplosionTimerAsync(explosionCancellationTokenSource.Token).Forget();
    }
    
    private async UniTaskVoid ExplosionTimerAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Ждем указанное время, учитывая паузу
            await UniTask.Delay((int)(lifetime * 1000), 
                cancellationToken: cancellationToken,
                ignoreTimeScale: false);
            
            if (!cancellationToken.IsCancellationRequested)
            {
                Explode();
            }
        }
        catch (System.OperationCanceledException)
        {
            // Таск был отменен - это нормально
        }
    }
    
    private void PlaySpawnAnimation()
    {
        if (bombModel != null)
        {
            bombModel.localScale = Vector3.zero;
            bombModel.localPosition = new Vector3(0, -0.5f, 0);
            
            Sequence spawnSequence = DOTween.Sequence();
            spawnSequence
                .Append(bombModel.DOLocalMoveY(0, 0.5f).SetEase(Ease.OutBack))
                .Join(bombModel.DOScale(1f, 0.5f).SetEase(Ease.OutBack));
        }
    }
    
    private void StartBombAnimation()
    {
        if (bombModel != null)
        {
            bombAnimationSequence = DOTween.Sequence();
            
            bombAnimationSequence
                .Append(bombModel.DOLocalMoveY(levitationHeight, levitationDuration).SetEase(Ease.InOutQuad))
                .Append(bombModel.DOLocalMoveY(0, levitationDuration).SetEase(Ease.InOutQuad))
                .SetLoops(-1, LoopType.Yoyo);
                
            bombAnimationSequence
                .Join(bombModel.DOLocalRotate(new Vector3(0, 180, 0), levitationDuration * 2, RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental));
        }
        
        StartWarningAnimation();
    }
    
    private void StartWarningAnimation()
    {
        if (warningIndicator != null)
        {
            float warningStartTime = lifetime * 0.5f;
            
            // Используем UniTask для отложенного вызова
            StartWarningAsync(warningStartTime).Forget();
        }
        
        if (bombModel != null)
        {
            float warningStartTime = lifetime * 0.5f;
            StartModelWarningAsync(warningStartTime).Forget();
        }
    }
    
    private async UniTaskVoid StartWarningAsync(float warningStartTime)
    {
        await UniTask.Delay((int)(warningStartTime * 1000), ignoreTimeScale: false);
        
        if (warningIndicator != null)
        {
            warningIndicator.material.DOColor(Color.red, 0.2f)
                .SetLoops(Mathf.RoundToInt((lifetime - warningStartTime) / 0.4f), LoopType.Yoyo)
                .SetEase(Ease.InOutQuad);
        }
    }
    
    private async UniTaskVoid StartModelWarningAsync(float warningStartTime)
    {
        await UniTask.Delay((int)(warningStartTime * 1000), ignoreTimeScale: false);
        
        if (bombModel != null)
        {
            bombModel.DOScale(1.3f, lifetime - warningStartTime).SetEase(Ease.OutQuad);
            
            bombAnimationSequence?.Kill();
            bombAnimationSequence = DOTween.Sequence();
            bombAnimationSequence
                .Append(bombModel.DOLocalMoveY(levitationHeight * 1.5f, 0.3f).SetEase(Ease.InOutQuad))
                .Append(bombModel.DOLocalMoveY(0, 0.3f).SetEase(Ease.InOutQuad))
                .SetLoops(-1, LoopType.Yoyo);
        }
    }
    
    private void Explode()
    {
        GridCell[] affectedCells = GetAffectedCells();
        CreateExplosionEffects(affectedCells);
        OnExploded?.Invoke(this, affectedCells);
        
        if (currentCell != null)
        {
            currentCell.HasBomb = false;
            currentCell.SpawnedObject = null;
        }
        
        PlayDestroyAnimation();
    }
    
    private void CreateExplosionEffects(GridCell[] affectedCells)
    {
        if (explosionEffect == null) return;
        
        foreach (GridCell cell in affectedCells)
        {
            if (cell != null)
            {
                Vector3 explosionPosition = cell.transform.position;
                Instantiate(explosionEffect, explosionPosition, Quaternion.identity);
            }
        }
    }
    
    private GridCell[] GetAffectedCells()
    {
        if (grid == null || currentCell == null)
            return new GridCell[] { currentCell };

        Vector2Int currentCoord = currentCell.Coord;
        var affectedCells = new System.Collections.Generic.List<GridCell>();
        
        affectedCells.Add(currentCell);
        
        Vector2Int[] directions = {
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0)
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
        // Отменяем все UniTask'и
        explosionCancellationTokenSource?.Cancel();
        explosionCancellationTokenSource?.Dispose();
            
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