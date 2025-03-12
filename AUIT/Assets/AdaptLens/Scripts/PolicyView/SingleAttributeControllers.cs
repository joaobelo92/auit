using Numpy;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public class SingleAttributeControllers : MonoBehaviour
{
    public delegate void OnHover(int hoverIndex);
    public OnHover onHover;


    private List<(string, List<(string, List<SingleAttributeController>)>)> m_sacs = new List<(string, List<(string, List<SingleAttributeController>)>)>();

    public List<(string, List<(string, List<SingleAttributeController>)>)> SACS
    {
        get { return m_sacs; }
    }

    public void Init(List<(string, List<(string, List<string>)>)> parameters)
    {
        m_sacs.Clear();

        foreach ((string objNames, List<(string, List<string>)> obj) in parameters)
        {
            List<(string, List<SingleAttributeController>)> objectiveSACs = new List<(string, List<SingleAttributeController>)>();
            foreach ((string objectiveName, List<string> objectiveParameters) in obj)
            {
                List<SingleAttributeController> sacs = new List<SingleAttributeController>();
                foreach (string parameter in objectiveParameters)
                {
                    SingleAttributeController sac = new SingleAttributeController(parameter);
                    sac.onHover += SetHoverSACs;
                    sacs.Add(sac);
                }
                objectiveSACs.Add((objectiveName, sacs));
            }
            m_sacs.Add((objNames, objectiveSACs));
        }
    }

    public void SetValues(NDarray values)
    {
        int pi = 0;
        foreach ((string objNames, List<(string, List<SingleAttributeController>)> obj) in m_sacs)
        {
            foreach ((string objectiveName, List<SingleAttributeController> objectiveSACs) in obj)
            {
                foreach (SingleAttributeController sac in objectiveSACs)
                {
                    sac.ClearValues();
                    for (int i = 0; i < values.shape[0]; i++)
                    {
                        sac.AddValue((float)values[i, pi]);
                    }
                    pi++;
                }
            }
        }
    }

    public void SetHoverSACs(int hoverIndex)
    {
        if (onHover != null)
        {
            onHover(hoverIndex);
        }

        foreach ((string objNames, List<(string, List<SingleAttributeController>)> obj) in m_sacs)
        {
            foreach ((string objectiveName, List<SingleAttributeController> objectiveSACs) in obj)
            {
                foreach (SingleAttributeController sac in objectiveSACs)
                {
                    sac.SetHover(hoverIndex);
                }
            }
        }
    }
}

[CustomEditor(typeof(SingleAttributeControllers))]
public class SingleAttributeControllersEditor : Editor
{
    SingleAttributeControllers sacs;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        sacs = (SingleAttributeControllers)target;

        foreach (var objSAC in sacs.SACS)
        {
            EditorGUILayout.LabelField(objSAC.Item1, EditorStyles.boldLabel);
            foreach (var objectiveSAC in objSAC.Item2)
            {
                EditorGUILayout.LabelField(objectiveSAC.Item1, EditorStyles.miniBoldLabel);
                foreach (var sac in objectiveSAC.Item2)
                {
                    sac.Draw();
                }
                EditorGUILayout.Space(5);
            }
            EditorGUILayout.Space(10);
        }

        Repaint();
    }
}
