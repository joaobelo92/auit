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

        public override ObjectiveType ObjectiveType => ObjectiveType.FieldOfView;

        
        [SerializeField]
        public bool debugDrawGrid = false;

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
    

        private void Update()
        {
            if (debugDrawGrid)
            {
                DrawDebugGrid();
            }
        }

        public void DrawDebugGrid()
        {
            Camera cam = userContextSource?.GetValue();
            if (cam == null) return;

            float z = 2.0f; // Distance in front of the camera to visualize the grid (adjust as needed)

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Center of the cell in normalized viewport coordinates
                    float cellCenterX = (x + 0.5f) / width;
                    float cellCenterY = (y + 0.5f) / height;
                    Vector3 viewportCenter = new Vector3(cellCenterX, cellCenterY, z);

                    Vector3 worldPoint = cam.ViewportToWorldPoint(viewportCenter);

                    Color color = IsCellActive(x, y) ? Color.green : Color.red;
                    Debug.DrawLine(cam.transform.position, worldPoint, color, 0.1f, false);
                }
            }
        }

        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            if (userContextSource == null || optimizationTarget == null)
            {
                Debug.LogError("No user context source or optimization target set for FOV Grid objective.");
                return 1f;
            }

            Camera cam = userContextSource.GetValue();
            if (cam == null)
            {
                Debug.LogError("No camera found in user context source for FOV Grid objective.");
                return 1f;
            }

            Vector3 viewportPos = cam.WorldToViewportPoint(optimizationTarget.Position);

            // Visibility check
            if (viewportPos.z < 0 || viewportPos.x < 0f || viewportPos.x > 1f || viewportPos.y < 0f || viewportPos.y > 1f)
            {
                // Debug.LogWarning("Optimization target is outside camera view.");
                return 1f;
            }

            // Map normalized viewport to grid cell
            // In this case X is the column and Y the row.. Perhaps make more intuitive
            int cellX = Mathf.FloorToInt(viewportPos.x * width);
            int cellY = Mathf.FloorToInt(viewportPos.y * height);
            // string gridString = "";
            // foreach (bool c in grid)
            // {
            //     gridString += c ? "X " : "0 ";
            // }
            // print("Cell: " + cellX + ", " + cellY);
            // print(gridString);

            if (IsCellActive(cellX, cellY))
            {
                // print("Cell is active: " + cellX + ", " + cellY);
                return 0f;
            }

            (int, int)? closestCell = DistanceToClosestActiveCell(viewportPos);
            if (closestCell == null)
            {
                Debug.LogWarning("No active cells found in the grid.");
                return 1f;
            }
            int manhattanDist = Math.Abs(cellX - closestCell.Value.Item1) + Math.Abs(cellY - closestCell.Value.Item2);
            // print("Man: " + manhattanDist);
            return Mathf.Clamp01(manhattanDist / 3f); // Normalize to a range of 0-1, assuming max distance is 3 cells away (Manhattan distance)

        }

        public override Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout = null)
        {
            if (Random.value < 0.5f)
            {
                // Find all active cells
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

                // Pick an active cell at random
                Vector2Int selectedCell = activeCells[Random.Range(0, activeCells.Count)];

                // Calculate the center of the selected cell in normalized viewport coordinates
                float cellCenterX = (selectedCell.x + 0.5f) / width;
                float cellCenterY = (selectedCell.y + 0.5f) / height;
                Vector3 viewportCenter = new Vector3(cellCenterX, cellCenterY, 0.5f); // z=0.5 as a reasonable default

                // Convert viewport center to world position using the user's camera
                Camera cam = userContextSource.GetValue();
                if (cam == null)
                {
                    Debug.LogError("OptimizationRule: No camera found in user context source.");
                    return optimizationTarget;
                }
                Vector3 worldTarget = cam.ViewportToWorldPoint(viewportCenter);

                Layout optimizedLayout = optimizationTarget.Clone();
                optimizedLayout.Position = worldTarget;
                // Debug.Log($"OptimizationRule: Moving to active cell {selectedCell} at world position {worldTarget}");
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


        private (int, int)? DistanceToClosestActiveCell(Vector2 position)
        {
            // float cellWidth = Screen.width / (float)width;
            // float cellHeight = Screen.height / (float)height;

            (int, int)? closestCell = null;
            float closestDistanceSqr = float.MaxValue;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!IsCellActive(x, y))
                        continue;

                    // Calculate center of this cell in screen space
                    // Vector2 cellCenter = new Vector2(
                    //     (x + 0.5f) * cellWidth,
                    //     (y + 0.5f) * cellHeight
                    // );
                    // Calculate center of this cell in screen space
                    Vector2 cellCenter = new Vector2(
                        (x + 0.5f) / width,
                        (y + 0.5f) / height
                    );

                    float distSqr = (cellCenter - position).sqrMagnitude;

                    if (distSqr < closestDistanceSqr)
                    {
                        closestDistanceSqr = distSqr;
                        closestCell = (x, y);
                    }
                }
            }
            
            // print(closestCell);
            // print(closestDistanceSqr);

            return closestCell;
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
            
            SerializedProperty debugProp = serializedObject.FindProperty("debugDrawGrid");
            EditorGUILayout.PropertyField(debugProp, new GUIContent("Debug Draw Grid"));

            serializedObject.ApplyModifiedProperties();
            
        }

    }
}
