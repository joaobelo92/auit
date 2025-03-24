using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class VoxelViewDistribution : MonoBehaviour
{
    public int m_numBins = 10;

    private List<float> m_values;


    private int[] m_binCounts;
    public int[] BinCounts
    {
        get { return m_binCounts; }
    }
    private List<float> m_binEdges;
    public List<float> BinEdges
    {
        get { return m_binEdges; }
    }

    public void SetValues(List<float> values)
    {
        m_values = values;
        // Get minimum and maximum
        float min = m_values.Min();
        float max = m_values.Max();

        m_binCounts = new int[m_numBins];
        m_binEdges = new List<float>();
        float binWidth = (max - min) / m_numBins;
        for (int i = 0; i <= m_numBins; i++)
        {
            m_binEdges.Add(min + i * binWidth);
        }
        // Count values in each bin
        foreach (float value in m_values)
        {
            // Handle the edge case where value == max
            if (value == max)
            {
                m_binCounts[m_numBins - 1]++;
                continue;
            }

            // Find appropriate bin
            int binIndex = (int)((value - min) / binWidth);
            m_binCounts[binIndex]++;
        }
    }
}


[CustomEditor(typeof(VoxelViewDistribution))]
public class VoxelViewDistributionEditor : Editor
{
    VoxelViewDistribution distribution;
    private const float HistogramHeight = 200f;
    private const float HistogramWidth = 400f;
    private Color barColor = new Color(0.3f, 0.6f, 1f);

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        distribution = (VoxelViewDistribution)target;

        if (distribution.BinCounts == null || distribution.BinEdges == null ||
            distribution.BinCounts.Length == 0 || distribution.BinEdges.Count == 0)
        {
            EditorGUILayout.HelpBox("No histogram data available.", MessageType.Info);
            return;
        }

        // Title for the histogram
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Distribution Histogram", EditorStyles.boldLabel);

        // Calculate max count for scaling
        int maxCount = 1; // Prevent division by zero
        foreach (int count in distribution.BinCounts)
        {
            maxCount = Mathf.Max(maxCount, count);
        }

        // Reserve space for the histogram
        Rect histogramRect = GUILayoutUtility.GetRect(HistogramWidth, HistogramHeight);

        // Draw background and border
        EditorGUI.DrawRect(histogramRect, new Color(0.2f, 0.2f, 0.2f));

        // Draw bars
        int binCount = distribution.BinCounts.Length;
        float barWidth = histogramRect.width / binCount;

        // Draw each bar
        for (int i = 0; i < binCount; i++)
        {
            float normalizedHeight = (float)distribution.BinCounts[i] / maxCount;
            float barHeight = normalizedHeight * histogramRect.height;

            Rect barRect = new Rect(
                histogramRect.x + i * barWidth,
                histogramRect.y + histogramRect.height - barHeight,
                barWidth - 1, // -1 for spacing between bars
                barHeight
            );

            EditorGUI.DrawRect(barRect, barColor);
        }

        // Draw X and Y axes labels
        EditorGUILayout.BeginHorizontal();

        // Min value
        EditorGUILayout.LabelField(distribution.BinEdges[0].ToString("F2"), GUILayout.Width(HistogramWidth * 0.15f));

        // Center value
        GUILayout.FlexibleSpace();
        int middleIndex = distribution.BinEdges.Count / 2;
        EditorGUILayout.LabelField(distribution.BinEdges[middleIndex].ToString("F2"));

        // Max value
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField(distribution.BinEdges[distribution.BinEdges.Count - 1].ToString("F2"),
            GUILayout.Width(HistogramWidth * 0.15f));

        EditorGUILayout.EndHorizontal();

        // Histogram details
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField($"Total count: {distribution.BinCounts.Sum()}");
        EditorGUILayout.LabelField($"Max bin: {maxCount}");
        EditorGUILayout.LabelField($"Range: {distribution.BinEdges[0]} to {distribution.BinEdges[distribution.BinEdges.Count - 1]}");
    }
}
