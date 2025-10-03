using UnityEngine;
using Game.Grid;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;

public class Carrot : MonoBehaviour
{
    public static System.Action<Carrot> OnCollected;
    public static System.Action<Carrot> OnExpired;
    
    [SerializeField] private float lifetime = 10f;
    [SerializeField] private Transform carrotModel;
    [SerializeField] private float levitationHeight = 0.3f;
    [SerializeField] private float levitationDuration = 1f;
    
    private GridCell currentCell;
    private bool isCollected = false;
    private CancellationTokenSource lifetimeCancellationTokenSource;
    private Sequence carrotAnimationSequence;

    public bool IsCollected => isCollected;

    public void Init(GridCell cell)
    {
        currentCell = cell;
        transform.SetParent(cell.transform, false);
        transform.localPosition = Vector3.zero;
        
        PlaySpawnAnimation();
            
        // Запускаем таймер жизни через UniTask
        lifetimeCancellationTokenSource = new CancellationTokenSource();
        LifetimeTimerAsync(lifetimeCancellationTokenSource.Token).Forget();
    }
    
    private async UniTaskVoid LifetimeTimerAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Ждем время жизни, учитывая паузу
            await UniTask.Delay((int)(lifetime * 1000), 
                cancellationToken: cancellationToken,
                ignoreTimeScale: false);
            
            if (!cancellationToken.IsCancellationRequested && !isCollected)
            {
                Expire();
            }
        }
        catch (System.OperationCanceledException)
        {
            // Таск был отменен - это нормально
        }
    }
    
    private void PlaySpawnAnimation()
    {
        if (carrotModel != null)
        {
            carrotModel.localScale = Vector3.zero;
            carrotModel.localPosition = new Vector3(0, -0.3f, 0);
            
            Sequence spawnSequence = DOTween.Sequence();
            spawnSequence
                .Append(carrotModel.DOLocalMoveY(0, 0.5f).SetEase(Ease.OutBack))
                .Join(carrotModel.DOScale(1f, 0.5f).SetEase(Ease.OutBack))
                .OnComplete(() => {
                    StartLevitationAnimation();
                });
        }
        else
        {
            StartLevitationAnimation();
        }
    }
    
    private void StartLevitationAnimation()
    {
        if (carrotModel != null)
        {
            carrotAnimationSequence = DOTween.Sequence();
            
            carrotAnimationSequence
                .Append(carrotModel.DOLocalMoveY(levitationHeight, levitationDuration).SetEase(Ease.InOutQuad))
                .Append(carrotModel.DOLocalMoveY(0, levitationDuration).SetEase(Ease.InOutQuad))
                .SetLoops(-1, LoopType.Yoyo);
                
            carrotAnimationSequence
                .Join(carrotModel.DOLocalRotate(new Vector3(0, 360, 0), levitationDuration * 4, RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart));
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;
        
        if (other.CompareTag("Player"))
        {
            Collect();
        }
    }
    
    public void Collect()
    {
        if (isCollected) return;
        
        isCollected = true;
        
        // Отменяем таймер жизни
        lifetimeCancellationTokenSource?.Cancel();
        
        PlayCollectAnimation();
    }
    
    private void PlayCollectAnimation()
    {
        if (carrotModel != null)
        {
            carrotAnimationSequence?.Kill();
        
            Sequence collectSequence = DOTween.Sequence();
            collectSequence
                .Append(carrotModel.DOLocalMoveY(1f, 0.5f).SetEase(Ease.OutQuad))
                .Join(carrotModel.DOScale(1.3f, 0.3f).SetEase(Ease.OutBack))
                .Join(carrotModel.DOLocalRotate(new Vector3(0, 720, 0), 0.5f, RotateMode.LocalAxisAdd))
                .Join(carrotModel.DOScale(0.5f, 0.5f).SetEase(Ease.InQuad))
                .Append(carrotModel.DOScale(0f, 0.2f).SetEase(Ease.InBack))
                .OnComplete(() => {
                    OnCollected?.Invoke(this);
                    DestroyImmediate();
                });
        }
        else
        {
            OnCollected?.Invoke(this);
            DestroyImmediate();
        }
    }
    
    private void DestroyImmediate()
    {
        if (currentCell != null)
        {
            currentCell.SpawnedObject = null;
            currentCell.HasCarrot = false;
        }
        
        Destroy(gameObject);
    }
    
    private void Expire()
    {
        if (isCollected) return;
        
        PlayExpireAnimation();
    }
    
    private void PlayExpireAnimation()
    {
        if (carrotModel != null)
        {
            carrotAnimationSequence?.Kill();
            
            Sequence expireSequence = DOTween.Sequence();
            expireSequence
                .Append(carrotModel.DOLocalMoveY(-0.5f, 0.8f).SetEase(Ease.InQuad))
                .Join(carrotModel.DOScale(0f, 0.8f).SetEase(Ease.InBack))
                .OnComplete(() => {
                    OnExpired?.Invoke(this);
                    DestroyImmediate();
                });
        }
        else
        {
            OnExpired?.Invoke(this);
            DestroyImmediate();
        }
    }
    
    private void OnDestroy()
    {
        // Отменяем все UniTask'и
        lifetimeCancellationTokenSource?.Cancel();
        lifetimeCancellationTokenSource?.Dispose();
        
        carrotAnimationSequence?.Kill();
        if (carrotModel != null)
        {
            carrotModel.DOKill();
        }
    }
}