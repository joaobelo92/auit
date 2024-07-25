using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
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
            
            var (layouts, cost) = await AdaptationManager.OptimizeLayout();
            
            AdaptationManager.Adapt(layouts);
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