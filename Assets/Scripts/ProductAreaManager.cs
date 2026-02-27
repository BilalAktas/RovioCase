using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class ProductAreaManager : MonoBehaviour
    {
        [Header("Grid")]
        private ProductGridNode[,] _grid;
        [SerializeField] private Vector2 _nodeSize;
        [SerializeField] private Vector2Int _gridSize;
        [SerializeField] private GameObject _nodePrefab;
        private Vector3 _bottomLeft;
        [SerializeField] private GameObject _cubePrefab;
        [SerializeField] private BoxProperties[] _boxProperties;
        [Header("DepthColumn")] 
        private static Dictionary<ProductDepthDirection, List<ProductGridNode>> _depthColumns = new();
        
        private void Start()
        {
            CreateGrid();
            CalculateDepth();
            
            EventBus.Subscribe<OnReCalculateDepthEvent>(OnReCalculateDepth);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OnReCalculateDepthEvent>(OnReCalculateDepth);
        }

        private void CreateGrid()
        {
            BoxColor[,] design =
            {
                { BoxColor.Red,   BoxColor.Red,   BoxColor.Red,   BoxColor.Green, BoxColor.Green, BoxColor.Green, BoxColor.Blue,  BoxColor.Blue,  BoxColor.Blue },
                { BoxColor.Red,   BoxColor.Red,   BoxColor.Red,   BoxColor.Green, BoxColor.Green, BoxColor.Green, BoxColor.Blue,  BoxColor.Blue,  BoxColor.Blue },
                { BoxColor.Red,   BoxColor.Red,   BoxColor.Red,   BoxColor.Green, BoxColor.Green, BoxColor.Green, BoxColor.Green, BoxColor.Red,   BoxColor.Red  },
                { BoxColor.Red,   BoxColor.Red,   BoxColor.Red,   BoxColor.Blue,  BoxColor.Blue,  BoxColor.Blue,  BoxColor.Green, BoxColor.Red,   BoxColor.Red  },
                { BoxColor.Green, BoxColor.Green, BoxColor.Green, BoxColor.Blue,  BoxColor.Blue,  BoxColor.Blue,  BoxColor.Green, BoxColor.Red,   BoxColor.Red  },
                { BoxColor.Green, BoxColor.Green, BoxColor.Green, BoxColor.Blue,  BoxColor.Blue,  BoxColor.Blue,  BoxColor.Red,   BoxColor.Red,   BoxColor.Red  },
                { BoxColor.Green, BoxColor.Green, BoxColor.Green, BoxColor.Blue,  BoxColor.Blue,  BoxColor.Blue,  BoxColor.Red,   BoxColor.Red,   BoxColor.Red  },
                { BoxColor.Green, BoxColor.Green, BoxColor.Green, BoxColor.Red,   BoxColor.Red,   BoxColor.Red,   BoxColor.Red,   BoxColor.Red,   BoxColor.Red  },
                { BoxColor.Green, BoxColor.Green, BoxColor.Green, BoxColor.Red,   BoxColor.Red,   BoxColor.Red,   BoxColor.Red,   BoxColor.Red,   BoxColor.Red  },
            };
            
            var origin = Vector2.zero;
            _grid = new ProductGridNode[_gridSize.x, _gridSize.y];
            var totalSize = new Vector2(_gridSize.x * _nodeSize.x, _gridSize.y * _nodeSize.y);
            _bottomLeft = origin - totalSize / 2 + _nodeSize / 2;
            for (var x = 0; x < _gridSize.x; x++)
            {
                for (var y = 0; y < _gridSize.y; y++)
                {
                    var worldPosition = _bottomLeft + new Vector3(x * _nodeSize.x, 0, y * _nodeSize.y);
                    worldPosition.y = 0;
                    var gridPosition = new Vector2Int(x, y);
                    var clone = Instantiate(_nodePrefab, transform);
                    var node = new ProductGridNode(worldPosition, gridPosition);
                    _grid[gridPosition.x, gridPosition.y] = node;
                    clone.transform.localPosition = worldPosition;

                    var cubeClone = Instantiate(_cubePrefab, transform);
                    var cube = _cubePrefab.GetComponent<Cube>();
                    node.SetCube(cube);
                    cubeClone.transform.localPosition = worldPosition;
                    cubeClone.GetComponent<Cube>().SetNode(node);

                    cubeClone.name = $"cube {cubeClone.transform.GetSiblingIndex()}";
                    
                    var c = design[x, y];
                    foreach (var property in _boxProperties)
                    {
                        if (property.BoxColor == c)
                        {
                            cube.SetProperties(property);
                            break;
                        }
                    }
                }
            }
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
            {
                for (var j = 0; j < _gridSize.y; j++)
                {
                    var n = _grid[i, j];
                    if (n.CurrentCube)
                    {
                        upColumns.Add(n);
                        break;
                    }
                }
            }
            
            for (var i = 0; i < _gridSize.y; i++)
            {
                for (var j = 0; j < _gridSize.x; j++)
                {
                    var n = _grid[j, i];
                    if (n.CurrentCube)
                    {
                        rightColumns.Add(n);
                        break;
                    }
                }
            }
            
            for (var i = 0; i < _gridSize.x; i++)
            {
                for (var j = _gridSize.y - 1; j >= 0; j--)
                {
                    var n = _grid[i, j];
                    if (n.CurrentCube)
                    {
                        downColumns.Add(n);
                        break;
                    }
                }
            }

            for (var i = _gridSize.y - 1; i >= 0; i--)
            {
                for (var j = _gridSize.x - 1; j >= 0; j--)
                {
                    var n = _grid[j, i];
                    if (n.CurrentCube)
                    {
                        leftColumns.Add(n);
                        break;
                    }   
                }
            }
            
            _depthColumns[ProductDepthDirection.Up] = upColumns;
            _depthColumns[ProductDepthDirection.Right] = rightColumns;
            _depthColumns[ProductDepthDirection.Down] = downColumns;
            _depthColumns[ProductDepthDirection.Left] = leftColumns;
        }

        public static bool IsFirstColumnToGet(ProductDepthDirection dir, Cube cube)
        {
            //Debug.Log($"dir {dir} -- {cube.name} -- get -- {_depthColumns[dir].Contains(cube.CurrentNode)}");
            if (_depthColumns[dir].Contains(cube.CurrentNode))
                return true;

            return false;
        }

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
            int x = node.GridPosition.x;
            int y = node.GridPosition.y;

            return dir switch
            {
                ProductDepthDirection.Up    => 8 - x,
                ProductDepthDirection.Right => y,
                ProductDepthDirection.Down  => x,
                ProductDepthDirection.Left  => 8 - y,
                _ => 0
            };
        }
    }
}