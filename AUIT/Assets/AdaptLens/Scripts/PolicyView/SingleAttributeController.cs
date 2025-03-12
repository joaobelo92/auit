using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SingleAttributeController
{
    private string m_label;

    private float m_height = 50;
    private Color m_color = new Color(0.2f, 0.2f, 0.2f);

    private Color m_gridColor = new Color(0.5f, 0.5f, 0.5f, 0.2f);
    private int m_numGridlines = 4;

    private Color m_pointColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
    private float m_pointSize = 5;

    private List<float> m_values = new List<float>();
    private List<Vector2> m_points = new List<Vector2>();

    private float m_minValue = 0;
    private float m_maxValue = 1;
    private float m_minMaxBuffer = 0.1f;

    private bool m_isHovering; 
    private Color m_hoverColor = new Color(49/255f, 130/255f, 189/255f, 0.8f);

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

    public void AddValue(float value)
    {
        m_values.Add(value);

        // Update min and max values
        if (value < m_minValue) m_minValue = value;
        if (value > m_maxValue) m_maxValue = value;
    }

    private Vector2 PointGraphPosition(float value, float min, float max, Rect cr)
    {
        float x = cr.x + (value - min) / (max - min) * cr.width;
        float y = cr.y + cr.height / 2;
        return new Vector2(x, y);
    }


    private void DrawPoints(Rect cr, float min, float max)
    {
        
        m_points.Clear();
        foreach (float value in m_values)
        {
            Vector2 point = PointGraphPosition(value, min, max, cr);

            Handles.color = m_pointColor;
            Handles.DrawSolidDisc(point, Vector3.forward, m_pointSize);
        }
    }

    // TODO
    /*
    private void HandleMouseHover(Rect cr)
    {
        Event e = Event.current;
        Vector2 mousePos = e.mousePosition;
        if (cr.Contains(mousePos))
        {
            m_isHovering = true;
            Vector2 vector2 = new Vector2(mousePos.x, cr.y + cr.height / 2);
            float x = mousePos.x - cr.x;
            float ratio = x / cr.width;
            float value = m_minValue + (m_maxValue - m_minValue) * ratio;
            Vector2 point = PointGraphPosition(value, m_minValue, m_maxValue, cr);
            Handles.color = m_hoverColor;
            Handles.DrawSolidDisc(point, Vector3.forward, m_pointSize);
        }
        else
        {
            m_isHovering = false;
        }
    }
    */

    public void Draw()
    {
        // Background
        GUILayout.Label(m_label, EditorStyles.boldLabel);
        Rect cr = EditorGUILayout.GetControlRect(false, m_height);
        EditorGUI.DrawRect(cr, m_color);

        float min = m_minValue;
        float max = m_maxValue;
        if (Mathf.Approximately(min, max))
        {
            min -= m_minMaxBuffer / 2;
            max += m_minMaxBuffer / 2;
        }

        DrawGridLines(cr, min, max);

        DrawPoints(cr, min, max);

        EditorGUILayout.Space(20);
    }

    public SingleAttributeController(string label)
    {
        m_label = label;
    }
}
