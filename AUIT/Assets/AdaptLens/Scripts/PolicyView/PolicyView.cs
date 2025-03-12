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
    public ParaHomeLoader m_paraHomeLoader;
    public Parameters m_parameters;
    public AUIT.AUIT auit;
    public SingleAttributeControllers m_sacs;


    [Header("Settings")]
    //public int m_numSamples = 4;
    //public float m_solver_interval = 0.1f; // todo: use ref
    public int m_numSamples = 100;

    //private IAsyncSolver solver = new ExhaustiveSearchSolver();

    //[SerializeField]
    //private List<Constraint> constraints;

    private List<ParaHomeContext> m_contexts = new List<ParaHomeContext>();
    private int m_currentContext = -1;
    private List<Layout[][]> m_layouts = new List<Layout[][]>();
    private List<Element[]> m_currentLayouts = new List<Element[]>();
    private int m_hoverIndex = -1;


    public int NumContexts
    {
        get { return m_contexts.Count; }
    }

    public int CurrentContext
    {
        get { return m_currentContext; }
        set { m_currentContext = value; }
    }

    public void AddContext()
    {
        if (m_paraHomeLoader == null)
        {
            Debug.LogError("PolicyView.AddContext(): ParaHomeLoader is not set");
            return;
        }
        ParaHomeScene scene = m_paraHomeLoader.CurrentScene;
        ParaHomeAvatarPose pose = m_paraHomeLoader.CurrentPose;
        if (scene == null || pose == null)
        {
            Debug.Log("PolicyView.AddContext(): Scene or Pose is null");
            return;
        }
        m_contexts.Add(new ParaHomeContext(pose, scene));
    }

    public void LoadContext()
    {
        if (m_currentContext < 0 || m_currentContext >= m_contexts.Count)
        {
            Debug.Log("PolicyView.LoadContext(): Invalid context index");
            return;
        }
        ParaHomeContext context = m_contexts[m_currentContext];
        m_paraHomeLoader.LoadSceneObjects(context.scene);
        m_paraHomeLoader.LoadScenePoses(context.pose);
        LoadSampledResults();
    }

    public void ClearContexts()
    {
        m_contexts.Clear();
        ClearSampledResults();
        m_layouts.Clear();
    }

    private void ClearSampledResults()
    {
        m_currentLayouts.Clear();
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }

    private void LoadSampledResults()
    {
        ClearSampledResults();
        if (m_currentContext < 0 || m_currentContext >= m_layouts.Count)
        {
            Debug.Log("PolicyView.LoadSampledResults(): Invalid context index");
            return;
        }

        Layout[][] layouts = m_layouts[m_currentContext];
        int numSamples = layouts.Length;
        for (int si = 0; si < numSamples; si++)
        {
            GameObject[] optimizedResult = auit.GetObjectsCopy();
            foreach (GameObject obj in optimizedResult)
            {
                obj.transform.SetParent(transform);
            }
            Layout[] elements = layouts[si];
            int numElements = elements.Length;
            Element[] optimizedElements = new Element[numElements];
            for (int ei = 0; ei < numElements; ei++)
            {
                Layout element = elements[ei];
                optimizedResult[ei].transform.position = element.Position;
                optimizedResult[ei].transform.rotation = element.Rotation;
                optimizedResult[ei].transform.localScale = element.Scale;
                optimizedElements[ei] = optimizedResult[ei].GetComponent<Element>();
                optimizedElements[ei].Init();
            }
            m_currentLayouts.Add(optimizedElements);
        }

        SetHover();
    }

    private void SetHover()
    {
        if (m_currentLayouts.Count == 0)
        {
            return;
        }

        int numElements = m_currentLayouts.Count;
        if (m_hoverIndex == -1)
        {
            foreach (Element[] elements in m_currentLayouts)
            {
                foreach (Element element in elements)
                {
                    element.SetOriginal();
                }
            }
        }

        else if (m_hoverIndex >= 0 && m_hoverIndex < numElements)
        {
            for (int i = 0; i < numElements; i++)
            {
                Element[] elements = m_currentLayouts[i];
                foreach (Element element in elements)
                {
                    if (i == m_hoverIndex)
                    {
                        element.SetHighlight();
                    }
                    else
                    {
                        element.SetHide();
                    }
                }
            }
        }
    }

    public void SetHover(int hoverIndex)
    {
        m_hoverIndex = hoverIndex;

        SetHover();
    }

    public async void SamplePolicies()
    {
        ClearSampledResults();
        m_layouts.Clear();

        if (m_numSamples <= 0)
        {
            Debug.Log("PolicyView.SamplePolicies(): Invalid number of samples");
            return;
        }
        if (m_contexts.Count == 0)
        {
            Debug.Log("PolicyView.SamplePolicies(): No contexts to sample");
            return;
        }
        if (m_parameters == null)
        {
            Debug.Log("PolicyView.SamplePolicies(): Parameters is not set");
            return;
        }

        List<Parameters.ParamReference<float>> parameters = m_parameters.GetParametersAll();
        int numParameters = parameters.Count;
        if (numParameters == 0)
        {
            Debug.Log("PolicyView.SamplePolicies(): No parameters to sample");
            return;
        }

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

        int numContexts = m_contexts.Count;
        for (int ci = 0; ci < numContexts; ci++)
        {
            ParaHomeContext context = m_contexts[ci];
            ParaHomeScene scene = context.scene;
            ParaHomeAvatarPose pose = context.pose;
            m_paraHomeLoader.LoadSceneObjects(scene);
            m_paraHomeLoader.LoadScenePoses(pose);

            Layout[][] layouts = new Layout[m_numSamples][];
            for (int si = 0; si < m_numSamples; si++)
            {
                for (int pi = 0; pi < numParameters; pi++)
                {
                    Parameters.ParamReference<float> parameter = parameters[pi];
                    float value = (float)samples[si, pi];
                    parameter.Value = value;
                }
                OptimizationResponse response = await auit.OptimizeLayout();

                Layout[] elements = response.suggested.elements;
                int numElements = elements.Length;
                layouts[si] = new Layout[numElements];
                for (int ei = 0; ei < numElements; ei++)
                {
                    Layout element = elements[ei];
                    layouts[si][ei] = element;
                }
            }
            m_layouts.Add(layouts);
        }
        
        LoadContext();

        m_sacs.Init(m_parameters.GetParametersInfo());
        m_sacs.SetValues(samples);
        m_sacs.onHover += SetHover;

        // Single attribute controllers


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

        EditorGUILayout.Space();
        // label
        EditorGUILayout.LabelField("Contexts", EditorStyles.boldLabel);
        if (GUILayout.Button("Add Context"))
        {
            policyView.AddContext();
        }
        if (GUILayout.Button("Clear Contexts"))
        {
            policyView.ClearContexts();
        }
        if (policyView.NumContexts > 1)
        {
            EditorGUI.BeginChangeCheck();
            policyView.CurrentContext = EditorGUILayout.IntSlider("Context", policyView.CurrentContext, 0, policyView.NumContexts - 1);
            if (EditorGUI.EndChangeCheck())
            {
                policyView.LoadContext();
            }
        }
    }
}
