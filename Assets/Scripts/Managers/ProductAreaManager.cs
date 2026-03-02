using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class ProductAreaManager : MonoBehaviour
    {
        [Header("Level")]
        private LevelDesignData _currentLevelDesignData;
        [Header("Grid")]
        private ProductGridNode[,] _grid;
        private static Vector2Int _gridSize;
        [Header("DepthColumn")] 
        private static Dictionary<ProductDepthDirection, List<ProductGridNode>> _depthColumns = new();
        private readonly List<Cube> _spawnedCubes = new();
        
        private void Awake()
        {
            EventBus.Subscribe<OnLevelSpawnedEvent>(OnLevelSpawned);
            EventBus.Subscribe<OnReCalculateDepthEvent>(OnReCalculateDepth);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OnLevelSpawnedEvent>(OnLevelSpawned);
            EventBus.Unsubscribe<OnReCalculateDepthEvent>(OnReCalculateDepth);
        }

        private void OnLevelSpawned(OnLevelSpawnedEvent data)
        {
            if (_grid != null)
            {
                for (var x = 0; x < _gridSize.x; x++)
                    for (var y = 0; y < _gridSize.y; y++)
                    {
                        var node = _grid[x, y];
                        ObjectPool.Instance.Deposit(node.Visual, "ProductNodePrefab");
                    }
            }

            foreach (var cube in _spawnedCubes)
                ObjectPool.Instance.Deposit(cube.gameObject, "Cube");

            _spawnedCubes.Clear();
            
            _currentLevelDesignData = LevelManager.Instance.CurrentLevelDesignData;
            _gridSize = new Vector2Int(_currentLevelDesignData.ProductGridWidth, _currentLevelDesignData.ProductGridHeight);
            CreateGrid();
            CalculateDepth();
        }

        private void CreateGrid()
        {
            _grid = Helpers.CreateGrid(
                _gridSize,
                origin: Vector3.zero,
                nodeSize: _currentLevelDesignData.ProductNodeSize,
                worldOffset: Vector3.zero,
                createNode: (worldPos, gridPos) =>
                {
                    var clone = ObjectPool.Instance.GetFromPool("ProductNodePrefab");
                    clone.transform.SetParent(transform);
                    clone.SetActive(true);
                    clone.transform.localPosition = worldPos;

                    return new ProductGridNode(worldPos, gridPos, clone);
                },
                afterCreate: (node, worldPos, gridPos) =>
                {
                    worldPos.y = 0;
                    
                    var c = LevelManager.Instance.CurrentLevelDesignData.Get(gridPos.x, gridPos.y);
                    if (c == BoxColor.Empty) return;

                    var cubeClone = ObjectPool.Instance.GetFromPool("Cube");
                    cubeClone.transform.SetParent(transform);
                    cubeClone.SetActive(true);
                    cubeClone.transform.localPosition = worldPos;

                    var cube = cubeClone.GetComponent<Cube>();
                    cube.enabled = true;

                    node.SetCube(cube);
                    cube.SetNode(node);
                    cube.SetProperties(_currentLevelDesignData.GetPropertyByColor(c));

                    _spawnedCubes.Add(cube);
                }
            );
        }

        

        private void OnReCalculateDepth(OnReCalculateDepthEvent data)
        {
            CalculateDepth();
        }

        private void ResetDepthColumns()
        {
            _depthColumns[ProductDepthDirection.Up] = null;
            _depthColumns[ProductDepthDirection.Right] = null;
            _depthColumns[ProductDepthDirection.Down] = null;
            _depthColumns[ProductDepthDirection.Left] = null;
        }
        
        private void CalculateDepth()
        {
            ResetDepthColumns();
            
            var upColumns = new List<ProductGridNode>();
            var rightColumns = new List<ProductGridNode>();
            var downColumns = new List<ProductGridNode>();
            var leftColumns = new List<ProductGridNode>();
            
            for (var i = _gridSize.x - 1; i >= 0; i--)
            for (var j = 0; j < _gridSize.y; j++)
            {
                var n = _grid[i, j];
                if (n.CurrentCube)
                {
                    upColumns.Add(n);
                    break;
                }
            }
            
            for (var i = 0; i < _gridSize.y; i++)
            for (var j = 0; j < _gridSize.x; j++)
            {
                var n = _grid[j, i];
                if (n.CurrentCube)
                {
                    rightColumns.Add(n);
                    break;
                }
            }
            
            for (var i = 0; i < _gridSize.x; i++)
            for (var j = _gridSize.y - 1; j >= 0; j--)
            {
                var n = _grid[i, j];
                if (n.CurrentCube)
                {
                    downColumns.Add(n);
                    break;
                }
            }

            for (var i = _gridSize.y - 1; i >= 0; i--)
            for (var j = _gridSize.x - 1; j >= 0; j--)
            {
                var n = _grid[j, i];
                if (n.CurrentCube)
                {
                    leftColumns.Add(n);
                    break;
                }   
            }
            
            _depthColumns[ProductDepthDirection.Up] = upColumns;
            _depthColumns[ProductDepthDirection.Right] = rightColumns;
            _depthColumns[ProductDepthDirection.Down] = downColumns;
            _depthColumns[ProductDepthDirection.Left] = leftColumns;
            
        }

        public static bool IsFirstColumnToGet(ProductDepthDirection dir, Cube cube) =>  _depthColumns[dir].Contains(cube.CurrentNode);

        public static bool IsLastColumn(ProductDepthDirection dir, ProductGridNode node)
        {
            switch (dir)
            {
                case ProductDepthDirection.Up:
                    if (node.GridPosition.y >= 8)
                        return true;
                    break;
                case ProductDepthDirection.Right:
                    if (node.GridPosition.x >= 8)
                        return true;
                    break;
                case ProductDepthDirection.Down:
                    if (node.GridPosition.y <= 0)
                        return true;
                    break;
                case ProductDepthDirection.Left:
                    if (node.GridPosition.x <= 0)
                        return true;
                    break;
            }

            return false;
        }

        public static int GetDepthColumnIndex(ProductDepthDirection dir, ProductGridNode node)
        {
            var x = node.GridPosition.x;
            var y = node.GridPosition.y;

            return dir switch
            {
                ProductDepthDirection.Up    => (_gridSize.y - 1) - x,
                ProductDepthDirection.Right => y,
                ProductDepthDirection.Down  => x,
                ProductDepthDirection.Left  => (_gridSize.x - 1) - y,
                _ => 0
            };
        }
    }
}