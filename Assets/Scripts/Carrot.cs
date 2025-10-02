using UnityEngine;
using Game.Grid;
using System.Collections;
using DG.Tweening;

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
    private Coroutine lifetimeCoroutine;
    private Sequence carrotAnimationSequence;

    public bool IsCollected => isCollected;

    public void Init(GridCell cell)
    {
        currentCell = cell;
        transform.SetParent(cell.transform, false);
        transform.localPosition = Vector3.zero;
        
        // Анимация появления через DoTween
        PlaySpawnAnimation();
            
        lifetimeCoroutine = StartCoroutine(LifetimeTimer());
    }
    
    private void PlaySpawnAnimation()
    {
        if (carrotModel != null)
        {
            // Анимация появления - рост из-под земли
            carrotModel.localScale = Vector3.zero;
            carrotModel.localPosition = new Vector3(0, -0.3f, 0);
            
            Sequence spawnSequence = DOTween.Sequence();
            spawnSequence
                .Append(carrotModel.DOLocalMoveY(0, 0.5f).SetEase(Ease.OutBack))
                .Join(carrotModel.DOScale(1f, 0.5f).SetEase(Ease.OutBack))
                .OnComplete(() => {
                    // После появления начинаем анимацию левитации
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
            
            // Основная анимация левитации
            carrotAnimationSequence
                .Append(carrotModel.DOLocalMoveY(levitationHeight, levitationDuration).SetEase(Ease.InOutQuad))
                .Append(carrotModel.DOLocalMoveY(0, levitationDuration).SetEase(Ease.InOutQuad))
                .SetLoops(-1, LoopType.Yoyo);
                
            // Легкое вращение
            carrotAnimationSequence
                .Join(carrotModel.DOLocalRotate(new Vector3(0, 360, 0), levitationDuration * 4, RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart));
        }
    }
    
    private IEnumerator LifetimeTimer()
    {
        yield return new WaitForSeconds(lifetime);
        
        if (!isCollected)
        {
            Expire();
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
        
        // Останавливаем таймер
        if (lifetimeCoroutine != null)
            StopCoroutine(lifetimeCoroutine);
        
        // Анимация сбора через DoTween
        PlayCollectAnimation();
    }
    
    private void PlayCollectAnimation()
    {
        if (carrotModel != null)
        {
            // Останавливаем основную анимацию
            carrotAnimationSequence?.Kill();
        
            // Анимация сбора - прыжок и вращение
            Sequence collectSequence = DOTween.Sequence();
            collectSequence
                .Append(carrotModel.DOLocalMoveY(1f, 0.5f).SetEase(Ease.OutQuad))
                .Join(carrotModel.DOScale(1.3f, 0.3f).SetEase(Ease.OutBack))
                .Join(carrotModel.DOLocalRotate(new Vector3(0, 720, 0), 0.5f, RotateMode.LocalAxisAdd))
                // Добавляем постепенное уменьшение во время вращения
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
        
        // Анимация исчезновения через DoTween
        PlayExpireAnimation();
    }
    
    private void PlayExpireAnimation()
    {
        if (carrotModel != null)
        {
            // Останавливаем основную анимацию
            carrotAnimationSequence?.Kill();
            
            // Анимация увядания
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
        // Останавливаем все анимации DOTween при уничтожении
        carrotAnimationSequence?.Kill();
        if (carrotModel != null)
        {
            carrotModel.DOKill();
        }
    }
}