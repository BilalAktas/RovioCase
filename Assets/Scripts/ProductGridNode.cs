using UnityEngine;

namespace Core
{
    public class ProductGridNode
    {
        public Vector3 WorldPosition { get; set; }
        public Vector2Int GridPosition { get; set; }
        public Cube CurrentCube { get; private set; }
        public GameObject Visual { get; private set; }

        public void SetCube(Cube box)
        {
            CurrentCube = box;
        }

        public ProductGridNode(Vector3 worldPosition, Vector2Int gridPosition, GameObject visual)
        {
            WorldPosition = worldPosition;
            GridPosition = gridPosition;
            Visual = visual;
        }
    }
}