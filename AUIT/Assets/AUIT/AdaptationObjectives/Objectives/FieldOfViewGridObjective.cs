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

        private void SetCellActive(int x, int y, bool active)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return;
            grid[y * width + x] = active;
        }

        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            if (userContextSource == null)
            {
                Debug.LogError("FieldOfViewObjective.CostFunction(): User context source is not set.");
            }
            
            Vector3 screenPos = userContextSource.GetValue().WorldToScreenPoint(optimizationTarget.Position);

            if (screenPos.z < 0)
                return 1f;
            
            int cellX = Mathf.FloorToInt((screenPos.x / Screen.width) * width);
            int cellY = Mathf.FloorToInt((screenPos.y / Screen.height) * height);
            
            if (IsCellActive(cellX, cellY))
            {
                return 0f;
            }

            print($"{DistanceToClosestActiveCell(screenPos).Item1}, {(float) Screen.height}");
            return Mathf.Clamp01(DistanceToClosestActiveCell(screenPos).Item1 / Screen.height);

        }

        public override Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout = null)
        {
            throw new System.NotImplementedException();
        }

        public override Layout DirectRule(Layout optimizationTarget)
        {
            throw new System.NotImplementedException();
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
                        closestCell = cellCenter;
                    }
                }
            }

            return (Mathf.Sqrt(closestDistanceSqr), closestCell);
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
            for (int y = 0; y < gridConfig.height; y++)
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
