using AUIT;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(AdaptationManager))]
    public class AdaptationManagerInspector : UnityEditor.Editor
    {
        // private SerializedProperty _solver;
        //
        // private SerializedProperty _iterations;
        //
        // private SerializedProperty _minimumTemperature;
        // private SerializedProperty _initialTemperature;
        // private SerializedProperty _annealingSchedule;
        // private SerializedProperty _earlyStopping;
        //
        // private SerializedProperty _uiElements;
        //
        // private AdaptationManager _target;
        //
        // private void OnEnable()
        // {
        //     // link serialized properties to the target's fields
        //     // more efficient doing this only once
        //     _solver = serializedObject.FindProperty("solver");
        //     
        //     _iterations = serializedObject.FindProperty("iterations");
        //     
        //     _minimumTemperature = serializedObject.FindProperty("minimumTemperature");
        //     _initialTemperature = serializedObject.FindProperty("initialTemperature");
        //     _annealingSchedule = serializedObject.FindProperty("annealingSchedule");
        //     _earlyStopping = serializedObject.FindProperty("earlyStopping");
        //     
        //     _uiElements = serializedObject.FindProperty("uiElements");
        //     
        //     _target = (AdaptationManager) target;
        // }
        //
        // public override void OnInspectorGUI()
        // {
        //     // fetch current values from the real instance into the serialized "clone"
        //     serializedObject.Update();
        //
        //     // Draw field for A
        //     EditorGUILayout.PropertyField(_solver);
        //     EditorGUILayout.PropertyField(_iterations);
        //
        //     if (_solver.intValue == (int) AdaptationManager.Solver.SimulatedAnnealing)
        //     {
        //         // Draw field for B
        //         EditorGUILayout.PropertyField(_minimumTemperature);
        //         EditorGUILayout.PropertyField(_initialTemperature);
        //         EditorGUILayout.PropertyField(_annealingSchedule);
        //         EditorGUILayout.PropertyField(_earlyStopping);
        //     }
        //
        //     EditorGUILayout.PropertyField(_uiElements, new GUIContent("UI Elements // GameObjects"));
        //
        //
        //     // write back serialized values to the real instance
        //     // automatically handles all marking dirty and undo/redo
        //     serializedObject.ApplyModifiedProperties();
        // }
    }
}
