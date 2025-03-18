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
using System.Linq;
using UnityEngine.UIElements;

public class PolicyView : MonoBehaviour
{
    public ParaHomeLoader m_paraHomeLoader;
    public Parameters m_parameters;
    public AUIT.AUIT auit;
    public SingleAttributeControllers m_sacs;
    public Camera m_camera;

    public int m_numSamples = 100;

    public bool m_enableHovering = false;

    //public int m_numSamples = 4;
    //public float m_solver_interval = 0.1f; // todo: use ref


    //private IAsyncSolver solver = new ExhaustiveSearchSolver();

    //[SerializeField]
    //private List<Constraint> constraints;

    private NDarray m_samples;
    private List<ParaHomeContext> m_contexts = new List<ParaHomeContext>();
    private int m_currentContext = -1;
    private List<Layout[][]> m_layouts = new List<Layout[][]>();
    private List<Element[]> m_currentLayouts = new List<Element[]>();
    private int m_hoverIndex = -1;

    private NDarray m_filteredSamples; 
    private List<Layout[][]> m_filteredLayouts = new List<Layout[][]>();
    private List<Element[]> m_filteredElements = new List<Element[]>();

    private int m_numParameters;


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
        if (m_hoverIndex == hoverIndex)
        {
            return;
        }

        m_hoverIndex = hoverIndex;

