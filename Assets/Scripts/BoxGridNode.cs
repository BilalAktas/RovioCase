using UnityEngine;

namespace Core
{
    public class BoxGridNode
    {
        public Vector3 WorldPosition { get; set; }
        public Vector2Int GridPosition { get; set; }
        public Box CurrentBox { get; private set; }

        public void SetBox(Box box)
        {
            CurrentBox = box;
        }

        public BoxGridNode(Vector3 worldPosition, Vector2Int gridPosition)
        {
            WorldPosition = worldPosition;
            GridPosition = gridPosition;
        }
    }
}