using UnityEngine;
using System;
using System.Collections.Generic;

namespace Game.Grid
{
    [DisallowMultipleComponent]
    public class GridManager : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private GameObject cellPrefab;
        [SerializeField] private Vector2Int size = new Vector2Int(5, 5);
        [SerializeField] private Vector2 origin = Vector2.zero;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Transform cellsParent;

        private GridCell[,] cells;

        public int Width => size.x;
        public int Height => size.y;

        private void Awake()
        {
            if (cellPrefab == null) throw new Exception("GridManager: cellPrefab is null");
            BuildGrid();
        }

        public void BuildGrid()
        {
            // очистка старых объектов (если есть)
            if (cells != null)
            {
                foreach (var c in cells)
                    if (c != null)
                        if (Application.isPlaying) Destroy(c.gameObject); else DestroyImmediate(c.gameObject);
            }

            cells = new GridCell[size.x, size.y];

            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    Vector2Int coord = new Vector2Int(x, y);
                    Vector3 pos = CellWorldPosition(coord);
                    GameObject go = Instantiate(cellPrefab, pos, Quaternion.identity, cellsParent ? cellsParent : transform);
                    GridCell cell = go.GetComponent<GridCell>();
                    if (cell == null) cell = go.AddComponent<GridCell>();
                    cell.Init(coord);
                    cells[x, y] = cell;
                }
            }
        }

        public bool IsInside(Vector2Int coord)
            => coord.x >= 0 && coord.y >= 0 && coord.x < size.x && coord.y < size.y;

        public GridCell GetCell(Vector2Int coord)
            => IsInside(coord) ? cells[coord.x, coord.y] : null;

        public Vector3 CellWorldPosition(Vector2Int coord)
        {
            float offsetX = -(Width - 1) * cellSize * 0.5f;
            float offsetY = -(Height - 1) * cellSize * 0.5f;

            return (Vector3)origin + new Vector3(
                coord.x * cellSize + offsetX,
                coord.y * cellSize + offsetY,
                0f
            );
        }

        public IReadOnlyList<GridCell> GetOrthogonalNeighbors(Vector2Int coord)
        {
            var list = new List<GridCell>(4);
            var candidates = new[] {
                coord + Vector2Int.up,
                coord + Vector2Int.down,
                coord + Vector2Int.left,
                coord + Vector2Int.right
            };
            foreach (var c in candidates)
            {
                if (IsInside(c)) list.Add(GetCell(c));
            }
            return list;
        }
    }
}