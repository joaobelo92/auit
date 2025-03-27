using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;
using Numpy;
using UnityEditor;
using AUIT.Extras;
using System.Linq;
using System.Collections;
using System;
using AUIT.Solvers;

public class PolicyView : MonoBehaviour
{
    public ParaHomeLoader m_paraHomeLoader;
    public Parameters m_parameters;
    public AUIT.AUIT auit;
    public SingleAttributeControllers m_sacs;
    public GalleryView m_gallery;
    public Camera m_userCamera;
    public Camera m_supportCamera;

    public enum SamplingApproach
    {
        Random,
        Interval
    }
    public SamplingApproach m_samplingApproach = SamplingApproach.Interval;
    public int m_numSamples = 10;
    public float m_increment = 0.2f;
    public bool m_initializePlacement = false;

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

    private int m_selected = -1;

    private List<int> m_saved = new List<int>();
    private List<int> m_filteredSaved = new List<int>();

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

    private void SetContexts(List<ParaHomeContext> contexts)
    {
        m_contexts = contexts;
        if (m_contexts.Count > 0)
        {
            m_currentContext = 0;
        }
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
        SetSelected();
        UpdateGallerySaved();
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
                GameObject resultObj = optimizedResult[ei];
                resultObj.SetActive(true);
                resultObj.transform.position = element.Position;
                resultObj.transform.rotation = element.Rotation;
                resultObj.transform.localScale = element.Scale;
                Element resultElement = resultObj.GetComponent<Element>();
                resultElement.Init();
                optimizedElements[ei] = resultElement;
            }
            m_currentLayouts.Add(optimizedElements);
        }

        //SetHover();
    }

    private void SetHover()
    {
        m_sacs.SetHoverSACs(m_hoverIndex);

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

    public void SetHoverSelected(bool hover)
    {
        if (hover)
        {
            m_hoverIndex = m_selected;
        }
        else
        {
             m_hoverIndex = -1; 
        }
        SetHover();
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

    public void SetHoverSaved(int savedIndex)
    {
        int hoverIndex = -1; 
        if (savedIndex >= 0)
        {
            hoverIndex = m_saved[savedIndex];
        }
        SetHover(hoverIndex);

    }

    public void ApplyFiltering(int pi, float min, float max)
    {
        var parameterValues = m_samples[":", pi];
        var sampleMask = (parameterValues >= min) & (parameterValues <= max);
        var filteredMask = ~sampleMask;
        // Identify sample versus filtered out indices 
        int[] sampleIndices = np.nonzero(sampleMask)[0].astype(np.int32).GetData<int>();
        int[] filteredIndices = np.nonzero(filteredMask)[0].astype(np.int32).GetData<int>();

        // Identify samples versus filtered out values
        var samples = m_samples[sampleMask, ":"];
        var filteredSamples = m_samples[filteredMask, ":"];

        int numFilteredCurrent = 0;
        int numFilteredNew = filteredIndices.Length;
        if (m_filteredSamples == null)
        {
            m_filteredSamples = filteredSamples;
        } else
        {
            numFilteredCurrent = m_filteredSamples.shape[0];
            m_filteredSamples = np.concatenate(new NDarray[] { m_filteredSamples, filteredSamples });
        }
        int numFiltered = numFilteredCurrent + numFilteredNew;
        m_samples = samples;

        // Identify sample versus filtered out layouts
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

        // Update selected
        m_selected = Array.IndexOf(sampleIndices, m_selected);

        // Update saved
        List<int> saved = new List<int>();
        foreach (int savedIndex in m_saved)
        {
            if (Array.IndexOf(sampleIndices, savedIndex) >= 0)
            {
                saved.Add(Array.IndexOf(sampleIndices, savedIndex));
            } else
            {
                m_filteredSaved.Add(Array.IndexOf(filteredIndices, savedIndex) + numFilteredCurrent);
            }
        }
        m_saved = saved;

        LoadContext();
        m_sacs.SetValues(m_samples);
        m_sacs.SetSACMinMax(pi, min, max);
    }

    public void ResetFiltering()
    {
        int numSamples = m_samples.shape[0];
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

        foreach (int savedIndex in m_filteredSaved)
        {
            m_saved.Add(savedIndex + numSamples);
        }
        m_filteredSaved.Clear();


        LoadContext();
        m_sacs.SetValues(m_samples);
    }

    private Texture2D CaptureView()
    {
        m_supportCamera.gameObject.SetActive(true);
        m_supportCamera.transform.SetPositionAndRotation(m_userCamera.transform.position, m_userCamera.transform.rotation);

        RenderTexture currentRT = RenderTexture.active;

        RenderTexture.active = m_supportCamera.targetTexture;

        m_supportCamera.Render();
        Texture2D snapshot = new Texture2D(m_supportCamera.targetTexture.width, m_supportCamera.targetTexture.height);
        snapshot.ReadPixels(new Rect(0, 0, m_supportCamera.targetTexture.width, m_supportCamera.targetTexture.height), 0, 0);
        snapshot.Apply();

        RenderTexture.active = currentRT;

        m_supportCamera.gameObject.SetActive(false);

        return snapshot;
    }

    public IEnumerator GetLayoutView(int targetLayout, System.Action<Texture2D> callback)
    {
        for (int li = 0; li < m_currentLayouts.Count; li++)
        {
            foreach (Element element in m_currentLayouts[li])
            {
                
                element.gameObject.SetActive(li == targetLayout);
            }
        }

        yield return new WaitForEndOfFrame();

        Texture2D view = CaptureView();

        for (int li = 0; li < m_currentLayouts.Count; li++)
        {
            foreach (Element element in m_currentLayouts[li])
            {
                element.gameObject.SetActive(true);
            }
            
        }

        callback(view);
    }

    public IEnumerator GetLayoutViews(System.Action<List<Texture2D>> callback)
    {
        List<Texture2D> views = new List<Texture2D>();

        yield return new WaitForEndOfFrame();

        foreach (int saved in m_saved)
        {
            bool captured = false;
            Texture2D capturedView = null; 

            yield return StartCoroutine(GetLayoutView(saved, (texture) =>
            {
                captured = true;
                capturedView = texture;
            }));

            yield return new WaitUntil(() => captured);

            views.Add(capturedView);
        }

        callback(views);
    }

    private void SetSelected()
    {
        // Update SACs 
        m_sacs.SetSelectedSACS(m_selected);

        // Update Gallery view
        if (m_selected < 0)
        {
            m_gallery.ResetSelected();
            return; 
        }
        string[] selectedParams = m_parameters.GetParametersInfoFlat().ToArray();

        string info = "Selected:\n";
        for (int pi = 0; pi < selectedParams.Length; pi++)
        {
            info += $"{selectedParams[pi]}: {(float)m_samples[m_selected, pi]}\n";
        }

        StartCoroutine(GetLayoutView(m_selected, (view) =>
        {
            m_gallery.SetSelected(view, info);
        }));
    }

    private void ResetSelected()
    {
        m_selected = -1;
        SetSelected();
    }

    private void SaveSelected()
    {
        if (m_selected >= 0 && !m_saved.Contains(m_selected))
        {
            m_saved.Add(m_selected);
        }
        UpdateGallerySaved();
    }

    private void ClearSaved()
    {
        m_saved.Clear();
        UpdateGallerySaved();
    }

    private void UpdateGallerySaved()
    {
        StartCoroutine(GetLayoutViews((views) =>
        {
            m_gallery.SetSaved(views);
        }));
    }

    public void SetSelected(int si)
    {
        m_selected = si;
        SetSelected();
    }

    public void SetSelectedSaved(int savedIndex)
    {
        int selectedIndex = -1;
        if (savedIndex >= 0)
        {
            selectedIndex = m_saved[savedIndex];
        }
        SetSelected(selectedIndex);
    }

    public async void SamplePolicies()
    {
        DateTime tsStart = DateTime.Now;
        ClearSaved();
        ClearSampledResults();
        m_layouts.Clear();
        m_filteredSamples = null;
        m_filteredLayouts.Clear();
        m_selected = -1;

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

        m_parameters.GetParameters();
        List<Parameters.ParamReference<float>> parameters = m_parameters.GetParametersAll();
        m_numParameters = parameters.Count;
        if (m_numParameters == 0)
        {
            Debug.Log("PolicyView.SamplePolicies(): No parameters to sample");
            return;
        }

        switch (m_samplingApproach)
        {
            case SamplingApproach.Interval:
                m_samples = IntervalSampling.GenerateSamples(m_increment, m_numParameters);
                m_samples = np.concatenate(new NDarray[] { m_samples, m_samples }, axis: 0);
                m_numSamples = m_samples.shape[0];
                m_samples -= m_increment * np.random.rand(m_numSamples, m_numParameters);
                m_samples = np.clip(m_samples, np.array(0), np.array(1));
                break;
            case SamplingApproach.Random:
                m_samples = np.random.rand(m_numSamples, m_numParameters);
                for (int i = 0; i < m_numParameters; i++)
                {
                    Parameters.ParamReference<float> parameter = parameters[i];
                    float min = 0;
                    float max = 1;
                    min = ((Parameters.FloatParamReference)parameter).min;
                    max = ((Parameters.FloatParamReference)parameter).max;

                    m_samples[":", i] = min + (max - min) * m_samples[":", i];
                }
                
                
                break;
        }

        if (m_numParameters > 1)
        {
            var row_sums = np.sum(m_samples, axis: 1, keepdims: true);
            m_samples = m_samples / row_sums;
        }

        Debug.Log($"Sampling Approach: {m_samplingApproach}" +
                    $"Increment: {m_increment}\n" +
                    $"Num parameters {m_numParameters}\n" +
                    $"Shape: {m_samples.shape}");

        int numContexts = m_contexts.Count;
        for (int ci = 0; ci < numContexts; ci++)
        {
            ParaHomeContext context = m_contexts[ci];
            ParaHomeScene scene = context.scene;
            ParaHomeAvatarPose pose = context.pose;
            m_paraHomeLoader.LoadSceneObjects(scene);
            m_paraHomeLoader.LoadScenePoses(pose);

            // Initialize placement to in front of user camera
            if (m_initializePlacement)
            {
                foreach (GameObject obj in auit.gameObjectsToOptimize)
                {
                    obj.transform.position = m_userCamera.transform.position + m_userCamera.transform.forward;
                    obj.transform.rotation = Quaternion.LookRotation(-m_userCamera.transform.forward);
                }
            }


            Layout[][] layouts = new Layout[m_numSamples][];
            for (int si = 0; si < m_numSamples; si++)
            {
                for (int pi = 0; pi < m_numParameters; pi++)
                {
                    Parameters.ParamReference<float> parameter = parameters[pi];
                    float value = (float)m_samples[si, pi];
                    parameter.Value = value;
                }

                // Call to solver
                OptimizationResponse response = await auit.OptimizeLayout();
                
                List<float> multiObjectiveCosts;
                List<List<float>> objectiveCosts;
                (objectiveCosts, multiObjectiveCosts) = Utils.ComputeCosts(response.suggested.elements.ToList(), 
                    auit.gatherOptimizationData().objectives, auit.MultiElementObjectives);

                // In case you wish to print the costs:
                // Debug.Log("multiObjectiveCosts:");
                // foreach (float cost in multiObjectiveCosts)
                // {
                //     Debug.Log(cost);
                // }
                //
                // Debug.Log("objectiveCosts:");
                // for (int i = 0; i < objectiveCosts.Count; i++)
                // {
                //     string row = $"Element {i}: ";
                //     foreach (float cost in objectiveCosts[i])
                //     {
                //         row += cost + " ";
                //     }
                //     Debug.Log(row);
                // }

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

        Debug.Log((DateTime.Now - tsStart).TotalSeconds);

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
        Ray ray = m_userCamera.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(m_userCamera.transform.position, ray.direction * 500, Color.yellow, Time.deltaTime);
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

                        // Support selection on mouse click
                        if (Input.GetMouseButtonDown(0))
                        {
                            SetSelected(ei);
                        }


                        return;
                    }
                }
            }
        }
        SetHover(-1);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //solver.Initialize(constraints);
        //((ExhaustiveSearchSolver)solver).interval = m_solver_interval;

        m_gallery.onSaveSelected += SaveSelected;
        m_gallery.onClearSelected += ResetSelected;
        m_gallery.onClearSaved += ClearSaved;
        m_gallery.onHoverSelected += SetHoverSelected;
        m_gallery.onHoverSaved += SetHoverSaved;
        m_gallery.onSelectedSaved += SetSelectedSaved;
    }

    // Update is called once per frame
    void Update()
    {
        HandleHovering();
    }

    private void OnEnable()
    {
        m_paraHomeLoader.onScenesLoaded += SetContexts;
    }

    private void OnDisable()
    {
        m_paraHomeLoader.onScenesLoaded -= SetContexts;
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
        policyView.m_sacs = (SingleAttributeControllers)EditorGUILayout.ObjectField("Single Attribute Controllers", policyView.m_sacs, typeof(SingleAttributeControllers), true);
        policyView.m_gallery = (GalleryView)EditorGUILayout.ObjectField("Gallery View", policyView.m_gallery, typeof(GalleryView), true);
        policyView.m_userCamera = (Camera)EditorGUILayout.ObjectField("User Camera", policyView.m_userCamera, typeof(Camera), true);
        policyView.m_supportCamera = (Camera)EditorGUILayout.ObjectField("support Camera", policyView.m_supportCamera, typeof(Camera), true);
        EditorGUILayout.Space();

        // label
        EditorGUILayout.LabelField("Contexts", EditorStyles.boldLabel);
        /*
        if (GUILayout.Button("Add Context"))
        {
            policyView.AddContext();
        }
        if (GUILayout.Button("Clear Contexts"))
        {
            policyView.ClearContexts();
        }
        */
        if (policyView.NumContexts > 1)
        {
            EditorGUI.BeginChangeCheck();
            policyView.CurrentContext = EditorGUILayout.IntSlider("Context", policyView.CurrentContext, 0, policyView.NumContexts - 1);
            if (EditorGUI.EndChangeCheck())
            {
                policyView.LoadContext();
            }
        } else
        {
            EditorGUILayout.LabelField("No contexts loaded");
        }
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Sampling", EditorStyles.boldLabel);
        policyView.m_samplingApproach = (PolicyView.SamplingApproach)EditorGUILayout.EnumPopup("Sampling Approach", policyView.m_samplingApproach);
        switch (policyView.m_samplingApproach)
        {
            case PolicyView.SamplingApproach.Random:
                policyView.m_numSamples = EditorGUILayout.IntField("Number of Samples", policyView.m_numSamples);
                break;
            case PolicyView.SamplingApproach.Interval:
                policyView.m_increment = EditorGUILayout.FloatField("Increment", policyView.m_increment);
                break;
        }
        
        policyView.m_initializePlacement = EditorGUILayout.Toggle("Initialize Placement", policyView.m_initializePlacement);
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
