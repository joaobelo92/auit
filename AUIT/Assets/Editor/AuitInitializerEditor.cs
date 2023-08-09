using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public class AuitInitializerEditor : EditorWindow
{
    private List<SelectedObjectStruct> _selectedGameObjects = new ();
    private ListView _listView;
    private bool _dim;

    private class SelectedObjectStruct
    {
        public GameObject gameObject;
        public bool selected;

        public SelectedObjectStruct(GameObject gameObject, bool selected = true)
        {
            this.gameObject = gameObject;
            this.selected = selected;
        }
    }

[MenuItem("AUIT/Initialization")]
    public static void ShowAuitInitializerEditor()
    {
        GetWindow<AuitInitializerEditor>("AUIT Initialization");
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;
        const string instruction = "Here you can initialize an AUIT adaptation loop. An adaptation in AUIT consists of: \n" +
                                   "1) Adaptation Objectives \n2) Solver \n3) Context Sources \n4) Adaptation Trigger \n" +
                                   "5) Property Transitions";
        var explanationLabel = new Label(instruction)
        {
            style =
            {
                marginBottom = 10f,
                marginLeft = 10f,
                marginRight = 10f,
                marginTop = 10f,
                whiteSpace = WhiteSpace.Normal, // Allow the label to wrap text if it exceeds the maximum width
                maxWidth = 400f // Set the maximum width for the explanatory text
            }
        };
        root.Add(explanationLabel);
        
        ObjectField objectField = new ObjectField("Add to:")    
        {
            objectType = typeof(GameObject),
            style =
            {
                width = 300f,
                marginBottom = 10f
            }
        };
        objectField.RegisterValueChangedCallback(OnGameObjectSelected);
        root.Add(objectField);
        
        // Create a Label to display the instruction
        var label = new Label("Select Game Objects:");
        root.Add(label);
        
    }
    
    private void OnGameObjectSelected(ChangeEvent<Object> evt)
    {
        GameObject go = evt.newValue as GameObject;
        if (_selectedGameObjects.Any(m => m.gameObject == go))
        {
            return;
        }
        
        _selectedGameObjects.Add(new SelectedObjectStruct(go));
        
        var label = new Toggle(go.name)
        {
            value = true,
            tooltip = go.GetInstanceID().ToString(),
            style =
            {
                marginLeft = 10f,
                marginRight = 10f,
                whiteSpace = WhiteSpace.Normal
            }
        };
        label.RegisterValueChangedCallback(OnObjectToggled);
        rootVisualElement.Add(label);
    }

    private void OnObjectToggled(ChangeEvent<bool> evt)
    {
        // Crappy hack, but I did not manage to have bindings working
        int id = Convert.ToInt32(((Toggle)evt.target).tooltip);
        Debug.Log(id);
        int idx = _selectedGameObjects.FindIndex(m => m.gameObject.GetInstanceID() == id);
        _selectedGameObjects[idx].selected = !_selectedGameObjects[idx].selected;
    }
}
