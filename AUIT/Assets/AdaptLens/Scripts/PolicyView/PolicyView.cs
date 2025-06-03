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
using Cysharp.Threading.Tasks;
using AUIT.AdaptationObjectives;

public class PolicyView : MonoBehaviour
{
    public delegate void OnHover(int si, List<List<LocalObjective>> localObjectives, List<MultiElementObjective> m_multiElementObjectives, NDarray weights); 
    public OnHover onHover;

    public delegate void OnSelect(int si, List<List<LocalObjective>> localObjectives, List<MultiElementObjective> m_multiElementObjectives, NDarray weights);
    public OnSelect onSelect;

    public delegate void OnSave(int si, List<List<LocalObjective>> localObjectives, List<MultiElementObjective> m_multiElementObjectives, NDarray weights);
    public OnSave onSave;

    public delegate void OnFilter(int pi, float min, float max, PolicyView.SACValues filterValue, List<List<LocalObjective>> localObjectives, List<MultiElementObjective> multiElementObjectives);
    public OnFilter onFilter;

    public delegate void OnChangedScene(int i);
    public OnChangedScene onChangedScene;

    public ParaHomeLoader m_paraHomeLoader;
    public Parameters m_parameters;
    public AUIT.AUIT auit;
    public SingleAttributeControllers m_sacs;
    public GalleryView m_gallery;
    public Camera m_userCamera;
    public Camera m_supportCamera;
    public Transform m_boundaries;

    public enum SamplingApproach
    {
        Random,
        Interval,
        Uniform
    }
    public SamplingApproach m_samplingApproach = SamplingApproach.Interval;
    public int m_numSamples = 10;
    public float m_increment = 0.2f;

    public bool m_enableHovering = false;

    public bool m_excludeOutOfBounds = true;

    public bool m_debugProgress = true;
    public int m_debugProgressPrintInterval = 10;

    public enum SACValues
    {
        AverageCost,
        PerContextCost,
        MaxCost,
        ParameterValues
    }
    public SACValues m_sacValues = SACValues.AverageCost;

    private class FilterOperation
    {
        public int pi;
        public float min;
        public float max;
        public SACValues filterValue;
        public FilterOperation(int pi, float min, float max, SACValues filterValue)
        {
            this.pi = pi;
            this.min = min;
            this.max = max;
            this.filterValue = filterValue;
        }
    }
    private List<ParaHomeContext> m_contexts = new List<ParaHomeContext>();

    private NDarray m_samples;
    private NDarray m_mask;
    private List<Layout[][]> m_layouts = new List<Layout[][]>();
    private NDarray m_costs;
    
    private Stack<FilterOperation> m_filterStack = new Stack<FilterOperation>();

    private List<Element[]> m_currentLayouts = new List<Element[]>();

    // Cache for logging purposes
    private List<List<AUIT.AdaptationObjectives.LocalObjective>> m_localObjectives;
    private List<AUIT.AdaptationObjectives.MultiElementObjective> m_multiElementObjectives;



    private int m_currentContext = -1;
    private int m_hoverIndex = -1;
    private int m_selected = -1;

    private List<int> m_saved = new List<int>();

    private int m_numParameters;


    public int NumContexts
    {
        get { 
            if (m_contexts == null)
            {
                return 0;
            }
            return m_contexts.Count; 
        }
    }

    public int CurrentContext
    {
        get { return m_currentContext; }
        set { m_currentContext = value; }
    }

