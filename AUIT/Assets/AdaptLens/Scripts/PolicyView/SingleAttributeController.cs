using Numpy;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;

public class SingleAttributeController
{
    public delegate void OnHover(int hoverIndex);
    public OnHover onHover;

    public delegate void OnSelect(int selectIndex);
    public OnSelect onSelect;

    public delegate void OnApplyFiltering(int id, float min, float max);
    public OnApplyFiltering onApplyFiltering;

    private string m_parameter;
    private int m_id;

    private float m_height = 50;
    private float m_margin = 10;
    private Color m_color = new Color(0.2f, 0.2f, 0.2f);

    private Color m_gridColor = new Color(0.4f, 0.4f, 0.4f, 0.2f);
    private int m_numGridlines = 4;

    private Color m_pointColor = new Color(0.6f, 0.6f, 0.6f, 0.4f);
    private float m_pointSize = 5;

    private NDarray m_values;
    private NDarray m_mask;
    private NDarray m_offsets; 

    private float m_minValue = 0;
    private float m_maxValue = 1;
    private float m_minMaxBuffer = 0.1f;

    private int m_hoverIndex = -1;
    private Color m_hoverColor = new Color(0.8f, 0.8f, 0.8f, 1.0f);

    private bool m_filtering;
    private float m_filteringStart;
    private float m_filteringEnd; 
    private Rect m_filter;
    private Color m_filterColor = new Color(49 / 255f, 130 / 255f, 189 / 255f, 0.5f);

    private int m_selectedIndex = -1;
    private Color m_selectedColor = new Color(49 / 255f, 130 / 255f, 189 / 255f, 1.0f);

    public string Name
    {
        get { return m_parameter; }
    }

    public int Id
    {
        get { return m_id; }
    }

    private void DrawGridLines(Rect cr, float min, float max)
    {
        Handles.color = m_gridColor;
        for (int i = 0; i <= m_numGridlines; i++)
        {
            float ratio = i / (float)m_numGridlines;
            float value = min + (max - min) * ratio;
            float x = cr.x + ratio * cr.width;

            GUI.color = Color.white;
            string label = value.ToString("F2");
            Vector2 labelSize = GUI.skin.label.CalcSize(new GUIContent(label));

            // Draw vertical grid line
            float offset = 0;
            if (i == 0)
            {
                offset = labelSize.x / 2;
            }
            else if (i == m_numGridlines)
            {
                offset = -labelSize.x / 2;
            } else
            {
                Handles.DrawLine(new Vector3(x, cr.y, 0), new Vector3(x, cr.y + cr.height, 0));
            }

            // Draw value label
            GUI.Label(new Rect(x - labelSize.x / 2 + offset, cr.y + cr.height, labelSize.x, 20), label);
        }
    }

    /*
    public void ClearValues()
    {
        m_values.Clear();
        m_offsets.Clear();
    }

    public void AddValue(float value)
    {
        m_values.Add(value);
        m_offsets.Add(Random.Range(-1f, 1f));

        // Update min and max values
        if (value < m_minValue) m_minValue = value;
        if (value > m_maxValue) m_maxValue = value;
    }
    */

    public void SetValues(NDarray values, NDarray mask)
    {
        m_values = values;
        m_mask = mask;

        float min = (float)np.min(values);
        float max = (float)np.max(values);

        if (Mathf.Approximately(min, max))
        {
            min -= m_minMaxBuffer / 2;
            max += m_minMaxBuffer / 2;
        }

        m_minValue = min;
        m_maxValue = max;

        m_offsets = np.random.uniform(np.array(-1.0f), np.array(1.0f), new int[] { values.shape[0] }).astype(np.float32);
    }

    public void SetMinMax(float min, float max)
    {
        m_minValue = min;
        m_maxValue = max;
    }

    /*
    public void CalculateMinMax()
    {
        if (m_values.Count == 0)
            return; 

        float min = Mathf.Infinity;
        float max = Mathf.NegativeInfinity;

        foreach (var value in m_values)
        {
            if (value < min)
                min = value;
            if (value > max)
                max = value;
        }

        if (Mathf.Approximately(min, max))
        {
            min -= m_minMaxBuffer / 2;
            max += m_minMaxBuffer / 2;
        }

        m_minValue = min;
        m_maxValue = max;
    }
    */

    private Vector2 ValueGraphPosition(float value, float min, float max, Rect cr)
    {
        float x = cr.x + (value - min) / (max - min) * cr.width;
        float y = cr.y + cr.height / 2;
        return new Vector2(x, y);
    }

    private Vector2 OffsetGraphPosition(float offset, Rect cr)
    {
        return new Vector2(0, offset * (cr.height / 2 - m_margin));
    }

    private float GraphPositionValue(Vector2 point, float min, float max, Rect cr)
    {
        float x = point.x - cr.x;
        float ratio = x / cr.width;
        return m_minValue + (m_maxValue - m_minValue) * ratio;
    }

    private float GraphPositionValue(float pointX, float min, float max, Rect cr)
    {
        float x = pointX - cr.x;
        float ratio = x / cr.width;
        return m_minValue + (m_maxValue - m_minValue) * ratio;
    }

    private void DrawPoint(float value, float offset, Rect cr, float min, float max, Color color) {
        Vector2 point2 = ValueGraphPosition(value, min, max, cr) + OffsetGraphPosition(offset, cr);
        Handles.color = color;
        Handles.DrawSolidDisc(point2, Vector3.forward, m_pointSize);

    }

