using AUIT.AdaptationObjectives;
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
            this.source = source;
            this.field = field;
            this.name = name;
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

    public List<(string, List<(string, List<ParamReference<float>>)>)> Params
    {
        get { return m_parameters; }
    }
    private List<(string, List<(string, List<ParamReference<float>>)>)> m_parameters = new List<(string, List<(string, List<ParamReference<float>>)>)>();
    public (string, List<ParamReference<float>>) GetParameters(object obj)
    {
        List<ParamReference<float>> parameters = new List<ParamReference<float>>();

        var type = obj.GetType();
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var field in fields)
        {
            var attribute = field.GetCustomAttribute<Parameter>();
            if (attribute != null)
            {
                string name = string.IsNullOrEmpty(attribute.name) ? field.Name : attribute.name;

                object value = field.GetValue(obj);
                if (value is float)
                {
                    if (field.GetCustomAttribute<RangeAttribute>() != null)
                    {
                        var range = field.GetCustomAttribute<RangeAttribute>();
                        parameters.Add(new FloatParamReference(obj, field, name, range.min, range.max));
                    }
                    else
                    {
                        parameters.Add(new FloatParamReference(obj, field, name));
                    }
                }
            }
        }
        return (type.Name, parameters);
    }

    public void GetParameters()
    {
        m_parameters.Clear();

        List<(string, List<LocalObjective>)> objectives = m_auit.GetLocalObjectives();
        
        foreach ((string obj, List<LocalObjective> objs) in objectives)
        {
            List<(string, List<ParamReference<float>>)> objParams = new List<(string, List<ParamReference<float>>)>();
            foreach (var o in objs)
            {
                (string oName, List<ParamReference<float>> oParams) = GetParameters(o);
                objParams.Add((oName, oParams));
            }
            m_parameters.Add((obj, objParams));
        }

    }

    public List<ParamReference<float>> GetParametersAll()
    {
        if (m_parameters.Count <= 0)
        {
            GetParameters();
        }
        List<ParamReference<float>> parameters = new List<ParamReference<float>>();
        foreach ((string objName, List<(string, List<Parameters.ParamReference<float>>)> obj) in m_parameters)
        {
            foreach ((string oName, List<Parameters.ParamReference<float>> o) in obj)
            {
                foreach (var parameter in o)
                {
                    parameters.Add(parameter);
                }
            }
        }
        return parameters;
    }

    public List<(string, List<(string, List<string>)>)> GetParametersInfo()
    {
        List<(string, List<(string, List<string>)>)> parameters = new List<(string, List<(string, List<string>)>)>();
        foreach ((string objName, List<(string, List<Parameters.ParamReference<float>>)> obj) in m_parameters)
        {
            List<(string, List<string>)> objParams = new List<(string, List<string>)>();
            foreach ((string oName, List<Parameters.ParamReference<float>> o) in obj)
            {
                List<string> paramNames = new List<string>();
                foreach (var parameter in o)
                {
                    paramNames.Add(parameter.name);
                }
                objParams.Add((oName, paramNames));
            }
            parameters.Add((objName, objParams));
        }
        return parameters;
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

        foreach ((string objName, List<(string, List<Parameters.ParamReference<float>>)> obj) in parameters.Params)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(objName, EditorStyles.boldLabel);
            foreach ((string oName, List<Parameters.ParamReference<float>> o) in obj)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField(oName, EditorStyles.miniBoldLabel);
                foreach (var parameter in o)
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
    }
}
