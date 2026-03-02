#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Core
{
    [CustomEditor(typeof(LevelDesignData))]
    public class LevelDesignDataEditor : Editor
    {
        private static readonly BoxColor[] _cycleOrder =
        {
            BoxColor.Empty, BoxColor.Red, BoxColor.Green, BoxColor.Blue
        };

        private const float CELL_SIZE = 28f;
        private const float CELL_PAD = 3f;

        private int _selectedBoxX = -1;
        private int _selectedBoxY = -1;

        private SerializedProperty _boxNodeSize;
        private SerializedProperty _boxNodePrefab;
        private SerializedProperty _boxWorldPositionOffset;
        private SerializedProperty _colorProperties;
        private SerializedProperty _boxPrefab;
        private SerializedProperty _boxObstaclePrefab;
        private SerializedProperty _productNodeSize;

        private void OnEnable()
        {
            _boxNodeSize = serializedObject.FindProperty("_boxNodeSize");
            _boxNodePrefab = serializedObject.FindProperty("_boxNodePrefab");
            _boxWorldPositionOffset = serializedObject.FindProperty("_boxWorldPositionOffset");
            _colorProperties = serializedObject.FindProperty("_colorProperties");
            _boxPrefab = serializedObject.FindProperty("_boxPrefab");
            _boxObstaclePrefab = serializedObject.FindProperty("_boxObstaclePrefab");
            _productNodeSize = serializedObject.FindProperty("_productNodeSize");
        }

        public override void OnInspectorGUI()
        {
            var data = (LevelDesignData)target;

            var newWidth = Mathf.Max(1, EditorGUILayout.IntField("ProductGridWidth", data.ProductGridWidth));
            var newHeight = Mathf.Max(1, EditorGUILayout.IntField("ProductGridHeight", data.ProductGridHeight));

            serializedObject.Update();

            EditorGUILayout.PropertyField(_boxNodeSize);
            EditorGUILayout.PropertyField(_boxNodePrefab);
            EditorGUILayout.PropertyField(_boxWorldPositionOffset);
            EditorGUILayout.PropertyField(_colorProperties, true);
            EditorGUILayout.PropertyField(_boxPrefab);
            EditorGUILayout.PropertyField(_boxObstaclePrefab);
            EditorGUILayout.PropertyField(_productNodeSize);

            serializedObject.ApplyModifiedProperties();

            if (newWidth != data.ProductGridWidth || newHeight != data.ProductGridHeight)
            {
                Undo.RecordObject(data, "Resize Main Grid");
                data.ProductGridWidth = newWidth;
                data.ProductGridHeight = newHeight;

                data.EnsureSize();
                EditorUtility.SetDirty(data);
            }

            data.EnsureSize();

            GUILayout.Space(8);
            DrawLegend();


            GUILayout.Space(8);
            EditorGUILayout.LabelField("Main Grid (Cells)  (0,0 = bottom-left)", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Fill Empty")) PaintFillMain(data, BoxColor.Empty);
                if (GUILayout.Button("Fill Red")) PaintFillMain(data, BoxColor.Red);
                if (GUILayout.Button("Fill Green")) PaintFillMain(data, BoxColor.Green);
                if (GUILayout.Button("Fill Blue")) PaintFillMain(data, BoxColor.Blue);
                if (GUILayout.Button("Fill Random")) PaintRandomMain(data);
            }

            GUILayout.Space(6);
            DrawGridMain(data);


            GUILayout.Space(14);
            EditorGUILayout.LabelField("Box Grid (3x3)  (0,0 = bottom-left)", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Fill Empty")) PaintFillBox(data, BoxColor.Empty);
                if (GUILayout.Button("Fill Red")) PaintFillBox(data, BoxColor.Red);
                if (GUILayout.Button("Fill Green")) PaintFillBox(data, BoxColor.Green);
                if (GUILayout.Button("Fill Blue")) PaintFillBox(data, BoxColor.Blue);
                if (GUILayout.Button("Generate From Main Grid"))
                    GenerateBoxesFromMainGrid(data);
            }

            GUILayout.Space(6);
            DrawGridBox(data);

            DrawSelectedBoxCapacityEditor(data);
        }

        private static void PaintRandomMain(LevelDesignData data)
        {
            Undo.RecordObject(data, "Randomize Main Grid");

            for (var y = 0; y < data.ProductGridHeight; y++)
            {
                for (var x = 0; x < data.ProductGridWidth; x++)
                {
                    var randomColor = (BoxColor)Random.Range(0, 3);
                    data.Set(x, y, randomColor);
                }
            }

            EditorUtility.SetDirty(data);
        }

        private static void GenerateBoxesFromMainGrid(LevelDesignData data)
        {
            Undo.RecordObject(data, "Generate Balanced Boxes");

            var colorCounts = new System.Collections.Generic.Dictionary<BoxColor, int>();
            
            for (var y = 0; y < data.ProductGridHeight; y++)
            {
                for (var x = 0; x < data.ProductGridWidth; x++)
                {
                    var c = data.Get(x, y);
                    if (c == BoxColor.Empty) continue;

                    if (!colorCounts.ContainsKey(c))
                        colorCounts[c] = 0;

                    colorCounts[c]++;
                }
            }

            var totalCells = 0;
            foreach (var kvp in colorCounts)
                totalCells += kvp.Value;

            if (totalCells == 0)
            {
                EditorUtility.SetDirty(data);
                return;
            }

            var totalBoxes = LevelDesignData.BoxWidth * LevelDesignData.BoxHeight;
            
            var boxDistribution = new System.Collections.Generic.Dictionary<BoxColor, int>();
            var assignedBoxes = 0;

            foreach (var kvp in colorCounts)
            {
                var ratio = (float)kvp.Value / totalCells;
                var boxCount = Mathf.FloorToInt(ratio * totalBoxes);
                boxDistribution[kvp.Key] = boxCount;
                assignedBoxes += boxCount;
            }
            
            while (assignedBoxes < totalBoxes)
            {
                foreach (var color in colorCounts.Keys)
                {
                    boxDistribution[color]++;
                    assignedBoxes++;
                    if (assignedBoxes >= totalBoxes)
                        break;
                }
            }
            
            var boxList = new System.Collections.Generic.List<(BoxColor color, int capacity)>();

            foreach (var kvp in boxDistribution)
            {
                var color = kvp.Key;
                var boxCount = kvp.Value;
                var totalColorCount = colorCounts[color];

                if (boxCount == 0) continue;

                var baseCap = totalColorCount / boxCount;
                var remainder = totalColorCount % boxCount;

                for (var i = 0; i < boxCount; i++)
                {
                    var cap = baseCap;
                    if (remainder > 0)
                    {
                        cap++;
                        remainder--;
                    }

                    boxList.Add((color, cap));
                }
            }

            for (var i = boxList.Count - 1; i > 0; i--)
            {
                var rand = Random.Range(0, i + 1);
                var temp = boxList[i];
                boxList[i] = boxList[rand];
                boxList[rand] = temp;
            }

            for (var y = 0; y < LevelDesignData.BoxHeight; y++)
            {
                for (var x = 0; x < LevelDesignData.BoxWidth; x++)
                {
                    data.SetBox(x, y, BoxColor.Empty);
                    data.SetBoxCapacity(x, y, 0);
                }
            }

            for (var i = 0; i < boxList.Count && i < totalBoxes; i++)
            {
                var x = i % LevelDesignData.BoxWidth;
                var y = i / LevelDesignData.BoxWidth;

                data.SetBox(x, y, boxList[i].color);
                data.SetBoxCapacity(x, y, boxList[i].capacity);
            }

            EditorUtility.SetDirty(data);
        }

        private static void PaintRandom(LevelDesignData data)
        {
            Undo.RecordObject(data, "Randomize Box Grid");

            for (var y = 0; y < LevelDesignData.BoxHeight; y++)
            {
                for (var x = 0; x < LevelDesignData.BoxWidth; x++)
                {
                    var randomColor = (BoxColor)Random.Range(0, 3);
                    data.SetBox(x, y, randomColor);
                }
            }

            EditorUtility.SetDirty(data);
        }


        private static void PaintFillMain(LevelDesignData data, BoxColor c)
        {
            Undo.RecordObject(data, "Fill Main Grid");
            data.Fill(c);
            EditorUtility.SetDirty(data);
        }

        private static void PaintFillBox(LevelDesignData data, BoxColor c)
        {
            Undo.RecordObject(data, "Fill Box Grid");
            data.FillBox(c);
            EditorUtility.SetDirty(data);
        }

        private static void DrawGridMain(LevelDesignData data)
        {
            for (var y = data.ProductGridHeight - 1; y >= 0; y--)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(2);
                    for (var x = 0; x < data.ProductGridWidth; x++)
                    {
                        var current = data.Get(x, y);
                        DrawCell(() =>
                        {
                            Undo.RecordObject(data, "Paint Main Cell");
                            data.Set(x, y, Cycle(current, +1));
                            EditorUtility.SetDirty(data);
                        }, current);
                    }
                }
            }
        }


        private void DrawGridBox(LevelDesignData data)
        {
            for (var y = LevelDesignData.BoxHeight - 1; y >= 0; y--)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(2);
                    for (var x = 0; x < LevelDesignData.BoxWidth; x++)
                    {
                        var currentColor = data.GetBox(x, y);
                        var cap = data.GetBoxCapacity(x, y);
                        DrawBoxCell(data, x, y, currentColor, cap);
                    }
                }
            }
        }

        private void DrawBoxCell(LevelDesignData data, int x, int y, BoxColor color, int cap)
        {
            var r = GUILayoutUtility.GetRect(CELL_SIZE, CELL_SIZE, GUILayout.Width(CELL_SIZE),
                GUILayout.Height(CELL_SIZE));

            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = ToGuiColor(color);


            var selected = (x == _selectedBoxX && y == _selectedBoxY);
            if (selected)
            {
                var prevCol = GUI.color;
                GUI.color = Color.yellow;
                GUI.Box(r, GUIContent.none);
                GUI.color = prevCol;
            }


            if (GUI.Button(r, GUIContent.none))
            {
                Undo.RecordObject(data, "Paint Box Cell");
                data.SetBox(x, y, Cycle(color, +1));
                EditorUtility.SetDirty(data);

                _selectedBoxX = x;
                _selectedBoxY = y;
                Repaint();
            }


            if (data.GetBox(x, y) != BoxColor.Empty)
            {
                var style = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
                GUI.Label(r, cap.ToString(), style);
            }

            GUI.backgroundColor = prevBg;
            GUILayout.Space(CELL_PAD);
        }

        private void DrawSelectedBoxCapacityEditor(LevelDesignData data)
        {
            if (_selectedBoxX < 0 || _selectedBoxY < 0) return;

            GUILayout.Space(10);
            EditorGUILayout.LabelField($"Selected Box: ({_selectedBoxX},{_selectedBoxY})", EditorStyles.miniBoldLabel);

            var currentCap = data.GetBoxCapacity(_selectedBoxX, _selectedBoxY);
            var newCap = EditorGUILayout.IntField("Capacity", currentCap);
            newCap = Mathf.Max(0, newCap);

            if (newCap != currentCap)
            {
                Undo.RecordObject(data, "Change Box Capacity");
                data.SetBoxCapacity(_selectedBoxX, _selectedBoxY, newCap);
                EditorUtility.SetDirty(data);
            }
        }

        private static void DrawCell(System.Action onClick, BoxColor current)
        {
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = ToGuiColor(current);

            if (GUILayout.Button("", GUILayout.Width(CELL_SIZE), GUILayout.Height(CELL_SIZE)))
                onClick?.Invoke();

            GUI.backgroundColor = prevBg;
            GUILayout.Space(CELL_PAD);
        }

        private static void DrawLegend()
        {
            EditorGUILayout.LabelField("Click a cell to cycle: Empty → Red → Green → Blue", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawSwatch(BoxColor.Empty);
                DrawSwatch(BoxColor.Red);
                DrawSwatch(BoxColor.Green);
                DrawSwatch(BoxColor.Blue);
            }
        }

        private static void DrawSwatch(BoxColor c)
        {
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = ToGuiColor(c);
            GUILayout.Box(c.ToString(), GUILayout.Width(70), GUILayout.Height(18));
            GUI.backgroundColor = prev;
        }

        private static BoxColor Cycle(BoxColor current, int dir)
        {
            var idx = 0;
            for (var i = 0; i < _cycleOrder.Length; i++)
            {
                if (_cycleOrder[i] == current)
                {
                    idx = i;
                    break;
                }
            }

            idx = (idx + dir) % _cycleOrder.Length;
            if (idx < 0) idx += _cycleOrder.Length;
            return _cycleOrder[idx];
        }

        private static Color ToGuiColor(BoxColor c)
        {
            return c switch
            {
                BoxColor.Empty => new Color(0.18f, 0.18f, 0.18f, 1f),
                BoxColor.Red => new Color(0.78f, 0.20f, 0.20f, 1f),
                BoxColor.Green => new Color(0.20f, 0.78f, 0.35f, 1f),
                BoxColor.Blue => new Color(0.20f, 0.55f, 0.85f, 1f),
                _ => Color.gray
            };
        }
    }
}
#endif