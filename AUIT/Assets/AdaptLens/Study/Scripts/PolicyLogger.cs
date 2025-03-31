using AUIT.AdaptationObjectives;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Runtime.Serialization;

public class PolicyLogger : MonoBehaviour
{
    [Serializable]
    private class Objective
    {
        public string obj;
        public string objective;
        public float[] parameters;
    }

    public string m_layoutPolicyDir = "Assets/AdaptLens/Study/Saved Policies";
    public string m_fname = "";

    public void SaveLayoutPolicy()
    {
        List<Objective> objectives = new List<Objective>();
        LocalObjective[] localObjectives = GetComponentsInChildren<LocalObjective>();

        foreach (LocalObjective localObjective in localObjectives)
        {
            bool objectiveActive = localObjective.isActiveAndEnabled;
            if (!objectiveActive)
            {
                continue;
            }
            System.Type objectiveType = localObjective.GetType();
            GameObject objectiveObj = localObjective.gameObject;
            float[] parameters = localObjective.GetParameters();
            Objective objective = new Objective
            {
                obj = objectiveObj.name,
                objective = objectiveType.ToString(),
                parameters = parameters
            };
            objectives.Add(objective);
        }

        MultiElementObjective[] multiElementObjectives = GetComponents<MultiElementObjective>();
        foreach (MultiElementObjective multiElementObjective in multiElementObjectives)
        {
            bool objectiveActive = multiElementObjective.isActiveAndEnabled;
            if (!objectiveActive)
            {
                continue;
            }
            System.Type objectiveType = multiElementObjective.GetType();
            GameObject objectiveObj = multiElementObjective.gameObject;
            float[] parameters = multiElementObjective.GetParameters();
            Objective objective = new Objective
            {
                obj = "",
                objective = objectiveType.ToString(),
                parameters = parameters
            };
            objectives.Add(objective);
        }

        string json = JsonConvert.SerializeObject(objectives, Formatting.Indented);
        DateTime dateTime = DateTime.Now;
        string path = Path.Combine(m_layoutPolicyDir, $"{dateTime.ToString("MM_dd_yyyy_hh_mm_ss")}.json");
        File.WriteAllText(path, json);
    }

    public void LoadLayoutPolicy()
    {
        // Check if file exists
        string path = Path.Combine(m_layoutPolicyDir, m_fname);
        if (!File.Exists(path))
        {
            Debug.LogError($"File {path} does not exist.");
            return;
        }
        string objectivesJson = File.ReadAllText(path);
        List<Objective> objectives = JsonConvert.DeserializeObject<List<Objective>>(objectivesJson);
        foreach (Objective objective in objectives)
        {
            GameObject obj;
            if (objective.obj == "")
            {
                obj = gameObject;
            } else
            {
                obj = transform.Find(objective.obj)?.gameObject;
            }

            if (obj == null)
            {
                continue;
            }

            Type type = Type.GetType(objective.objective);
            if (type == null)
            {
                continue;
            }
            var targetObjective = obj.GetComponent(type);
            if (targetObjective is LocalObjective)
            {
                (targetObjective as LocalObjective).SetParameters(objective.parameters);
                (targetObjective as LocalObjective).enabled = true;
            }
            else if (targetObjective is MultiElementObjective)
            {
                (targetObjective as MultiElementObjective).SetParameters(objective.parameters);
                (targetObjective as MultiElementObjective).enabled = true;
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

[CustomEditor(typeof(PolicyLogger))]
public class PolicyLoggerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        PolicyLogger policyLogger = (PolicyLogger)target;
        if (GUILayout.Button("Load Layout Policy"))
        {
            policyLogger.LoadLayoutPolicy();
        }
        if (GUILayout.Button("Save Layout Policy"))
        {
            policyLogger.SaveLayoutPolicy();
        }
    }
}