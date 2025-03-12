using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class StripChartEditorWindow : EditorWindow
{
    // Data for the strip chart
    private List<float> dataPoints = new List<float>();
    private float minValue = float.MaxValue;
    private float maxValue = float.MinValue;
    private int maxDataPoints = 100;
    private float newDataPoint = 0f;

    // Dictionary to store jitter values for each data point
    private Dictionary<float, List<float>> jitterMap = new Dictionary<float, List<float>>();
    private float jitterAmount = 0.4f; // Controls the amount of vertical spread

    // Appearance settings
    private Color pointColor = Color.green;
    private float pointSize = 5f;
    private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f);
    private bool autoScale = true;
    private float manualMinValue = 0f;
    private float manualMaxValue = 1f;

    [MenuItem("Window/Value Strip Chart")]
    public static void ShowWindow()
    {
        GetWindow<StripChartEditorWindow>("Value Strip Chart");
    }

    private void OnGUI()
    {
        GUILayout.Label("Value Strip Chart", EditorStyles.boldLabel);

        EditorGUILayout.Space();
        DrawControls();
        EditorGUILayout.Space();
        DrawChart();
        EditorGUILayout.Space();
        DrawLegend();

        // Auto-repaint to update the chart every 100ms
        if (EditorApplication.timeSinceStartup % 0.1 < 0.016)
        {
            Repaint();
        }
    }

    private void DrawControls()
    {
        EditorGUILayout.BeginVertical("box");

        // Data input
        EditorGUILayout.BeginHorizontal();
        newDataPoint = EditorGUILayout.FloatField("New Data Point", newDataPoint);
        if (GUILayout.Button("Add"))
        {
            AddDataPoint(newDataPoint);
        }
        EditorGUILayout.EndHorizontal();

        // Random data generation for testing
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Random"))
        {
            AddDataPoint(Random.Range(-1f, 1f));
        }
        if (GUILayout.Button("Generate 10 Random"))
        {
            for (int i = 0; i < 10; i++)
            {
                AddDataPoint(Random.Range(-1f, 1f));
            }
        }
        if (GUILayout.Button("Clear Data"))
        {
            dataPoints.Clear();
            jitterMap.Clear();
            minValue = float.MaxValue;
            maxValue = float.MinValue;
        }
        EditorGUILayout.EndHorizontal();

        // Settings
        EditorGUILayout.Space();
        maxDataPoints = EditorGUILayout.IntSlider("Max Data Points", maxDataPoints, 10, 1000);
        pointColor = EditorGUILayout.ColorField("Point Color", pointColor);
        pointSize = EditorGUILayout.Slider("Point Size", pointSize, 1f, 10f);
        jitterAmount = EditorGUILayout.Slider("Jitter Amount", jitterAmount, 0.1f, 1f);
        backgroundColor = EditorGUILayout.ColorField("Background Color", backgroundColor);

        // Scale settings
        autoScale = EditorGUILayout.Toggle("Auto Scale", autoScale);
        if (!autoScale)
        {
            EditorGUILayout.BeginHorizontal();
            manualMinValue = EditorGUILayout.FloatField("Min Value", manualMinValue);
            manualMaxValue = EditorGUILayout.FloatField("Max Value", manualMaxValue);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawChart()
    {
        if (dataPoints.Count == 0) return;

        Rect chartRect = EditorGUILayout.GetControlRect(false, 200);

        // Draw background
        EditorGUI.DrawRect(chartRect, backgroundColor);

        // Get min and max values for scaling
        float min = autoScale ? minValue : manualMinValue;
        float max = autoScale ? maxValue : manualMaxValue;

        // Ensure min and max are different to avoid division by zero
        if (Mathf.Approximately(min, max))
        {
            max = min + 1f;
        }

        // Draw grid lines
        DrawGridLines(chartRect, min, max);

        // Draw data points
        Handles.color = pointColor;
        foreach (float value in dataPoints)
        {
            // Calculate horizontal position based on value
            float x = chartRect.x + (value - min) / (max - min) * chartRect.width;

            // Center points vertically
            float y = chartRect.y + chartRect.height / 2;

            // Draw point
            Handles.DrawSolidDisc(new Vector3(x, y, 0), Vector3.forward, pointSize);
        }
    }

    private void DrawGridLines(Rect chartRect, float min, float max)
    {
        // Draw horizontal center line
        Handles.color = new Color(1, 1, 1, 0.2f);
        float centerY = chartRect.y + chartRect.height / 2;
        Handles.DrawLine(new Vector3(chartRect.x, centerY, 0), new Vector3(chartRect.x + chartRect.width, centerY, 0));

        // Draw vertical grid lines and value labels
        int gridLines = 10;
        for (int i = 0; i <= gridLines; i++)
        {
            float ratio = i / (float)gridLines;
            float value = min + (max - min) * ratio;
            float x = chartRect.x + ratio * chartRect.width;

            // Draw vertical grid line
            Handles.DrawLine(new Vector3(x, chartRect.y, 0), new Vector3(x, chartRect.y + chartRect.height, 0));

            // Draw value label
            GUI.color = Color.white;
            string label = value.ToString("F2");
            Vector2 labelSize = GUI.skin.label.CalcSize(new GUIContent(label));
            GUI.Label(new Rect(x - labelSize.x / 2, chartRect.y + chartRect.height + 5, labelSize.x, 20), label);
        }
    }

    private void DrawLegend()
    {
        if (dataPoints.Count == 0) return;

        EditorGUILayout.BeginVertical("box");
        GUILayout.Label($"Data Points: {dataPoints.Count}");
        GUILayout.Label($"Min Value: {minValue.ToString("F2")}");
        GUILayout.Label($"Max Value: {maxValue.ToString("F2")}");
        GUILayout.Label($"Most Recent Value: {(dataPoints.Count > 0 ? dataPoints[dataPoints.Count - 1].ToString("F2") : "N/A")}");

        // Count of unique values
        GUILayout.Label($"Unique Values: {jitterMap.Count}");

        EditorGUILayout.EndVertical();
    }

    private void AddDataPoint(float value)
    {
        dataPoints.Add(value);

        // Update min and max values
        if (value < minValue) minValue = value;
        if (value > maxValue) maxValue = value;

        // Add jitter for this value
        if (!jitterMap.ContainsKey(value))
        {
            jitterMap[value] = new List<float>();
        }

        // Calculate jitter - if there are multiple identical values, spread them out
        float jitter = (jitterMap[value].Count % 2 == 0 ? 1 : -1) *
                     (jitterMap[value].Count / 2 + 1) * jitterAmount /
                     Mathf.Max(5, jitterMap[value].Count);

        // Clamp jitter to reasonable range
        jitter = Mathf.Clamp(jitter, -jitterAmount, jitterAmount);
        

        jitterMap[value].Add(jitter);

        // Limit the number of data points
        if (dataPoints.Count > maxDataPoints)
        {
            float removedValue = dataPoints[0];
            dataPoints.RemoveAt(0);

            // Remove a jitter entry for this value
            if (jitterMap.ContainsKey(removedValue) && jitterMap[removedValue].Count > 0)
            {
                jitterMap[removedValue].RemoveAt(0);
                if (jitterMap[removedValue].Count == 0)
                {
                    jitterMap.Remove(removedValue);
                }
            }

            // Recalculate min and max if necessary
            if (Mathf.Approximately(removedValue, minValue) || Mathf.Approximately(removedValue, maxValue))
            {
                RecalculateMinMax();
            }
        }
    }

    private void RecalculateMinMax()
    {
        minValue = float.MaxValue;
        maxValue = float.MinValue;

        foreach (float point in dataPoints)
        {
            if (point < minValue) minValue = point;
            if (point > maxValue) maxValue = point;
        }
    }
}

/*
public class StripChartEditorWindow : EditorWindow
{
    private List<Vector2> points;
    private float graphWidth = 400;
    private float graphHeight = 200;
    private float pointRadius = 5f;
    private int hoveredIndex = -1;

    [MenuItem("Window/Custom Graph")]
    public static void ShowWindow()
    {
        GetWindow<StripChartEditorWindow>("Graph");
    }

    private void OnEnable()
    {
        GeneratePoints();
    }

    private void GeneratePoints()
    {
        points = new List<Vector2>();
        for (int i = 0; i < 10; i++)
        {
            float x = Mathf.Lerp(50, graphWidth - 50, i / 9f);  // Distribute along X-axis
            float y = Random.Range(50, graphHeight - 50);      // Random Y (not meaningful)
            points.Add(new Vector2(x, y));
        }
    }

    private void OnGUI()
    {
        DrawGraph();
        HandleMouseHover();
        Repaint(); // Ensures continuous updates for smooth interaction
    }

    private void DrawGraph()
    {
        Handles.BeginGUI();
        for (int i = 0; i < points.Count; i++)
        {
            Color pointColor = (i == hoveredIndex) ? Color.red : Color.white;
            Handles.color = pointColor;
            Handles.DrawSolidDisc(points[i], Vector3.forward, pointRadius);
        }
        Handles.EndGUI();
    }

    private void HandleMouseHover()
    {
        Vector2 mousePos = Event.current.mousePosition;
        hoveredIndex = -1;

        for (int i = 0; i < points.Count; i++)
        {
            if (Vector2.Distance(mousePos, points[i]) < pointRadius * 1.5f)
            {
                hoveredIndex = i;
                break;
            }
        }
    }
}

*/