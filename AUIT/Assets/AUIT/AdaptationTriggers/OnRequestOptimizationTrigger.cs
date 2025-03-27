using System.Collections.Generic;
using System.Linq;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.Extras;
using AUIT.Solvers;
using UnityEditor;
using UnityEngine;

namespace AUIT.AdaptationTriggers
{
    public class OnRequestOptimizationTrigger : AdaptationTrigger
    {
        public override async void ApplyStrategy()
        {
            if (enabled == false)
                return;
            
            OptimizationResponse response = await Auit.OptimizeLayout();

            List<float> multiObjectiveCosts;
            List<List<float>> objectiveCosts;
            (objectiveCosts, multiObjectiveCosts) = Utils.ComputeCosts(response.suggested.elements.ToList(), Auit.gatherOptimizationData().objectives, Auit.MultiElementObjectives);
            
            Debug.Log("multiObjectiveCosts:");
            foreach (float cost in multiObjectiveCosts)
            {
                Debug.Log(cost);
            }

            Debug.Log("objectiveCosts:");
            for (int i = 0; i < objectiveCosts.Count; i++)
            {
                string row = $"Element {i}: ";
                foreach (float cost in objectiveCosts[i])
                {
                    row += cost + " ";
                }
                Debug.Log(row);
            }
            
            if (response != null)
                Auit.Adapt(response.solutions);
        }

        private void Update()
        {
            if (Input.GetButtonDown("Optimization Request"))
            {
                ApplyStrategy();
            }
        }
    }
    
    [CustomEditor(typeof(OnRequestOptimizationTrigger))]
    public class MyComponentEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default Inspector
            DrawDefaultInspector();

            // Add a custom button
            OnRequestOptimizationTrigger trigger = (OnRequestOptimizationTrigger)target;
            if (GUILayout.Button("Request Optimization"))
            {
                trigger.ApplyStrategy();
            }
        }
    }
}