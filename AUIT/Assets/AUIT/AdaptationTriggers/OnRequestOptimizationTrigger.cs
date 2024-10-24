using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.Extras;
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
            
            OptimizationResponse response = await AdaptationManager.OptimizeLayout();
            
            AdaptationManager.Adapt(response.solutions);
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