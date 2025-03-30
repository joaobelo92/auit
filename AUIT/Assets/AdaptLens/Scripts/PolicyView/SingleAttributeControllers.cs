using Newtonsoft.Json.Linq;
using Numpy;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;


public class SingleAttributeControllers : MonoBehaviour
{
    public delegate void OnHover(int hoverIndex);
    public OnHover onHover;

    public delegate void OnSelect(int selectIndex);
    public OnSelect onSelect;

    public delegate void OnApplyFiltering(int pi, float min, float max);
    public OnApplyFiltering onApplyFiltering;

    private List<(string, List<(string, List<SingleAttributeController>)>)> m_sacs = new List<(string, List<(string, List<SingleAttributeController>)>)>();

    public List<(string, List<(string, List<SingleAttributeController>)>)> SACS
    {
        get { return m_sacs; }
    }

    public void Init(List<(string, List<(string, List<string>)>)> parameters)
    {
        m_sacs.Clear();

        int pi = 0;
        foreach ((string objNames, List<(string, List<string>)> obj) in parameters)
        {
            List<(string, List<SingleAttributeController>)> objectiveSACs = new List<(string, List<SingleAttributeController>)>();
            foreach ((string objectiveName, List<string> objectiveParameters) in obj)
            {
                List<SingleAttributeController> sacs = new List<SingleAttributeController>();
                foreach (string parameter in objectiveParameters)
                {
                    SingleAttributeController sac = new SingleAttributeController(parameter, pi++);
                    sac.onHover += SetHoverPolicyViewer;
                    sac.onSelect += SetSelectedPolicyViewer;
                    sac.onApplyFiltering += ApplyFiltering;
                    sacs.Add(sac);
                }
                objectiveSACs.Add((objectiveName, sacs));
            }
            m_sacs.Add((objNames, objectiveSACs));
        }
    }

    public void SetValues(NDarray values, NDarray mask)
    {
        foreach ((string objNames, List<(string, List<SingleAttributeController>)> obj) in m_sacs)
        {
            foreach ((string objectiveName, List<SingleAttributeController> objectiveSACs) in obj)
            {
                foreach (SingleAttributeController sac in objectiveSACs)
                {
                    sac.SetValues(values[":", sac.Id], mask);

                    /*
                    sac.ClearValues();
                    for (int i = 0; i < values.shape[0]; i++)
                    {
                        sac.AddValue((float)values[i, sac.Id]);
                    }
                    sac.CalculateMinMax();
                    */
                }
            }
        }
    }

    /*
    public void SetValues(NDarray values)
    {
        foreach ((string objNames, List<(string, List<SingleAttributeController>)> obj) in m_sacs)
        {
            foreach ((string objectiveName, List<SingleAttributeController> objectiveSACs) in obj)
            {
                foreach (SingleAttributeController sac in objectiveSACs)
                {
                    sac.ClearValues();
                    for (int i = 0; i < values.shape[0]; i++)
                    {
                        sac.AddValue((float)values[i, sac.Id]);
                    } 
                    sac.CalculateMinMax();
                }
            }
        }
    }
    */

    public void SetSACMinMax(int pi, float min, float max)
    {
        foreach ((string objNames, List<(string, List<SingleAttributeController>)> obj) in m_sacs)
        {
            foreach ((string objectiveName, List<SingleAttributeController> objectiveSACs) in obj)
            {
                foreach (SingleAttributeController sac in objectiveSACs)
                {
                    if (sac.Id == pi)
                    {
                        sac.SetMinMax(min, max);
                    }
                }
            }
        }
    }

    private void SetHoverPolicyViewer(int hoverIndex)
    {
        if (onHover != null)
        {
            onHover(hoverIndex);
        }
    }

    public void SetHoverSACs(int hoverIndex)
    {
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

    private void SetSelectedPolicyViewer(int selectedIndex)
    {
        if (onSelect != null)
        {
            onSelect(selectedIndex);
        }
    }

    public void SetSelectedSACS(int selectedIndex)
    {
        foreach ((string objNames, List<(string, List<SingleAttributeController>)> obj) in m_sacs)
        {
            foreach ((string objectiveName, List<SingleAttributeController> objectiveSACs) in obj)
            {
                foreach (SingleAttributeController sac in objectiveSACs)
                {
                    sac.SetSelected(selectedIndex);
                }
            }
        }
    }

    public void ApplyFiltering(int pi, float min, float max)
    {
        if (onApplyFiltering != null)
        {
            onApplyFiltering(pi, min, max);
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
