using System.Collections.Generic;
using AUIT.AdaptationObjectives;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.Constraints;
using AUIT.Solvers;
using UnityEngine;
using Numpy;
using UnityEditor;
using AUIT.Extras;
using AUIT;

public class PolicyView : MonoBehaviour
{
    [Header("References")]
    public Parameters m_parameters;
    public AUIT.AUIT auit;

    [Header("Settings")]
    //public int m_numSamples = 4;
    //public float m_solver_interval = 0.1f; // todo: use ref
    public int m_numSamples = 100;

    //private IAsyncSolver solver = new ExhaustiveSearchSolver();

    //[SerializeField]
    //private List<Constraint> constraints;
    
    public async void SamplePolicies()
    {
        List<Parameters.ParamReference<float>> parameters = m_parameters.GetParametersAll();
        int numParameters = parameters.Count;
        NDarray samples = np.random.rand(m_numSamples, numParameters);
        for (int i = 0; i < numParameters; i++)
        {
            Parameters.ParamReference<float> parameter = parameters[i];
            float min = 0;
            float max = 1;
            min = ((Parameters.FloatParamReference)parameter).min;
            max = ((Parameters.FloatParamReference)parameter).max;

            samples[":",i] = min + (max - min) * samples[":", i];
        }

        for (int i = 0; i < m_numSamples; i++)
        {
            for (int j = 0; j < numParameters; j++)
            {
                Parameters.ParamReference<float> parameter = parameters[j];
                parameter.Value = (float)samples[i, j];
            }
            OptimizationResponse response = await auit.OptimizeLayout();
            GameObject[] optimizedResult = auit.GetObjectsCopy();
            Layout[] elements = response.suggested.elements;
            int numElements = elements.Length;
            for (int e = 0; e < numElements; e++)
            {
                Layout element = elements[e];
                optimizedResult[e].transform.position = element.Position;
                optimizedResult[e].transform.rotation = element.Rotation;
                optimizedResult[e].transform.localScale = element.Scale;
            }
        }
        


        /*
        List<Parameters.ParamReference<float>> parameters = m_parameters.GetParameters();
        List<Parameters.ParamReference<float>> weights = new List<Parameters.ParamReference<float>>();
        foreach (var p in parameters)
        {
            if (p.name.Contains("Weight")) // only care about weights now
                weights.Add(p);
        }
        NDarray[] linRange = new NDarray[weights.Count];

        foreach (Parameters.ParamReference<float> weight in weights)
        {
            if (weight is Parameters.FloatParamReference fweight)
            {
                var vals = np.linspace(fweight.min, fweight.max, m_numSamples);
                linRange[weights.IndexOf(weight)] = vals;
            }
            else
            {
                throw new System.Exception("Policy View only supports floats at the moment");
            }
        }
        
        print("Discretizing...");
        NDarray discretization = np.array(np.meshgrid(linRange, indexing: "ij")).T.reshape(-1, weights.Count);
        
        print("Invoking solver...");
        (List<List<LocalObjective>> objectives, List<Layout> layouts) = auit.gatherOptimizationData();
        (_, NDarray points, NDarray costs) = await solver.OptimizeCoroutine(layouts, objectives, true);
        
        NDarray weightCombinations = np.empty((discretization.shape[0], weights.Count));
        for (int i = 0; i < discretization.shape[0]; i++)
        {
            weightCombinations[$"{i},:"] = discretization[i];
        }
        costs = costs.T;

        NDarray result = np.matmul(weightCombinations, costs);
        print(result.shape);
        */

        // for (int i = 0; i < points.shape[0]; ++i)
        // {
        //     print(points[i] + " " + costs[i]);
        // }


        // ;
        //
        // List<Parameters.ParamReference<float>> parameters = m_parameters.GetParameters();
        //
        // int numParameters = parameters.Count;
        //
        // float[,] values = new float[numParameters, m_numSamples];
        // for (int pi = 0; pi < numParameters; pi++)
        // {
        //     Parameters.ParamReference<float> parameter = parameters[pi];
        //     if (parameter is Parameters.FloatParamReference)
        //     {
        //         Parameters.FloatParamReference floatParameter = (Parameters.FloatParamReference)parameter;
        //         float min = floatParameter.min;
        //         float max = floatParameter.max;
        //         (NDarray parameterValues, float num) = np.linspace(np.array(min), np.array(max), m_numSamples);
        //         for (int si = 0; si < m_numSamples; si++)
        //         {
        //             values[pi, si] = (int)parameterValues[si];
        //         }
        //     }
        // }

        // TODO: Compute optimal results given samples

    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //solver.Initialize(constraints);
        //((ExhaustiveSearchSolver)solver).interval = m_solver_interval;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
}

[CustomEditor(typeof(PolicyView))]
public class PolicyViewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PolicyView policyView = (PolicyView)target;
        if (GUILayout.Button("Sample Policies"))
        {
            policyView.SamplePolicies();
        }
    }
}
