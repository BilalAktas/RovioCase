using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core
{
    public class BoxAreaManager : MonoBehaviour
    {
        [Header("Grid")]
        private BoxGridNode[,] _grid;
        [SerializeField] private Vector2 _nodeSize;
        [SerializeField] private Vector2Int _gridSize;
        [SerializeField] private GameObject _nodePrefab;
        private Vector3 _bottomLeft;
        [SerializeField] private Vector3 _worldPositionOffset;
        [Header("Box")] 
        [SerializeField] private BoxProperties[] _boxProperties;
        [SerializeField] private GameObject _boxPrefab;
        [SerializeField] private GameObject _obstaclePrefab;
        
        
        private void Start()
        {
            CreateGrid();
            SpawnBox();
            SetAllBoxClickableStatus(true);
            
            EventBus.Subscribe<OnBoxMovedFromBoxAreaEvent>(OnBoxMoved);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OnBoxMovedFromBoxAreaEvent>(OnBoxMoved);
        }

        private void CreateGrid()
        {
            var origin = Vector2.zero;
            _grid = new BoxGridNode[_gridSize.x, _gridSize.y];
            var totalSize = new Vector2(_gridSize.x * _nodeSize.x, _gridSize.y * _nodeSize.y);
            _bottomLeft = origin - totalSize / 2 + _nodeSize / 2;
            for (var x = 0; x < _gridSize.x; x++)
            {
                for (var y = 0; y < _gridSize.y; y++)
                {
                    var worldPosition = _bottomLeft + new Vector3(x * _nodeSize.x, 0, y * _nodeSize.y);
                    worldPosition.y = 0;
                    worldPosition.z += _worldPositionOffset.z;
                    var gridPosition = new Vector2Int(x, y);
                    var clone = Instantiate(_nodePrefab, transform);
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
                        var obstacleClone = Instantiate(_obstaclePrefab, transform);
                        obstacleClone.transform.localPosition = node.WorldPosition;
                        
                        continue;
                    }
                    
                    var clone = Instantiate(_boxPrefab, transform);
                    var rp = _boxProperties.FirstOrDefault(x => x.BoxColor == color);
                    var box = clone.GetComponent<Box>();
                    clone.transform.localPosition = node.WorldPosition;
                    box.SetProperties(rp, node, LevelManager.Instance.CurrentLevelDesignData.GetBoxCapacity(x,y));
                    node.SetBox(box);
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