using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core
{
    public class BoxAreaManager : MonoBehaviour
    {
        private LevelDesignData _currentLevelDesignData;
        [Header("Grid")]
        private BoxGridNode[,] _grid;
        private Vector2Int _gridSize;
        private Vector3 _bottomLeft;
        private readonly List<GameObject> _spawnedBoxes = new();
        private readonly List<GameObject> _spawnedObstacles = new();
        
        
        private void Awake()
        {
            EventBus.Subscribe<OnLevelSpawnedEvent>(OnLevelSpawned);
            EventBus.Subscribe<OnBoxMovedFromBoxAreaEvent>(OnBoxMoved);
        }

        private void OnLevelSpawned(OnLevelSpawnedEvent data)
        {
            foreach (var box in _spawnedBoxes.ToArray())
                Destroy(box);
            _spawnedBoxes.Clear();
            
            foreach (var obstacle in _spawnedObstacles.ToArray())
                Destroy(obstacle);
            _spawnedObstacles.Clear();
            
            _currentLevelDesignData = LevelManager.Instance.CurrentLevelDesignData;
            _gridSize = new Vector2Int(_currentLevelDesignData.BoxGridSize.x, _currentLevelDesignData.BoxGridSize.y);
            CreateGrid();
            SpawnBox();
            SetAllBoxClickableStatus(true);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OnLevelSpawnedEvent>(OnLevelSpawned);
            EventBus.Unsubscribe<OnBoxMovedFromBoxAreaEvent>(OnBoxMoved);
        }

        private void CreateGrid()
        {
            var origin = Vector2.zero;
            _grid = new BoxGridNode[_gridSize.x, _gridSize.y];
            var totalSize = new Vector2(_gridSize.x * _currentLevelDesignData.BoxNodeSize.x, _gridSize.y * _currentLevelDesignData.BoxNodeSize.y);
            _bottomLeft = origin - totalSize / 2 + _currentLevelDesignData.BoxNodeSize / 2;
            for (var x = 0; x < _gridSize.x; x++)
            {
                for (var y = 0; y < _gridSize.y; y++)
                {
                    var worldPosition = _bottomLeft + new Vector3(x * _currentLevelDesignData.BoxNodeSize.x, 0, y * _currentLevelDesignData.BoxNodeSize.y);
                    worldPosition.y = 0;
                    worldPosition.z += _currentLevelDesignData.WorldPositionOffet.z;
                    var gridPosition = new Vector2Int(x, y);
                    var clone = Instantiate(_currentLevelDesignData.NodePrefab, transform);
                    var node = new BoxGridNode(worldPosition, gridPosition);
                    _grid[gridPosition.x, gridPosition.y] = node;
                    clone.transform.localPosition = worldPosition;
                }
            }
        }

        private void SpawnBox()
        {
            for (var x = 0; x < _gridSize.x; x++)
            {
                for (var y = 0; y < _gridSize.y; y++)
                {
                    var color = LevelManager.Instance.CurrentLevelDesignData.GetBox(x, y);
                    var node = _grid[x, y];
                    if (color == BoxColor.Empty)
                    {
                        node.SetObstacle();
                        var obstacleClone = Instantiate(_currentLevelDesignData.ObstaclePrefab, transform);
                        obstacleClone.transform.localPosition = node.WorldPosition;
                        _spawnedObstacles.Add(obstacleClone);
                        continue;
                    }
                    
                    var clone = Instantiate(_currentLevelDesignData.BoxPrefab, transform);
                    var rp = _currentLevelDesignData.BoxProperties.FirstOrDefault(x => x.BoxColor == color);
                    var box = clone.GetComponent<Box>();
                    box.enabled = true;
                    clone.transform.localPosition = node.WorldPosition;
                    box.SetProperties(rp, node, LevelManager.Instance.CurrentLevelDesignData.GetBoxCapacity(x,y));
                    node.SetBox(box);
                    
                    _spawnedBoxes.Add(box.gameObject);
                }
            }
        }

        private void OnBoxMoved(OnBoxMovedFromBoxAreaEvent data)
        {
            SetAllBoxClickableStatus(false);
        }
        
        private void SetAllBoxClickableStatus(bool gameStart)
        {
            var directions = new List<Vector2Int>()
            {
                new Vector2Int(0, 1),
                new Vector2Int(1, 0),
                new Vector2Int(-1, 0),
            };
            
            for (var x = 0; x < _gridSize.x; x++)
            {
                for (var y = 0; y < _gridSize.y; y++)
                {
                    var node = _grid[x, y];
                    if (node.CurrentBox)
                    {
                        var check = true;
                        if (!IsPositionFirstOnGrid(node.GridPosition))
                        {
                  
                            foreach (var dir in directions)
                            {
                                check = true;
                            
                                var s = node.GridPosition + dir;
                                if (!IsPositionInBound(s))
                                {
                                    check = false;
                                    continue;
                                }

                                if (_grid[s.x, s.y].CurrentBox)
                                {
                                    check = false;
                                    continue;
                                }
                                
                                if (_grid[s.x, s.y].IsObstacle)
                                {
                                    check = false;
                                    continue;
                                }

                                break;
                            }   
                        }
                        
                     
                        
                        node.CurrentBox.SetClickableStatus(check, gameStart);
                    }
                }
            }
        }

        private bool IsPositionInBound(Vector2Int pos)
            => pos.x >= 0 && pos.x < _gridSize.x && pos.y >= 0 && pos.y < _gridSize.y;

        private bool IsPositionFirstOnGrid(Vector2Int pos)
            => pos.x >= 0 && pos.x < _gridSize.x && pos.y == _gridSize.y - 1;
    }
}