    private void SetContexts(List<ParaHomeContext> contexts)
    {
        m_contexts = contexts;
        if (m_contexts != null && m_contexts.Count > 0)
        {
            m_currentContext = 0;
        } else
        {
            m_currentContext = -1;
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

    public void LoadContextUser()
    {
        if (onChangedScene != null)
        {
            onChangedScene(m_currentContext);
        }
        LoadContext();
    }

    private void LoadContext()
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
        UpdateSACs();
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

        m_currentLayouts.Clear();
        for (int si = 0; si < m_numSamples; si++)
        {
            Layout[][] sampleLayouts = m_layouts[si];
            // GameObject[] optimizedObjs = auit.GetObjectsCopy();
            GameObject[] optimizedObjs = new GameObject[sampleLayouts.Length];
            foreach (GameObject obj in optimizedObjs)
            {
                obj.transform.SetParent(transform);
            }
            Layout[] sampleLayout = sampleLayouts[m_currentContext];
            int numElements = sampleLayout.Length;
            Element[] optimizedElements = new Element[numElements];
            for (int ei = 0; ei < numElements; ei++)
            {
                Layout layoutElement = sampleLayout[ei];
                GameObject obj = optimizedObjs[ei];
                obj.SetActive(true);
                obj.transform.position = layoutElement.Position;
                obj.transform.rotation = layoutElement.Rotation;
                obj.transform.localScale = layoutElement.Scale;
                Element element = obj.GetComponent<Element>();
                element.Init();
                optimizedElements[ei] = element;
            }
            if (!(bool)m_mask[si])
            {
                foreach (Element element in optimizedElements)
                {
                    element.gameObject.SetActive(false);
                }
            }
            m_currentLayouts.Add(optimizedElements);
        }

        SetHover();
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
        int hoverIndex = -1;    
        if (hover)
        {
            hoverIndex = m_selected;
        }
        if (m_hoverIndex == hoverIndex)
        {
            return;
        }

        m_hoverIndex = hoverIndex;
        if (onHover != null && m_hoverIndex >= 0)
        {
            onHover(m_hoverIndex, m_localObjectives, m_multiElementObjectives, m_samples[m_hoverIndex]);
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
        if (onHover != null && m_hoverIndex >= 0)
        {
            onHover(m_hoverIndex, m_localObjectives, m_multiElementObjectives, m_samples[m_hoverIndex]);
        }

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
    
    private NDarray GetUpdatedFilterMask(NDarray mask, SACValues sacValue, int pi, float min, float max)
    {
        NDarray parameterValues;
        switch (sacValue)
        {
            case SACValues.AverageCost:
            default:
                parameterValues = np.mean(m_costs, axis: 1)[":", pi];
                break;
            case SACValues.MaxCost:
                parameterValues = np.max(m_costs, axis: new int[] { 1 })[":", pi];
                break;
            case SACValues.PerContextCost:
                parameterValues = m_costs[":", m_currentContext, pi];
                break;
            case SACValues.ParameterValues:
                parameterValues = m_samples[":", pi];
                break;
        }
        var sampleMask = (parameterValues >= min) & (parameterValues <= max);
        return mask & sampleMask;
    }

    public void ApplyFiltering(int pi, float min, float max)
    {
        if (onFilter != null)
        {
            onFilter(pi, min, max, m_sacValues, m_localObjectives, m_multiElementObjectives);
        }
        // Currently filtering based on average
        m_mask = GetUpdatedFilterMask(m_mask, m_sacValues, pi, min, max);

        // Update selected 
        if (m_selected >= 0 && !(bool)m_mask[m_selected])
        {
            m_selected = -1;
        }

        LoadContext();
        m_sacs.SetSACMinMax(pi, min, max);

        // Save to filter stack 
        m_filterStack.Push(new FilterOperation(pi, min, max, m_sacValues));
    }

    public void UndoFiltering()
    {
        if (m_filterStack.Count == 0)
        {
            return;
        }
        // Pop last operation
        m_filterStack.Pop();

        // Calculate mask without previous operation
        FilterOperation[] filterOperations = m_filterStack.ToArray();
        NDarray mask = np.ones(m_numSamples).astype(np.bool_);
        foreach (FilterOperation filterOperation in filterOperations)
        {
            mask = GetUpdatedFilterMask(mask, filterOperation.filterValue, filterOperation.pi, filterOperation.min, filterOperation.max);
        }
        m_mask = mask;

        // Update selected 
        if (m_selected >= 0 && !(bool)m_mask[m_selected])
        {
            m_selected = -1;
        }

        LoadContext();
    }

    public void ResetFiltering()
    {
        m_filterStack.Clear();
        m_mask = np.ones(m_numSamples).astype(np.bool_);
        LoadContext();
    }

    public void UpdateSACs()
    {
        if (m_samples == null || m_costs == null)
        {
            return;
        }
        
        NDarray visualizedValues;
        switch (m_sacValues)
        {
            case SACValues.AverageCost:
            default:
                visualizedValues = np.mean(m_costs, axis: 1);
                break;
            case SACValues.MaxCost:
                visualizedValues = np.max(m_costs, axis: new int[] { 1 });
                break;
            case SACValues.PerContextCost:
                if (m_currentContext < 0 || m_currentContext >= m_contexts.Count)
                {
                    return;
                }
                visualizedValues = m_costs[":", m_currentContext];
                break;
            case SACValues.ParameterValues:
                visualizedValues = m_samples;
                break;
        }
        m_sacs.SetValues(visualizedValues, m_mask);
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

    public IEnumerator GetLayoutView(int targetLayoutIndex, System.Action<Texture2D> callback)
    {

        for (int li = 0; li < m_currentLayouts.Count; li++)
        {
            foreach (Element element in m_currentLayouts[li])
            {
                element.gameObject.SetActive(false);
            }
        }
        Element[] targetLayout = m_currentLayouts[targetLayoutIndex];
        foreach (Element element in targetLayout)
        {
            element.gameObject.SetActive(true);
        }

        yield return new WaitForEndOfFrame();

        Texture2D view = CaptureView();

        for (int li = 0; li < m_currentLayouts.Count; li++)
        {
            bool active = (bool)m_mask[li];
            foreach (Element element in m_currentLayouts[li])
            {
                element.gameObject.SetActive(active);
            }
        }

        callback(view);
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

    public void SetSelected(int si)
    {
        m_selected = si;
        if (onSelect != null && m_selected >= 0)
        {
            onSelect(m_selected, m_localObjectives, m_multiElementObjectives, m_samples[m_selected]);
        }
        SetSelected();
    }
    private void ResetSelected()
    {
        m_selected = -1;
        SetSelected();
    }

    public IEnumerator GetLayoutViews(System.Action<List<Texture2D>> callback)
    {
        List<Texture2D> views = new List<Texture2D>();

        yield return new WaitForEndOfFrame();

        foreach (int saved in m_saved)
        {
            if (!(bool)m_mask[saved])
            {
                continue;
            }

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

    private void SaveSelected()
    {
        if (m_selected >= 0 && !m_saved.Contains(m_selected))
        {
            m_saved.Add(m_selected);

            if (onSave != null)
            {
                onSave(m_selected, m_localObjectives, m_multiElementObjectives, m_samples[m_selected]);
            }
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

    public void SetSelectedSaved(int savedIndex)
    {
        int selectedIndex = -1;
        if (savedIndex >= 0)
        {
            selectedIndex = m_saved[savedIndex];
        }
        SetSelected(selectedIndex);
    }

    private void DeploySelected()
    {
        if (m_selected >= 0 && m_currentLayouts.Count > m_selected)
        {
            // Get selected parameters
            float[] parameterValues = m_samples[m_selected, ":"].astype(np.float32).GetData<float>();
            List<Parameters.ParamReference<float>> parameters = m_parameters.GetParametersAll();
            for (int i = 0; i < m_numParameters; i++)
            {
                parameters[i].Value = parameterValues[i];
            }
        }
    }

    public async void SamplePolicies()
    {
        m_selected = -1;
        ClearSaved();
        m_filterStack.Clear();

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

        // Get sampling parameters 
        m_parameters.GetParameters();
        List<Parameters.ParamReference<float>> parameters = m_parameters.GetParametersAll();
        m_numParameters = parameters.Count;
        if (m_numParameters == 0)
        {
            Debug.Log("PolicyView.SamplePolicies(): No parameters to sample");
            return;
        }

        // Get sampling parameter values
        switch (m_samplingApproach)
        {
            case SamplingApproach.Interval:
                m_samples = IntervalSampling.GenerateSamples(m_increment, m_numParameters);

                // Apply random noise
                m_numSamples = m_samples.shape[0];
                m_samples -= m_increment * np.random.rand(m_numSamples, m_numParameters);
                m_samples = np.clip(m_samples, np.array(0), np.array(1));
                if (m_numParameters > 1)
                {
                    var row_sums = np.sum(m_samples, axis: 1, keepdims: true);
                    m_samples = m_samples / row_sums;
                }
                break;
            case SamplingApproach.Random:
                m_samples = RandomSample.UniformSampleSimplex(m_numSamples, m_numParameters);
                break;
            case SamplingApproach.Uniform:
                m_samples = np.ones(new int[] { m_numSamples, m_numParameters }).astype(np.float32);
                break;
        }

        // Initialize mask 
        m_mask = np.ones(m_numSamples).astype(np.bool_);

        int numContexts = m_contexts.Count;

        m_localObjectives = auit.GetLayoutsAndLocalObjectives().Item1;
        m_multiElementObjectives = auit.MultiElementObjectives;
        int numLocalObjectives = 0;
        foreach (List<AUIT.AdaptationObjectives.LocalObjective> objectives in m_localObjectives)
        {
            numLocalObjectives += objectives.Count;
        }

        int numMultiElementObjectives = m_multiElementObjectives.Count;
        int numObjectives = numLocalObjectives + numMultiElementObjectives;

        //int numElements = auit.gameObjectsToOptimize.Count;

        // Initialize layouts 
        m_layouts.Clear();
        for (int si = 0; si < m_numSamples; si++)
        {
            Layout[][] layouts = new Layout[numContexts][];
            m_layouts.Add(layouts);
        }

        // Initialize costs
        m_costs = np.zeros(m_numSamples, numContexts, numObjectives).astype(np.float32);
        
        // Iterate through contexts
        for (int ci = 0; ci < numContexts; ci++)
        {
            ParaHomeContext context = m_contexts[ci];
            ParaHomeScene scene = context.scene;
            ParaHomeAvatarPose pose = context.pose;
            m_paraHomeLoader.LoadSceneObjects(scene);
            m_paraHomeLoader.LoadScenePoses(pose);

            // Iterate through samples 
            for (int si = 0; si < m_numSamples; si++)
            {
                // Set parameters
                for (int pi = 0; pi < m_numParameters; pi++)
                {
                    Parameters.ParamReference<float> parameter = parameters[pi];
                    float value = (float)m_samples[si, pi];
                    parameter.Value = value;
                }

                // Call to solver
                OptimizationResponse response = await auit.OptimizeLayout();

                // Calculate costs
                List<List<float>> localObjectiveCosts;
                List<float> multiObjectiveCosts;
                (localObjectiveCosts, multiObjectiveCosts) = Utils.ComputeCostsUnweighted(response.suggested.elements.ToList(), 
                    auit.GetLayoutsAndLocalObjectives().Item1, auit.MultiElementObjectives);
                // si = sample 
                // ci = context 
                // costi = objective index
                int costi = 0;
                foreach (List<float> costs in localObjectiveCosts)
                {
                    foreach (float cost in costs)
                    {
                        m_costs[si, ci, costi++] = np.array(cost);
                    }
                }
                foreach (float cost in multiObjectiveCosts)
                {
                    m_costs[si, ci, costi++] = np.array(cost);
                }

                // Store layouts
                m_layouts[si][ci] = response.suggested.elements;

                if (m_debugProgress && si % m_debugProgressPrintInterval == 0)
                {
                    Debug.Log($"Context {ci + 1}/{numContexts}, Sample {si + 1}/{m_numSamples}");
                }
            }
        }
        
        // Initialize sacs for costs 
        List<(string, List<(string, List<string>)>)> objectivesInfo = new List<(string, List<(string, List<string>)>)>();
        foreach(List<AUIT.AdaptationObjectives.LocalObjective> localObjectiveObj in m_localObjectives)
        {
            string objName = "";
            List<(string, List<string>)> obj = new List<(string, List<string>)>();
            foreach (AUIT.AdaptationObjectives.LocalObjective objective in localObjectiveObj)
            {
                // Get type of objective
                obj.Add((objective.GetType().Name, new List<string>() { "" }));
                objName = objective.gameObject.name;
            }
            objectivesInfo.Add((objName, obj));
        }
        List<(string, List<string>)> multiObjInfo = new List<(string, List<string>)>();
        foreach (AUIT.AdaptationObjectives.MultiElementObjective objective in m_multiElementObjectives)
        {
            multiObjInfo.Add((objective.GetType().Name, new List<string>() { "" }));
            
        }
        objectivesInfo.Add(("global", multiObjInfo));
        m_sacs.Init(objectivesInfo);


        // Initialize context
        LoadContext();
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
                        Debug.Log(ei);
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
        m_gallery.onSaveSelected += SaveSelected;
        m_gallery.onClearSelected += ResetSelected;
        m_gallery.onClearSaved += ClearSaved;
        m_gallery.onHoverSelected += SetHoverSelected;
        m_gallery.onHoverSaved += SetHoverSaved;
        m_gallery.onSelectedSaved += SetSelectedSaved;
        m_gallery.onDeploySelected += DeploySelected;

        m_sacs.onHover += SetHover;
        m_sacs.onSelect += SetSelected;
        m_sacs.onApplyFiltering += ApplyFiltering;
        m_sacs.onResetFiltering += ResetFiltering;
        m_sacs.onUndoFiltering += UndoFiltering;
    }

    // Update is called once per frame
    void Update()
    {
        HandleHovering();
    }

    private void OnEnable()
    {
        SetContexts(m_paraHomeLoader.Contexts);
        m_paraHomeLoader.onScenesLoaded += SetContexts;
    }

    private void OnDisable()
    {
        SetContexts(new List<ParaHomeContext>());
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
        policyView.m_supportCamera = (Camera)EditorGUILayout.ObjectField("Support Camera", policyView.m_supportCamera, typeof(Camera), true);
        policyView.m_boundaries = (Transform)EditorGUILayout.ObjectField("Boundaries", policyView.m_boundaries, typeof(Transform), true);
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
                policyView.LoadContextUser();
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
            case PolicyView.SamplingApproach.Uniform:
                policyView.m_numSamples = EditorGUILayout.IntField("Number of Samples", policyView.m_numSamples);
                break;
            case PolicyView.SamplingApproach.Interval:
                policyView.m_increment = EditorGUILayout.FloatField("Increment", policyView.m_increment);
                break;
        }
        
        if (GUILayout.Button("Sample Policies"))
        {
            policyView.SamplePolicies();
        }
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);
        policyView.m_enableHovering = EditorGUILayout.Toggle("Enable Hovering", policyView.m_enableHovering);

        EditorGUI.BeginChangeCheck();
        policyView.m_sacValues = (PolicyView.SACValues)EditorGUILayout.EnumPopup("SAC Values", policyView.m_sacValues);
        if (EditorGUI.EndChangeCheck())
        {
            policyView.UpdateSACs();
        }

        policyView.m_excludeOutOfBounds = EditorGUILayout.Toggle("Exclude Out of Bounds", policyView.m_excludeOutOfBounds);
        policyView.m_debugProgress = EditorGUILayout.Toggle("Debug Progress", policyView.m_debugProgress);
        policyView.m_debugProgressPrintInterval = EditorGUILayout.IntField("Debug Progress Interval", policyView.m_debugProgressPrintInterval);
    }
}