        SetHover();
    }

    public void ApplyFiltering(int pi, float min, float max)
    {
        var parameterValues = m_samples[$":,{pi}"];
        var sampleMask = (parameterValues >= min) & (parameterValues <= max);
        var filteredMask = ~sampleMask;

        var samples = m_samples[sampleMask, ":"];
        var filteredSamples = m_samples[filteredMask, ":"]; 
        if (m_filteredSamples == null)
        {
            m_filteredSamples = filteredSamples;
        } else
        {
            m_filteredSamples = np.concatenate(new NDarray[] { m_filteredSamples, filteredSamples });
        }
            m_samples = samples;
        Debug.Log($"ApplyFiltering(): {min}, {max}, {m_samples.shape}, {m_filteredSamples.shape}");
        
        int[] sampleIndices = np.nonzero(sampleMask)[0].GetData<int>();
        int[] filteredIndices = np.nonzero(filteredMask)[0].GetData<int>();

        List<Layout[][]> sampleLayouts = new List<Layout[][]>();
        foreach (Layout[][] layout in m_layouts)
        {
            Layout[][] contextSampleLayouts = new Layout[sampleIndices.Length][];
            int si = 0;
            foreach (int sampleIndex in sampleIndices)
            {
                contextSampleLayouts[si++] = layout[sampleIndex];
            }
            sampleLayouts.Add(contextSampleLayouts);
        }

        List<Layout[][]> filteredLayouts = new List<Layout[][]>();
        for (int ci = 0; ci < m_layouts.Count; ci++)
        {
            int numFiltered = filteredIndices.Length;
            if (m_filteredLayouts.Count > ci)
            {
                numFiltered += m_filteredLayouts[ci].Length;
            }
            Layout[][] contextfilteredLayouts = new Layout[numFiltered][];
            int si = 0;
            if (m_filteredLayouts.Count > ci)
            {
                for (si = 0; si < m_filteredLayouts[ci].Length; si++)
                {
                    contextfilteredLayouts[si] = m_filteredLayouts[ci][si];
                }
            }
            foreach (int filteredIndx in filteredIndices)
            {
                contextfilteredLayouts[si++] = m_layouts[ci][filteredIndx];
            }
            filteredLayouts.Add(contextfilteredLayouts);
        }
        m_filteredLayouts = filteredLayouts;
        m_layouts = sampleLayouts;


        LoadContext();
        m_sacs.SetValues(m_samples);
        m_sacs.SetSACMinMax(pi, min, max);
    }

    public void ResetFiltering()
    {
        if (m_filteredSamples != null)
        {
            m_samples = np.concatenate(new NDarray[] { m_samples, m_filteredSamples }, axis: 0);
        }
        m_filteredSamples = null;

        for (int ci = 0; ci < m_layouts.Count; ci++)
        {
            if (m_filteredLayouts.Count > ci)
            {
                Layout[][] layouts = m_layouts[ci];
                Layout[][] filteredLayouts = m_filteredLayouts[ci];   
                int numLayouts = layouts.Length + filteredLayouts.Length;
                Layout[][] combined = new Layout[numLayouts][];
                int si = 0;
                for (int i = 0; i < layouts.Length; i++)
                {
                    combined[si++] = layouts[i];
                }
                for (int i = 0; i < filteredLayouts.Length; i++)
                {
                    combined[si++] = filteredLayouts[i];
                }
                m_layouts[ci] = combined;
            }
        }
        m_filteredLayouts.Clear();
        /*
        Debug.Log($"ResetFiltering(): {m_filteredLayouts.Count}, {m_layouts.Count}");
        if (m_filteredLayouts != null)
        {
            foreach (Layout[][] layout in m_filteredLayouts)
            {
                m_layouts.Add(layout);
            }
        }
        m_filteredLayouts.Clear();
        Debug.Log($"ResetFiltering(): {m_layouts.Count}");
        */

        LoadContext();
        m_sacs.SetValues(m_samples);
    }

    public void SetSelected(int si)
    {
        string[] selectedParams = m_parameters.GetParametersInfoFlat().ToArray();

        string info = "Selected:\n";
        for (int pi = 0; pi < selectedParams.Length; pi++)
        {
            info += $"{selectedParams[pi]}: {(float)m_samples[si, pi]}\n";
        }
        Debug.Log(info);
    }

    public async void SamplePolicies()
    {
        ClearSampledResults();
        m_layouts.Clear();
        m_filteredSamples = null;
        m_filteredLayouts.Clear();


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
        m_numParameters = parameters.Count;
        if (m_numParameters == 0)
        {
            Debug.Log("PolicyView.SamplePolicies(): No parameters to sample");
            return;
        }

        m_samples = np.random.rand(m_numSamples, m_numParameters);
        for (int i = 0; i < m_numParameters; i++)
        {
            Parameters.ParamReference<float> parameter = parameters[i];
            float min = 0;
            float max = 1;
            min = ((Parameters.FloatParamReference)parameter).min;
            max = ((Parameters.FloatParamReference)parameter).max;

            m_samples[":",i] = min + (max - min) * m_samples[":", i];
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
                for (int pi = 0; pi < m_numParameters; pi++)
                {
                    Parameters.ParamReference<float> parameter = parameters[pi];
                    float value = (float)m_samples[si, pi];
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
        
        
        m_sacs.SetValues(m_samples);

        m_sacs.onHover += SetHover;
        m_sacs.onSelect += SetSelected;
        m_sacs.onApplyFiltering += ApplyFiltering;

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


    private void HandleHovering()
    {
        if (!Application.isFocused)
        {
            return;
        }
        if (!m_enableHovering)
        {
            return;
        }
        Ray ray = m_camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        // Get Element layer mask
        int layerMask = 1 << LayerMask.NameToLayer("Element");
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
        {
            Element element = hit.transform.GetComponent<Element>();
            if (element != null)
            {
                for (int ei = 0; ei < m_currentLayouts.Count; ei++)
                {
                    Element[] elements = m_currentLayouts[ei];
                    if (elements.Contains(element))
                    {
                        SetHover(ei);
                        m_sacs.SetHoverSACs(ei);
                        return;
                    }
                }
            }
        }
        SetHover(-1);
        m_sacs.SetHoverSACs(-1);
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
        HandleHovering();
    }
    
}

[CustomEditor(typeof(PolicyView))]
public class PolicyViewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        PolicyView policyView = (PolicyView)target;

        EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
        policyView.m_paraHomeLoader = (ParaHomeLoader)EditorGUILayout.ObjectField("ParaHomeLoader", policyView.m_paraHomeLoader, typeof(ParaHomeLoader), true);
        policyView.m_parameters = (Parameters)EditorGUILayout.ObjectField("Parameters", policyView.m_parameters, typeof(Parameters), true);
        policyView.auit = (AUIT.AUIT)EditorGUILayout.ObjectField("AUIT", policyView.auit, typeof(AUIT.AUIT), true);
        policyView.m_sacs = (SingleAttributeControllers)EditorGUILayout.ObjectField("SingleAttributeControllers", policyView.m_sacs, typeof(SingleAttributeControllers), true);
        policyView.m_camera = (Camera)EditorGUILayout.ObjectField("Camera", policyView.m_camera, typeof(Camera), true);
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
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Sampling", EditorStyles.boldLabel);
        policyView.m_numSamples = EditorGUILayout.IntField("Number of Samples", policyView.m_numSamples);
        if (GUILayout.Button("Sample Policies"))
        {
            policyView.SamplePolicies();
        }
        if (GUILayout.Button("Reset Filtering"))
        {
            policyView.ResetFiltering();
        }
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);
        policyView.m_enableHovering = EditorGUILayout.Toggle("Enable Hovering", policyView.m_enableHovering);

    }
}
