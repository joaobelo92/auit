using AUIT.AdaptationObjectives;
using UnityEngine;

public class PolicyLogger : MonoBehaviour
{
    private void GetObjectives()
    {
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
            Debug.Log($"Objective: {objectiveType} on {objectiveObj}");
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GetObjectives();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
