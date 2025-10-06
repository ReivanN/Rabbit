using UnityEngine;
using System.Collections;
using Game.Grid;

namespace Game.Characters
{
    [DisallowMultipleComponent]
    public class RabbitController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private Transform spriteRoot;
        [SerializeField] private Animator animator;

        [Header("Start")]
        [SerializeField] private Vector2Int startCoord = new Vector2Int(0, 0);

        [Header("Movement")]
        [SerializeField, Min(0.01f)] private float moveDuration = 0.25f;
        [SerializeField, Min(0f)] private float jumpHeight = 0.35f;
        [SerializeField] private AnimationCurve jumpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Sound")]
        [SerializeField] private AudioSource Audio;
        [SerializeField] private AudioClip jumpSound;
        [SerializeField] private AudioClip pickUpCarrotSound;

        // Параметры аниматора
        private static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");
        private static readonly int JumpSpeedHash = Animator.StringToHash("JumpSpeed");

        private GridCell currentCell;
        private bool isMoving;
        private bool isDead;
        
        public GridCell CurrentCell => currentCell;

        private void Start()
        {
            if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (Audio == null) Audio = GetComponent<AudioSource>();
            
            var startCell = gridManager.GetCell(startCoord) ?? gridManager.GetCell(new Vector2Int(0, 0));
            TeleportToCell(startCell);
        }

        private void Update()
        {
            if (isMoving || isDead) return;

            Vector2Int dir = Vector2Int.zero;
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) dir = Vector2Int.up;
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) dir = Vector2Int.down;
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) dir = Vector2Int.left;
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) dir = Vector2Int.right;

            if (dir != Vector2Int.zero) 
            {
                SetSpriteDirection(dir);
                TryMove(dir);
            }
        }

        private void SetSpriteDirection(Vector2Int direction)
        {
            if (spriteRoot == null) return;

            // Поворачиваем спрайт в направлении движения
            if (direction.x != 0)
            {
                float scaleX = Mathf.Abs(spriteRoot.localScale.x) * (direction.x > 0 ? 1 : -1);
                spriteRoot.localScale = new Vector3(scaleX, spriteRoot.localScale.y, spriteRoot.localScale.z);
            }
        }

        private void TryMove(Vector2Int dir)
        {
            var targetCoord = currentCell.Coord + dir;
            if (!gridManager.IsInside(targetCoord)) return;

            var targetCell = gridManager.GetCell(targetCoord);
            if (targetCell == null || !targetCell.IsWalkable) return;
            
            if (targetCell.HasBomb) return;
            
            StartCoroutine(MoveToCell(targetCell));
        }

        private IEnumerator MoveToCell(GridCell targetCell)
        {
            isMoving = true;
            
            // Запускаем анимацию прыжка
            if (animator != null)
            {
                animator.SetBool(IsJumpingHash, true);
                animator.SetFloat(JumpSpeedHash, 1f / moveDuration);
            }

            AudioManager.Instance.PlaySFX(jumpSound, Audio);
            
            Vector3 startWorld = transform.position;
            Vector3 endWorld = gridManager.CellWorldPosition(targetCell.Coord);

            currentCell.SetOccupied(false);
            targetCell.SetOccupied(true);

            float elapsed = 0f;
            while (elapsed < moveDuration)
            {
                float t = elapsed / moveDuration;
                transform.position = Vector3.Lerp(startWorld, endWorld, t);

                if (spriteRoot != null)
                {
                    float curveT = jumpCurve.Evaluate(t);
                    float vertical = Mathf.Sin(Mathf.PI * curveT) * jumpHeight;
                    spriteRoot.localPosition = new Vector3(0f, vertical, 0f);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = endWorld;
            if (spriteRoot != null) spriteRoot.localPosition = Vector3.zero;

            // Завершаем анимацию прыжка
            if (animator != null)
            {
                animator.SetBool(IsJumpingHash, false);
            }

            currentCell = targetCell;
            isMoving = false;
            
            // Проверяем сбор морковки при движении
            CheckForCarrotCollection(targetCell);
        }

        private void CheckForCarrotCollection(GridCell cell)
        {
            // Ищем морковку на этой клетке через дочерние объекты
            Carrot carrot = cell.GetComponentInChildren<Carrot>();
            if (carrot != null && !carrot.IsCollected)
            {
                carrot.Collect();
                AudioManager.Instance.PlaySFX(pickUpCarrotSound, Audio);
            }
        }

        private void TeleportToCell(GridCell cell)
        {
            if (cell == null) throw new System.Exception("RabbitController: start cell is null");
            transform.position = gridManager.CellWorldPosition(cell.Coord);
            currentCell = cell;
            cell.SetOccupied(true);
            if (spriteRoot != null) spriteRoot.localPosition = Vector3.zero;
        }
        
        public void Respawn()
        {
            StopAllCoroutines();
            isMoving = false;
            
            // Сбрасываем анимацию прыжка
            if (animator != null)
            {
                animator.SetBool(IsJumpingHash, false);
            }
            
            if (currentCell != null)
                currentCell.SetOccupied(false);
            
            var startCell = gridManager.GetCell(startCoord) ?? gridManager.GetCell(new Vector2Int(0, 0));
            TeleportToCell(startCell);
    
            if (spriteRoot != null) 
                spriteRoot.localPosition = Vector3.zero;
        }

        public void Die()
        {
            isDead = true;
            // Можно добавить анимацию смерти здесь
            if (animator != null)
            {
                // Например: animator.SetTrigger("Die");
            }
        }
    }
}