    private void DrawPoints(Rect cr, float min, float max)
    {
        for (int i = 0; i < m_values.shape[0]; i++)
        {
            if ((bool)m_mask[i])
            {
                DrawPoint((float)m_values[i], (float)m_offsets[i], cr, min, max, m_pointColor);
            }
        }
    }

    private void DrawHover(Rect cr, float min, float max)
    {
        Handles.color = m_hoverColor;
        if (m_hoverIndex >= 0)
        {
            DrawPoint((float)m_values[m_hoverIndex], (float)m_offsets[m_hoverIndex], cr, min, max, m_hoverColor);
        }
    }

    private void DrawSelected(Rect cr, float min, float max)
    {
        Handles.color = m_selectedColor;
        if (m_selectedIndex >= 0)
        {
            DrawPoint((float)m_values[m_selectedIndex], (float)m_offsets[m_selectedIndex], cr, min, max, m_selectedColor);
        }
    }

    private void HandleMouseHover(Rect cr, float min, float max)
    {
        // Disable while filtering
        if (m_filtering)
        {
            return;
        }

        Event e = Event.current;
        Vector2 mousePos = e.mousePosition;
        
        if (!cr.Contains(mousePos))
        {
            return; 
        }

        // TODO: Calculate hover index
        int hoverIndex = -1;
        for (int i = 0; i < m_values.shape[0]; i++)
        {
            if (!(bool)m_mask[i])
            {
                continue;
            }

            float value = (float)m_values[i];
            float offset = (float)m_offsets[i];
            Vector2 valuePoint = ValueGraphPosition(value, min, max,cr) + OffsetGraphPosition(offset, cr);
            if (Vector2.Distance(mousePos, valuePoint) < m_pointSize)
            {
                hoverIndex = i;
                break;
            }
        }
        if (m_hoverIndex != hoverIndex)
        {
            m_hoverIndex = hoverIndex;
            
            if (onHover != null)
            {
                onHover(m_hoverIndex);
            }
        }
    }

    public void SetHover(int hoverIndex)
    {
        m_hoverIndex = hoverIndex;
    }

    private void ApplyFiltering(Rect cr, float min, float max)
    {
        float filterStartValue = GraphPositionValue(m_filteringStart, min, max, cr);
        float filterEndValue = GraphPositionValue(m_filteringEnd, min, max, cr);
        float filterMin = Mathf.Min(filterStartValue, filterEndValue);
        float filterMax = Mathf.Max(filterStartValue, filterEndValue);
        if (onApplyFiltering != null)
        {
            onApplyFiltering(m_id, filterMin, filterMax);
        }
    }

    private void HandleFiltering(Rect cr, float min, float max)
    {
        // Disable while hovering
        if (m_hoverIndex >= 0)
        {
            return;
        }

        Event e = Event.current;
        Vector2 mousePos = e.mousePosition;

        int controlId = GUIUtility.GetControlID(FocusType.Passive);

        if (cr.Contains(mousePos) && e.type == EventType.MouseDown && e.button == 0)
        {
            m_filtering = true;

            m_filteringStart = mousePos.x;
            m_filteringEnd = Mathf.Clamp(mousePos.x, cr.x, cr.x + cr.width);

            GUIUtility.hotControl = controlId;

            e.Use();
        }

        if (e.type == EventType.MouseUp && GUIUtility.hotControl == controlId)
        {
            m_filtering = false;

            ApplyFiltering(cr, min, max);

            GUIUtility.hotControl = 0;

            e.Use();
        }


        if (m_filtering && e.type == EventType.MouseDrag && GUIUtility.hotControl == controlId)
        {
            m_filteringEnd = Mathf.Clamp(mousePos.x, cr.x, cr.x + cr.width);
            
            e.Use();
        }

    }

    private void HandleSelection()
    {
        if (m_hoverIndex < 0)
        {
            return;
        }

        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            if (onSelect != null)
            {
                onSelect(m_hoverIndex);
            }

            e.Use();
        }

    }

    public void SetSelected(int selectedIndex)
    {
        m_selectedIndex = selectedIndex;
    }

    private void DrawFilter(Rect cr, float min, float max)
    {
        if (m_filtering)
        {
            m_filter = new Rect(
                Mathf.Min(m_filteringStart, m_filteringEnd),
                cr.y,
                Mathf.Abs(m_filteringStart - m_filteringEnd),
                cr.height
            );
            EditorGUI.DrawRect(m_filter, m_filterColor);
        }
    }

    public void Draw()
    {
        // Background
        GUILayout.Label(m_parameter);
        Rect cr = EditorGUILayout.GetControlRect(false, m_height);
        EditorGUI.DrawRect(cr, m_color);

        float min = m_minValue;
        float max = m_maxValue;

        DrawGridLines(cr, min, max);

        HandleMouseHover(cr, min, max);
        HandleSelection();
        HandleFiltering(cr, min, max);

        DrawPoints(cr, min, max);
        DrawHover(cr, min, max);
        DrawSelected(cr, min, max);
        DrawFilter(cr, min, max);

        EditorGUILayout.Space(20);
    }

    public SingleAttributeController(string parameter, int id)
    {
        m_parameter = parameter;
        m_id = id; 
    }

}
