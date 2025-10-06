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

        private GridCell currentCell;
        private bool isMoving;
        private bool isDead;
        
        public GridCell CurrentCell => currentCell;

        private void Start()
        {
            if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
            Audio = GetComponent<AudioSource>();
            var startCell = gridManager.GetCell(startCoord) ?? gridManager.GetCell(new Vector2Int(0, 0));
            TeleportToCell(startCell);
        }

        private void Update()
        {
            if (isMoving) return;

            Vector2Int dir = Vector2Int.zero;
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) dir = Vector2Int.up;
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) dir = Vector2Int.down;
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) dir = Vector2Int.left;
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) dir = Vector2Int.right;

            if (dir != Vector2Int.zero) TryMove(dir);
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
            
            if (currentCell != null)
                currentCell.SetOccupied(false);
            
            var startCell = gridManager.GetCell(startCoord) ?? gridManager.GetCell(new Vector2Int(0, 0));
            TeleportToCell(startCell);
    
            if (spriteRoot != null) 
                spriteRoot.localPosition = Vector3.zero;
        }

        public void Die()
        {
            
        }
    }
}