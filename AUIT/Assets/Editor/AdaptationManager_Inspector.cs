using AUIT;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(AdaptationManager))]
    public class AdaptationManagerInspector : UnityEditor.Editor
    {
        private SerializedProperty _solver;
        private SerializedProperty _hyperparameters;
        private SerializedProperty _uiElements;
    
        private void OnEnable()
        {
            // link serialized properties to the target's fields
            // more efficient doing this only once
            _solver = serializedObject.FindProperty("solver");
            _hyperparameters = serializedObject.FindProperty("hyperparameters");
            _uiElements = serializedObject.FindProperty("uiElements");
        }
    
        public override void OnInspectorGUI()
        {
            // fetch current values from the real instance into the serialized "clone"
            serializedObject.Update();
    
            // Draw field for A
            EditorGUILayout.PropertyField(_solver);
    
            if (_solver.intValue == (int) AdaptationManager.Solver.SimulatedAnnealing)
            {
                // Draw field for B
                EditorGUILayout.PropertyField(_hyperparameters);
            }
        
            EditorGUILayout.PropertyField(_uiElements, new GUIContent("UI Elements // GameObjects"));
        
    
            // write back serialized values to the real instance
            // automatically handles all marking dirty and undo/redo
            serializedObject.ApplyModifiedProperties();
        }
    }
}
