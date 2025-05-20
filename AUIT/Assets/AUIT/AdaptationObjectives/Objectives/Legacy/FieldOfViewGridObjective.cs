using System;
using System.Collections;
using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.AdaptationObjectives.Extras;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AUIT.AdaptationObjectives.Objectives
{
    public class FieldOfViewGridObjective : LocalObjective
    {
        public ContextSource<Camera> userContextSource;
        
        [Range(3, 10)]
        public int width = 5;
        [Range(3, 10)]
        public int height = 5;

        [SerializeField]
        public List<bool> grid = new List<bool>();

        public void OnValidate()
        {
            ResizeGrid();
        }

        private void ResizeGrid()
        {
            int total = width * height;
            if (grid.Count != total)
            {
                List<bool> newGrid = new List<bool>(new bool[total]);

                for (int i = 0; i < Mathf.Min(grid.Count, total); i++)
                {
                    newGrid[i] = grid[i];
                }

                grid = newGrid;
            }
        }

        private bool IsCellActive(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return false;
            return grid[y * width + x];
        }

        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            if (userContextSource == null || optimizationTarget == null)
                return 1f;

            Camera cam = userContextSource.GetValue();
            if (cam == null)
                return 1f;

            Vector3 viewportPos = cam.WorldToViewportPoint(optimizationTarget.Position);

            // Visibility check
            if (viewportPos.z < 0 || viewportPos.x < 0f || viewportPos.x > 1f || viewportPos.y < 0f || viewportPos.y > 1f)
            {
                return 1f;
            }

            // Map normalized viewport to grid cell
            int cellX = Mathf.FloorToInt(viewportPos.x * width);
            int cellY = Mathf.FloorToInt(viewportPos.y * height);

            if (IsCellActive(cellX, cellY))
                return 0f;

            float distance = DistanceToClosestActiveCell(viewportPos).Item1;
            float maxViewportDist = Mathf.Sqrt(1f * 1f + 1f * 1f); // Diagonal of viewport
            return Mathf.Clamp01(distance / (maxViewportDist / 3f));

        }

        public override Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout = null)
        {
            if (Random.value < 0.33f)
            {
                List<Vector2Int> activeCells = new List<Vector2Int>();
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (IsCellActive(x, y))
                        {
                            activeCells.Add(new Vector2Int(x, y));
                        }
                    }
                }

                if (activeCells.Count == 0)
                {
                    Debug.LogWarning("OptimizationRule: No active cells to choose from.");
                    return optimizationTarget;
                }

                // Pick active cell at random
                Vector2Int selectedCell = activeCells[Random.Range(0, activeCells.Count)];

                float cellWidth = Screen.width / (float)width;
                float cellHeight = Screen.height / (float)height;

                Vector3 screenCenter = new Vector3(
                    (selectedCell.x + 0.5f) * cellWidth,
                    (selectedCell.y + 0.5f) * cellHeight,
                    userContextSource.GetValue().WorldToScreenPoint(optimizationTarget.Position).z
                );

                Vector3 worldTarget = userContextSource.GetValue().ScreenToWorldPoint(screenCenter);

                Layout optimizedLayout = optimizationTarget.Clone();
                optimizedLayout.Position = worldTarget;
                return optimizedLayout; 
            }
            else
            {
                Vector3 position = optimizationTarget.Position;
                optimizationTarget.Position = position + Random.onUnitSphere * 
                    (HelperMath.SampleNormalDistribution(1.0f, 0.5f) * 0.05f);
                return optimizationTarget; 
            }
        }

        public override Layout DirectRule(Layout optimizationTarget)
        {
            throw new System.NotImplementedException();
        }

        public override float[] GetParameters()
        {
            throw new NotImplementedException();
        }

        public override void SetParameters(float[] parameters)
        {
            throw new NotImplementedException();
        }


        private (float, Vector2?) DistanceToClosestActiveCell(Vector2 screenPosition)
        {
            float cellWidth = Screen.width / (float)width;
            float cellHeight = Screen.height / (float)height;

            Vector2? closestCell = null;
            float closestDistanceSqr = float.MaxValue;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!IsCellActive(x, y))
                        continue;

                    // Calculate center of this cell in screen space
                    Vector2 cellCenter = new Vector2(
                        (x + 0.5f) * cellWidth,
                        (y + 0.5f) * cellHeight
                    );

                    float distSqr = (cellCenter - screenPosition).sqrMagnitude;

                    if (distSqr < closestDistanceSqr)
                    {
                        closestDistanceSqr = distSqr;
                        closestCell = new Vector2(x,y);
                    }
                }
            }
            
            // print(closestCell);
            // print(closestDistanceSqr);

            return (Mathf.Sqrt(closestDistanceSqr), closestCell);
        }
        
        public void PrintGrid()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"Grid ({width} × {height}):");

            for (int y = 0; y < height; y++)
            {
                int flippedY = (height - 1) - y; // Match top-to-bottom visual order
                for (int x = 0; x < width; x++)
                {
                    int index = flippedY * width + x;
                    bool cell = (index < grid.Count) ? grid[index] : false;
                    sb.Append(cell ? "X " : ". ");
                }
                sb.AppendLine();
            }

            Debug.Log(sb.ToString());
        }
    }
    
    [CustomEditor(typeof(FieldOfViewGridObjective))]
    public class FieldOfViewGridObjectiveEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            FieldOfViewGridObjective gridConfig = (FieldOfViewGridObjective)target;

            // Draw user context source
            SerializedProperty contextProp = serializedObject.FindProperty("userContextSource");
            EditorGUILayout.PropertyField(contextProp, new GUIContent("User Camera Source"));
            
            SerializedProperty weightProp = serializedObject.FindProperty("weight");
            EditorGUILayout.PropertyField(weightProp, new GUIContent("Weight"));

            EditorGUI.BeginChangeCheck();
            gridConfig.width = EditorGUILayout.IntSlider("Width", gridConfig.width, 3, 10);
            gridConfig.height = EditorGUILayout.IntSlider("Height", gridConfig.height, 3, 10);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(gridConfig, "Resize Grid");
                gridConfig.OnValidate();
                EditorUtility.SetDirty(gridConfig);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Active Cells", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            for (int y = gridConfig.height - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = 0; x < gridConfig.width; x++)
                {
                    int index = y * gridConfig.width + x;
                    if (index < gridConfig.grid.Count)
                    {
                        bool oldValue = gridConfig.grid[index];
                        bool newValue = EditorGUILayout.Toggle(oldValue, GUILayout.Width(20));
                        if (oldValue != newValue)
                        {
                            Undo.RecordObject(gridConfig, "Modify Grid Cell");
                            gridConfig.grid[index] = newValue;
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(gridConfig);
            }

            serializedObject.ApplyModifiedProperties();
        }

    }
}
