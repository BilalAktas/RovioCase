using System;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(fileName = "LevelDesignData", menuName = "ScriptableObjects/Level Design Data")]
    public class LevelDesignData : ScriptableObject
    {
        [Header("Product)")] 
        [Min(1)] public int ProductGridWidth = 9;
        [Min(1)] public int ProductGridHeight = 9;
        [SerializeField] private Vector2 _productNodeSize;
        public Vector2 ProductNodeSize => _productNodeSize;
        [Header("Box")]
        public const int BoxWidth = 3;
        public const int BoxHeight = 3;
        [SerializeField] private Vector2 _boxNodeSize;
        [SerializeField] private GameObject _boxNodePrefab;
        [SerializeField] private Vector3 _boxWorldPositionOffset;
        [SerializeField] private ColorProperties[] _colorProperties;
        [SerializeField] private GameObject _boxPrefab;
        [SerializeField] private GameObject _boxObstaclePrefab;
        public Vector2 BoxNodeSize => _boxNodeSize;
        public GameObject NodePrefab => _boxNodePrefab;
        public Vector3 WorldPositionOffet => _boxWorldPositionOffset;
        public ColorProperties[] ColorProperties => _colorProperties;
        public GameObject BoxPrefab => _boxPrefab;
        public GameObject ObstaclePrefab => _boxObstaclePrefab;
        public Vector2Int BoxGridSize => new Vector2Int(BoxWidth, BoxHeight);
        [SerializeField] private BoxColor[] _boxCells;
        [SerializeField] private int[] _boxCaps;
        [SerializeField] private BoxColor[] _cells;

        public ColorProperties GetPropertyByColor(BoxColor color)
        {
            foreach (var property in ColorProperties)
                if (property.BoxColor == color) return property;

            return null;
        }
        
        
        public void EnsureSize()
        {
            var size = ProductGridWidth * ProductGridHeight;
            if (_cells == null || _cells.Length != size)
            {
                var newCells = new BoxColor[size];
                if (_cells != null) Array.Copy(_cells, newCells, Mathf.Min(_cells.Length, newCells.Length));
                _cells = newCells;
            }
            
            var boxSize = BoxWidth * BoxHeight;
            if (_boxCells == null || _boxCells.Length != boxSize)
            {
                var newBox = new BoxColor[boxSize];
                if (_boxCells != null) Array.Copy(_boxCells, newBox, Mathf.Min(_boxCells.Length, newBox.Length));
                _boxCells = newBox;
            }
            
            if (_boxCaps == null || _boxCaps.Length != boxSize)
            {
                var newCaps = new int[boxSize];
                if (_boxCaps != null) Array.Copy(_boxCaps, newCaps, Mathf.Min(_boxCaps.Length, newCaps.Length));
                _boxCaps = newCaps;
                
                 for (var i = 0; i < _boxCaps.Length; i++)
                     if (_boxCaps[i] == 0) _boxCaps[i] = 9;
            }
        }
        
        public BoxColor Get(int x, int y)
        {
            EnsureSize();
            return _cells[y * ProductGridWidth + x];
        }

        public void Set(int x, int y, BoxColor value)
        {
            EnsureSize();
            _cells[y * ProductGridWidth + x] = value;
        }

        public void Fill(BoxColor value)
        {
            EnsureSize();
            for (var i = 0; i < _cells.Length; i++)
                _cells[i] = value;
        }

        public BoxColor[] GetRaw()
        {
            EnsureSize();
            return _cells;
        }
        
        public BoxColor GetBox(int x, int y)
        {
            EnsureSize();
            return _boxCells[y * BoxWidth + x];
        }

        public void SetBox(int x, int y, BoxColor value)
        {
            EnsureSize();
            _boxCells[y * BoxWidth + x] = value;
            
            if (value == BoxColor.Empty)
                _boxCaps[y * BoxWidth + x] = 0;
        }

        public void FillBox(BoxColor value)
        {
            EnsureSize();
            for (var i = 0; i < _boxCells.Length; i++)
            {
                _boxCells[i] = value;
                if (value == BoxColor.Empty) _boxCaps[i] = 0;
            }
        }

        public int GetBoxCapacity(int x, int y)
        {
            EnsureSize();
            return _boxCaps[y * BoxWidth + x];
        }

        public void SetBoxCapacity(int x, int y, int cap)
        {
            EnsureSize();
            _boxCaps[y * BoxWidth + x] = Mathf.Max(0, cap);
        }

        public int GetBoxAmount()
        {
            EnsureSize();
            var amount = 0;
            foreach (var cell in _boxCells)
            {
                if (cell != BoxColor.Empty)
                    amount++;
            }
            return amount;
        }
    }
}