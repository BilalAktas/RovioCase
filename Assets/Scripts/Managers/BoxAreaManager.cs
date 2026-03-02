using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class BoxAreaManager : MonoBehaviour
    {
        [Header("Level")]
        private LevelDesignData _currentLevelDesignData;
        [Header("Grid")]
        private BoxGridNode[,] _grid;
        private Vector2Int _gridSize;
        private readonly List<GameObject> _spawnedBoxes = new();
        private readonly List<GameObject> _spawnedObstacles = new();
        private readonly List<GameObject> _spawnedNodes = new();
        private static readonly Vector2Int[] ClickableCheckDirs =
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, -1),
        };
        
        private void Awake()
        {
            EventBus.Subscribe<OnLevelSpawnedEvent>(OnLevelSpawned);
            EventBus.Subscribe<OnBoxMovedFromBoxAreaEvent>(OnBoxMoved);
        }

        private void OnLevelSpawned(OnLevelSpawnedEvent data)
        {
            foreach (var box in _spawnedBoxes)
                Destroy(box);
            _spawnedBoxes.Clear();
            
            foreach (var obstacle in _spawnedObstacles)
                Destroy(obstacle);
            _spawnedObstacles.Clear();
            
            foreach (var node in _spawnedNodes)
                Destroy(node);
            _spawnedNodes.Clear();
            
            _currentLevelDesignData = LevelManager.Instance.CurrentLevelDesignData;
            _gridSize = new Vector2Int(_currentLevelDesignData.BoxGridSize.x, _currentLevelDesignData.BoxGridSize.y);
            CreateGrid();
            SetAllBoxClickableStatus(true);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OnLevelSpawnedEvent>(OnLevelSpawned);
            EventBus.Unsubscribe<OnBoxMovedFromBoxAreaEvent>(OnBoxMoved);
        }

        private void CreateGrid()
        {
            _grid = Helpers.CreateGrid(
                _gridSize,
                origin: Vector3.zero,
                nodeSize: _currentLevelDesignData.BoxNodeSize,
                worldOffset: _currentLevelDesignData.WorldPositionOffet,
                createNode: (worldPos, gridPos) =>
                {
                    var clone = Instantiate(_currentLevelDesignData.NodePrefab, transform);
                    clone.transform.localPosition = worldPos;
                    _spawnedNodes.Add(clone);

                    return new BoxGridNode(worldPos, gridPos);
                },
                afterCreate: (node, worldPos, gridPos) =>
                {
                    var color = LevelManager.Instance.CurrentLevelDesignData.GetBox(gridPos.x, gridPos.y);
                    if (color == BoxColor.Empty)
                    {
                        node.SetObstacle();
                        var obstacleClone = Instantiate(_currentLevelDesignData.ObstaclePrefab, transform);
                        obstacleClone.transform.localPosition = worldPos;
                        _spawnedObstacles.Add(obstacleClone);
                    }
                    else
                    {
                        var clone = Instantiate(_currentLevelDesignData.BoxPrefab, transform);
                        var rp = _currentLevelDesignData.GetPropertyByColor(color);
                        var box = clone.GetComponent<Box>();
                        box.enabled = true;
                        clone.transform.localPosition = worldPos;
                        box.SetProperties(rp, node, LevelManager.Instance.CurrentLevelDesignData.GetBoxCapacity(gridPos.x,gridPos.y));
                        node.SetBox(box);
                        _spawnedBoxes.Add(box.gameObject);
                    }
                }
            );
        }

        

        private void OnBoxMoved(OnBoxMovedFromBoxAreaEvent data)
        {
            SetAllBoxClickableStatus(false);
        }
        
        private void SetAllBoxClickableStatus(bool gameStart)
        {
            for (var x = 0; x < _gridSize.x; x++)
            for (var y = 0; y < _gridSize.y; y++)
            {
                var node = _grid[x, y];
                var box = node.CurrentBox;
                if (!box) continue;

                var clickable = IsPositionFirstOnGrid(node.GridPosition) || HasFreeNeighbor(node.GridPosition);
                box.SetClickableStatus(clickable, gameStart);
            }
        }
        
        private bool HasFreeNeighbor(Vector2Int pos)
        {
            foreach (var dir in ClickableCheckDirs)
            {
                var s = pos + dir;
                if (!IsPositionInBound(s)) continue;

                var neighbor = _grid[s.x, s.y];
                if (neighbor.IsObstacle) continue;
                if (neighbor.CurrentBox) continue;

                return true;
            }

            return false;
        }

        private bool IsPositionInBound(Vector2Int pos)
            => pos.x >= 0 && pos.x < _gridSize.x && pos.y >= 0 && pos.y < _gridSize.y;

        private bool IsPositionFirstOnGrid(Vector2Int pos)
            => pos.x >= 0 && pos.x < _gridSize.x && pos.y == _gridSize.y - 1;
    }
}