using AUIT; 
using AUIT.AdaptationObjectives;
using AUIT.AdaptationObjectives.Definitions;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using static UnityEngine.GraphicsBuffer;

public class Element : MonoBehaviour
{
    public UnityEvent inspectingElementEvent = new UnityEvent();
    private const string LAYER = "Element";
    private List<Material> m_mat;
    private List<Color> m_originalColors;
    private Color m_highlightColor = Color.white;
    private List<Color> m_hideColors;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Init()
    {
        List<Renderer> rs = new List<Renderer>(); 
        Renderer r = GetComponent<Renderer>();
        if (r != null)
        {
            rs.Add(r);
        }
        foreach (Renderer rChild in GetComponentsInChildren<Renderer>())
        {
            rs.Add(rChild);
        }
        if (rs.Count <= 0)
        {
            Debug.LogError("Element: No renderer found");
            return;
        }
        m_mat = new List<Material>();
        foreach (Renderer rend in rs)
        {
            foreach (Material mat in rend.materials)
            {
                m_mat.Add(mat);
            }
        }
        m_originalColors = new List<Color>();
        m_hideColors = new List<Color>();
        foreach (Material mat in m_mat)
        {
            Color originalColor = mat.color;
            m_originalColors.Add(originalColor);
            m_hideColors.Add(new Color(originalColor.r, originalColor.g, originalColor.b, 0.05f));
        }

        gameObject.layer = LayerMask.NameToLayer(LAYER);
    }

    public void SetHighlight()
    {
        foreach (Material mat in m_mat)
        {
            mat.color = m_highlightColor;
        }
    }

    public void SetHide()
    {
        for (int i = 0; i < m_hideColors.Count; i++)
        {
            m_mat[i].color = m_hideColors[i];
        }
    }

    public void SetOriginal()
    {
        for (int i = 0; i < m_originalColors.Count; i++)
        {
            m_mat[i].color = m_originalColors[i];
        }
    }

    public void DebugCost()
    {
        Layout layout = new Layout("debug", transform);
        //float cost = CostFunction(layout);
        List<List<LocalObjective>> localObjectives = AUIT.AUIT.Instance.gatherOptimizationData().objectives;
        List<MultiElementObjective> globalObjectives = AUIT.AUIT.Instance.MultiElementObjectives;
        List<List<float>> objectiveCosts;
        List<float> multiObjectiveCosts;
        (objectiveCosts, multiObjectiveCosts) = AUIT.Solvers.Utils.ComputeCostsUnweighted(new List<Layout>() { layout }, localObjectives, globalObjectives);
        string debug = "";
        for (int i = 0; i < localObjectives.Count; i++)
        {
            for (int j = 0; j < localObjectives[i].Count; j++)
            {
                debug += localObjectives[i][j].gameObject.name + ", " + localObjectives[i][j].GetType().Name + ": " + objectiveCosts[i][j] + "\n";
            }
        }
        for (int i = 0; i < globalObjectives.Count; i++)
        {
            debug += "global, " + globalObjectives[i].GetType().Name + ": " + multiObjectiveCosts[i] + "\n";
        }
        Debug.Log(debug);
    }

    public void SetInspectingElement()
    {
        if (gameObject.activeInHierarchy)
        {
            inspectingElementEvent?.Invoke();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}


[CustomEditor(typeof(Element))]
public class ElementEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        Element element = (Element)target;
        if (GUILayout.Button("Debug Cost"))
        {
            element.DebugCost();
        }
        
        element.SetInspectingElement();
    }
}