using UnityEngine;
using System;

namespace Game.Grid
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class GridCell : MonoBehaviour
    {
        public Vector2Int Coord { get; private set; }
        public bool IsWalkable { get; private set; } = true;
        public bool IsOccupied { get; private set; } = false;
        
        public bool HasCarrot;
        public bool HasBomb;
        public GameObject SpawnedObject;
        
        public event Action<GridCell> OnEnter;
        public event Action<GridCell> OnExit;
        
        public void Init(Vector2Int coord)
        {
            Coord = coord;
            name = $"Cell_{coord.x}_{coord.y}";
        }

        public void SetOccupied(bool occupied)
        {
            if (IsOccupied == occupied) return;
            IsOccupied = occupied;
            if (occupied) OnEnter?.Invoke(this); else OnExit?.Invoke(this);
            UpdateVisual();
        }

        public void SetWalkable(bool walkable)
        {
            IsWalkable = walkable;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // простая визуальная подсветка: серый для непроходимых, зелёный — занятый
                if (!IsWalkable) sr.color = Color.gray;
                else if (IsOccupied) sr.color = new Color(0.8f, 1f, 0.8f);
                else sr.color = Color.white;
            }
        }
    }
}