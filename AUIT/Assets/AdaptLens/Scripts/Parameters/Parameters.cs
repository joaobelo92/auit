using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class Parameters : MonoBehaviour
{
    public class ParamReference<T>
    {
        public string name { get; private set; } 
        public object source { get; private set; }
        public FieldInfo field { get; private set; }

        public T Value
        {
            get { 
                return (T)field.GetValue(source);
            }
            set { 
                field.SetValue(source, value);
            }
        }

        public ParamReference(object source, FieldInfo field, string name = null)
        {
            this.name = name;
            this.source = source;
            this.field = field;
        }
    }

    public class FloatParamReference : ParamReference<float>
    {
        public float min { get; private set; }
        public float max { get; private set; }
        public FloatParamReference(object source, FieldInfo field, string name = null, float min = 0, float max = 1) : base(source, field, name)
        {
            this.min = min;
            this.max = max;
        }
    }

    [Header("References")]
    public AUIT.AUIT m_auit;

    public List<ParamReference<float>> m_parameters = new List<ParamReference<float>>();

    public void GetParameters(object obj)
    {
        var type = obj.GetType();
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var field in fields)
        {
            var attribute = field.GetCustomAttribute<Parameter>();
            if (attribute != null)
            {
                string gameObject = (obj as MonoBehaviour).gameObject.name;
                string name = string.IsNullOrEmpty(attribute.name) ? field.Name : attribute.name;
                name = $"{gameObject} {type.Name} {name}";

                object value = field.GetValue(obj);
                if (value is float)
                {
                    if (field.GetCustomAttribute<RangeAttribute>() != null)
                    {
                        var range = field.GetCustomAttribute<RangeAttribute>();
                        m_parameters.Add(new FloatParamReference(obj, field, name, range.min, range.max));
                    }
                    else
                    {
                        m_parameters.Add(new FloatParamReference(obj, field, name));
                    }
                }
            }
        }
    }

    public void GetParameters()
    {
        m_parameters.Clear();

        List<object> objects = m_auit.GetLocalObjectives().Cast<object>().ToList();
        foreach (object obj in objects)
        {
            GetParameters(obj);
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

[CustomEditor(typeof(Parameters))]
public class ParametersEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        Parameters parameters = (Parameters)target;

        if (GUILayout.Button("Get Parameters"))
        {
            parameters.GetParameters();
        }

        foreach (var parameter in parameters.m_parameters)
        {
            if (parameter is Parameters.FloatParamReference)
            {
                float min = 0;
                float max = 1;
                min = ((Parameters.FloatParamReference)parameter).min;
                max = ((Parameters.FloatParamReference)parameter).max;
                parameter.Value = EditorGUILayout.Slider(parameter.name, parameter.Value, min, max);
            }
            
        }
    }
}
