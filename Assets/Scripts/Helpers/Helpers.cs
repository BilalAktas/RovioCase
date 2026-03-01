using System;
using UnityEngine;

namespace Core
{
    public static class Helpers
    {
        public static Color AdjustBrightness(Color color, float delta)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            v = Mathf.Clamp01(v + delta);
            var result = Color.HSVToRGB(h, s, v);
            result.a = color.a;
            return result;
        }
        public static TNode[,] CreateGrid<TNode>(
            Vector2Int gridSize,
            Vector3 origin,
            Vector2 nodeSize,
            Vector3 worldOffset,
            Func<Vector3, Vector2Int, TNode> createNode,
            Action<TNode, Vector3, Vector2Int> afterCreate = null)
        {
            var grid = new TNode[gridSize.x, gridSize.y];

            var totalSize = new Vector2(gridSize.x * nodeSize.x, gridSize.y * nodeSize.y);
            var bottomLeft = (Vector2)origin - totalSize / 2f + nodeSize / 2f;

            for (var x = 0; x < gridSize.x; x++)
            for (var y = 0; y < gridSize.y; y++)
            {
                var worldPos = (Vector3)bottomLeft + new Vector3(x * nodeSize.x, 0f, y * nodeSize.y) + worldOffset;
                var gridPos = new Vector2Int(x, y);

                var node = createNode(worldPos, gridPos);
                grid[x, y] = node;

                afterCreate?.Invoke(node, worldPos, gridPos);
            }

            return grid;
        }

    }
}