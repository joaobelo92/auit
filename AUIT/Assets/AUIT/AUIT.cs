using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AUIT.PropertyTransitions;
using AUIT.AdaptationTriggers;
using AUIT.AdaptationObjectives;
using AUIT.SelectionStrategies;
using AUIT.Solvers;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.Constraints;
using AUIT.Extras;
using Cysharp.Threading.Tasks;
using UnityEditor;
using Numpy;

namespace AUIT
{
    public sealed class AUIT : MonoBehaviour
    {
        public static AUIT Instance;

        public string Id { get; } = Guid.NewGuid().ToString();

        private LocalObjectiveHandler _localObjectiveHandler;
        private AdaptationTrigger _adaptationTrigger;
        private readonly List<PropertyTransition> _propertyTransitions = new();
        private readonly List<AdaptationListener> _adaptationListeners = new();

        [SerializeField] private BackendSolver backendSolver;
        private string _previousSolver;

        private IAsyncSolver _asyncSolver;

        [SerializeReference] public IAsyncSolver solverSettings;
        public bool developmentMode = true;
        private bool _waitingForOptimization;

        private bool _job;
        private UIConfiguration[] _layoutJob;
        private List<List<float>> _jobResult;

        private SelectionStrategy _selectionStrategy;

        // To be phased out for multiple layouts
        private Layout _layout;

        public List<GameObject> gameObjectsToOptimize;

        public bool m_initPlacement = true;
        public Transform m_initAnchor;
        public Vector3 m_initOffset;

        private (GameObject, LocalObjectiveHandler)[] _gameObjects;

        

        [SerializeField]
        private List<Constraint> constraints;
        public List<Constraint> GetConstraints()
        {
            return constraints;
        }

        // flag to signal that the manager has been initialized
        [NonSerialized]
        public bool initialized = false;

        // NOTE: This is where all the multi-element objectives are stored
        public List<MultiElementObjective> MultiElementObjectives { get; } = new ();
        
        #region MonoBehaviour Implementation
        
        private void AssignSolver()
        {
            Debug.Log($"Assigning solver: {backendSolver.backend}");

            switch (backendSolver.backend)
            {
                case Backend.Unity:
                    SolverUnity solver = (SolverUnity)Enum.Parse(typeof(SolverUnity), backendSolver.solver);
                    if (solver == SolverUnity.SimulatedAnnealing)
                        _asyncSolver = new SimulatedAnnealingSolver();
                    if (solver == SolverUnity.ExhaustiveSearch)
                        _asyncSolver = new ExhaustiveSearchSolver();
                    break;
                case Backend.Python:
                    _asyncSolver = new ParetoFrontierSolver();
                    break;
            }
            solverSettings = _asyncSolver;
            _previousSolver = backendSolver.solver;
        }
        
        private void InitializeSolver()
        {
            Debug.Log($"Initializing solver: {backendSolver.backend}");
            
            switch (backendSolver.backend)
            {
                case Backend.Unity:
                    _asyncSolver.Initialize(constraints);
                    break;
                case Backend.Python:
                    print("initializing python solver");
                    _asyncSolver.Auit = this;
                    _asyncSolver.Initialize(constraints);
                    InvokeRepeating(nameof(RunJobs), 0, 0.0001f);
                    break;
            }

            initialized = true;
        }

        private void OnValidate()
        {
            if (_previousSolver != backendSolver.solver)
                AssignSolver();
        }

        private void Start()
        {
            Instance = this; 

            AsyncIO.ForceDotNet.Force();
            // Start by gathering all the game objects to optimize
            int size = gameObjectsToOptimize.Count;
            _gameObjects = new (GameObject, LocalObjectiveHandler)[size];
            GameObject[] gameObjectsArray = gameObjectsToOptimize.ToArray();
            // Collect  adaptation objectives from the game objects to optimize
            for (int i = 0; i < _gameObjects.Length; i++)
            {
                LocalObjectiveHandler goLocalObjectiveHandler = gameObjectsArray[i]
                    .GetComponent<LocalObjectiveHandler>();
                if (goLocalObjectiveHandler == null)
                {
                    Debug.LogWarning("No objectives / objective handler found in " +
                                   $"{gameObjectsArray[i].name}!");
                }
                _gameObjects[i] = (gameObjectsArray[i],
                    gameObjectsArray[i].GetComponent<LocalObjectiveHandler>());
            }

            InitializeSolver();

            _isSelectionStrategyNotNull = _selectionStrategy != null;
            
        }

        private void OnDestroy()
        {
            _asyncSolver.Destroy();
        }


        #endregion

        public void RegisterTrigger(AdaptationTrigger adaptationTrigger)
        {
            _adaptationTrigger = adaptationTrigger;
        }

        public void UnregisterTrigger(AdaptationTrigger adaptationTrigger)
        {
            if (_adaptationTrigger == adaptationTrigger)
            {
                _adaptationTrigger = null;
            }
        }

        public void RegisterTransition(PropertyTransition propertyTransition)
        {
            if (!_propertyTransitions.Contains(propertyTransition))
            {
                _propertyTransitions.Add(propertyTransition);
            }
        }

        public void UnregisterTransition(PropertyTransition propertyTransition)
        {
            _propertyTransitions.Remove(propertyTransition);
        }
        
        public (List<List<LocalObjective>> objectives, List<Layout> layouts) gatherOptimizationData()
        {
            // The adaptation manager is responsible for knowing the layout 
            // (e.g. what to optimize). The properties to be optimized should 
            // be obtained dynamically in the future, but for now we hardcode 
            // the properties we want to optimize.
            List<List<LocalObjective>> objectives = new List<List<LocalObjective>>();
            List<Layout> layouts = gatherLayouts();

            for (int i = 0; i < _gameObjects.Length; i++)
            {
                if (_gameObjects[i].Item2 != null)
                    objectives.Add(_gameObjects[i].Item2.Objectives);
                
            }
            return (objectives, layouts);
        }

        public List<Layout> gatherLayouts()
        {
            List<Layout> layouts = new List<Layout>();
            for (int i = 0; i < _gameObjects.Length; i++)
            {
                if (_gameObjects[i].Item2 != null)
                {
                    layouts.Add(new
                        Layout(
                            _gameObjects[i].Item2.Id,
                            _gameObjects[i].Item1.transform
                        ));
                }
            }
            return layouts;
        }

        public async UniTask<OptimizationResponse> OptimizeLayout()
        {
            if (isActiveAndEnabled == false)
            {
                Debug.LogError($"[AdaptationManager.OptimizeLayout()]: " +
                               $"AdaptationManager on " +
                               $"{gameObject.name} is disabled!");
                return null;
            }

            // Initialize placement to in front of user camera
            if (m_initPlacement && m_initAnchor != null)
            {
                Matrix4x4 anchorMatrix = Matrix4x4.TRS(
                        m_initAnchor.position,
                        m_initAnchor.rotation,
                        Vector3.one
                    );
                Vector3 initPosition = anchorMatrix.MultiplyPoint3x4(m_initOffset);
                Quaternion initRotation = Quaternion.LookRotation(m_initAnchor.position - initPosition);
                foreach (GameObject obj in gameObjectsToOptimize)
                {
                    obj.transform.position = initPosition;
                    obj.transform.rotation = initRotation;
                }
            }

            (List<List<LocalObjective>> objectives, List<Layout> layouts) = gatherOptimizationData();

            if (objectives.Count == 0)
            {
                Debug.LogWarning($"[AdaptationManager.OptimizeLayout()]: " +
                                 $"Unable to find any objectives on " +
                                 $"adaptation manager game objects...");
                return null;
            }

            //Debug.Log($"Invoking solver: {backendSolver.solver}");
            (OptimizationResponse response, _, _) = await _asyncSolver.
                OptimizeCoroutine(layouts, objectives, MultiElementObjectives);

            //Debug.Log($"First res: {response.suggested.elements[0].Position}");
            return response;
        }

        public int NumObjectives
        {
            get
            {
                if (!isActiveAndEnabled)
                {
                    Debug.LogError($"[AdaptationManager.ComputeCost()]: " +
                                   $"AdaptationManager on " +
                                   $"{gameObject.name} is disabled!");
                    return 0;
                }

                int numObjectives = 0;
                foreach (var element in gameObjectsToOptimize)
                {
                    LocalObjectiveHandler currentHandler = element.GetComponent<LocalObjectiveHandler>();
                    numObjectives += currentHandler.Objectives.Count;
                }

                return numObjectives;

            }
        }

        public List<(string, List<LocalObjective>)> GetLocalObjectives()
        {
            List<(string, List<LocalObjective>)> objectives = new List<(string, List<LocalObjective>)>();
            foreach (var element in gameObjectsToOptimize)
            {
                List<LocalObjective> objObjectives = new List<LocalObjective>();
                LocalObjectiveHandler currentHandler = element.GetComponent<LocalObjectiveHandler>();
                objObjectives.AddRange(currentHandler.Objectives);
                objectives.Add((element.name, objObjectives));
            }
            return objectives;
        }

        public GameObject[] GetObjectsCopy()
        {
            GameObject[] copy = new GameObject[gameObjectsToOptimize.Count];
            for (int i = 0; i < gameObjectsToOptimize.Count; i++)
            {
                copy[i] = Instantiate(gameObjectsToOptimize[i]);
            }
            return copy;
        }

        public NDarray IsParetoDominated(NDarray scores)
        {
            // Get number of points
            int nPoints = scores.shape[0];

            // Initialize array of indices of efficient points
            NDarray isEfficient = np.arange(nPoints);

            

            // Next index in the isEfficient array to search for
            int nextPointIndex = 0;

            while (nextPointIndex < scores.shape[0])
            {
                // Create mask for non-dominated points
                // Check if any dimension is less than the current point (which would mean it's not dominated)
                NDarray nondominatedPointMask = np.any(scores < scores[nextPointIndex], 1);

                // Set the current point as non-dominated
                nondominatedPointMask[nextPointIndex] = np.array(true);

                // Remove dominated points
                isEfficient = isEfficient[nondominatedPointMask];
                scores = scores[nondominatedPointMask];

                // Update next point index
                nextPointIndex = (int)np.sum(nondominatedPointMask[":" + nextPointIndex.ToString()]) + 1;
            }

            return isEfficient;
        }

        // Find pareto-efficient layouts
        public int[] ComputePareto(Layout[] ls)
        {
            int numSamples = ls.Length;
            int numObjectives = NumObjectives;
            NDarray scores = np.zeros((numSamples, numObjectives));
            Debug.Log($"{numSamples} samples, {numObjectives} objectives");

            for (int si = 0; si < numSamples; si++)
            {
                Layout l = ls[si];
                int oi = 0;
                foreach (var element in gameObjectsToOptimize)
                {
                    LocalObjectiveHandler currentHandler = element.GetComponent<LocalObjectiveHandler>();
                    foreach (var objective in currentHandler.Objectives)
                    {
                        scores[si, oi++] = np.array(objective.CostFunction(l));
                    }
                }
            }

            NDarray isEfficient = IsParetoDominated(scores);
            return isEfficient.GetData<int>();
        }

        public int[] ComputeElementPareto(GameObject element, Layout[] ls)
        {
            int numSamples = ls.Length;
            int numObjectives = NumObjectives;
            NDarray scores = np.zeros((numSamples, numObjectives));
            Debug.Log($"{numSamples} samples, {numObjectives} objectives");

            // Get layout of all elements 
            Layout[] lAll = gatherLayouts().ToArray();

            // Get layout of target element
            Layout lElement = lAll[gameObjectsToOptimize.IndexOf(element)];

            LocalObjectiveHandler currentHandler = element.GetComponent<LocalObjectiveHandler>();
            for (int si = 0; si < numSamples; si++)
            {
                Layout l = ls[si];
                int oi = 0;
                foreach (var objective in currentHandler.Objectives)
                {
                    scores[si, oi++] = np.array(objective.CostFunction(l));
                }
                foreach (var objective in MultiElementObjectives)
                {
                    scores[si, oi++] = np.array(objective.CostFunction(l, lAll, lElement));
                }
            }

            NDarray isEfficient = IsParetoDominated(scores);
            return isEfficient.GetData<int>();
        }

        public float ComputeElementCost(GameObject element, Layout l = null)
        {
            l ??= _layout;


            if (!isActiveAndEnabled)
            {
                Debug.LogError($"[AdaptationManager.ComputeCost()]: " +
                               $"AdaptationManager on " +
                               $"{gameObject.name} is disabled!");
                return 0.0f;
            }

            LocalObjectiveHandler currentHandler = element.GetComponent<LocalObjectiveHandler>();
            if (currentHandler.Objectives.Count == 0)
            {
                Debug.LogWarning($"[AdaptationManager.ComputeCost()]: " +
                                 $"Unable to find any objectives on " +
                                 $"{element.name}...");
                return 0.0f;
            }


            // Get layout of all elements 
            Layout[] lAll = gatherLayouts().ToArray();
            // Get layout of target element
            Layout lElement = lAll[gameObjectsToOptimize.IndexOf(element)];
            

            float cost = currentHandler.Objectives.Sum(
                objective => objective.Weight * objective.CostFunction(l));
            float elementWeightSum = currentHandler.Objectives.Sum(objective => objective.Weight);

            // Accounting for global objectives
            cost += MultiElementObjectives.Sum(
                objective => objective.Weight * objective.CostFunction(l, lAll, lElement));
            elementWeightSum += MultiElementObjectives.Sum(objective => objective.Weight);

            if (elementWeightSum >= 0)
            {
                cost /= elementWeightSum;
            }

            

            return cost;
        }

        public float ComputeCost(Layout l = null, bool verbose = false)
        {
            l ??= _layout;
            
            if (!isActiveAndEnabled)
            {
                Debug.LogError($"[AdaptationManager.ComputeCost()]: " +
                               $"AdaptationManager on " +
                               $"{gameObject.name} is disabled!");
                return 0.0f;
            }
            
            // List<List<LocalObjective>> objectives = new List<List<LocalObjective>>();
            // List<Layout> layouts = new List<Layout>();
            
            // TODO: Decide global objectives
            // foreach (var element in gameObjectsToOptimize)
            // {
            //     AUIT auit = element.GetComponent<AUIT>();
            //     globalObjectives.Add(auit._localObjectiveHandler.Objectives);
            //     layouts.Add(auit._layout);
            // }
            
            float cost = 0;
            foreach (var element in gameObjectsToOptimize)
            {
                LocalObjectiveHandler currentHandler = element.GetComponent<LocalObjectiveHandler>();
                float elementCostSum = currentHandler.Objectives.Sum(
                    objective => objective.Weight * objective.CostFunction(l));
                float elementWeightSum = currentHandler.Objectives.Sum(objective => objective.Weight);
                if (elementWeightSum >= 0)
                {
                    elementCostSum /= elementWeightSum;
                }
                cost += elementCostSum;
            }

            // TODO: Global objective cost
            
            cost /= gameObjectsToOptimize.Count;
            return cost;
        }
        
        private bool _isSelectionStrategyNotNull;

        #region Adaptation Logic
        // When an adaptation is invoked, the manager will contain the method for doing so. This is
        // necessary as property transitions might require additional logic in the future
        // (e.g., pareto optimal adaptations). It will be necessary to support property transitions
        // with more responsibilities such as picking from various layouts
        
        public void Adapt(UIConfiguration[] layouts)
        {
            if (!isActiveAndEnabled)
            {
                Debug.LogError($"[AdaptationManager.Adapt(layout)]: AdaptationManager on " +
                               $"{gameObject.name} is disabled!");
                return;
            }
            
            // If global property transition logic exists, execute it
            if (_isSelectionStrategyNotNull)
            {
                _selectionStrategy.Adapt(layouts);
            }
            else // otherwise, apply the property transitions each UI element contains
            {
                if (layouts.Length > 1)
                    Debug.LogWarning("Solver is computing multiple layouts but there is no " +
                                     "solution selection strategy. Applying the first solution " +
                                     $"by default. GameObject: {name}");
                // pick first layout and apply property transitions
                Layout[] layoutArray = layouts[0].elements;
                GameObject[] elementArray = gameObjectsToOptimize.ToArray();
                for (int i = 0; i < layoutArray.Length; i++)
                {
                    Layout result = layoutArray[i];
                    elementArray[i].GetComponent<LocalObjectiveHandler>().Transition(result);
                }
            }
        }

        public void RegisterAdaptationListener(AdaptationListener adaptationListener)
        {
            if (_adaptationListeners.Contains(adaptationListener))
                return;

            _adaptationListeners.Add(adaptationListener);
        }

        public void UnregisterAdaptationListener(AdaptationListener adaptationListener)
        {
            _adaptationListeners.Remove(adaptationListener);
        }

        private void InvokeAdaptationListeners(Layout adaptation)
        {
            foreach (var adaptationListener in _adaptationListeners)
            {
                adaptationListener.AdaptationUpdated(adaptation);
            }
        }
        #endregion
        
        #region LayoutSelectionStrategy
        public void RegisterSelectionStrategy(SelectionStrategy selectionStrategy)
        {
            if (_selectionStrategy != null && _selectionStrategy != selectionStrategy)
                Debug.LogError($"Multiple selection strategies in GameObject {name}");

            _selectionStrategy = selectionStrategy;
            _isSelectionStrategyNotNull = true;
        }

        public void UnregisterSelectionStrategy()
        {
            _selectionStrategy = null;
            _isSelectionStrategyNotNull = false;
        }
        
        #endregion

        #region Solver server communication

        // This method is invoked repeatedly to process requests from the
        // server (used by the genetic algorithm solver)
        private void RunJobs()
        {
            if (!_job) return;
            _jobResult = new List<List<float>>();
            foreach (var candidateLayout in _layoutJob)
            {
                var costsForCandidateLayout = new List<float>();
                Layout[] candidateLayoutArray = candidateLayout.elements;
                for (int i = 0; i < candidateLayout.elements.Length; i++)
                {
                    if (developmentMode && _gameObjects[i].Item2.Id !=
                        candidateLayout.elements[i].Id)
                    {
                        Debug.LogError("Ids do not match in evaluation " +
                                       "request!");
                    }
                    costsForCandidateLayout.AddRange(
                        _gameObjects[i].Item2
                        .Objectives
                        .Select(objective =>
                            objective.CostFunction(candidateLayoutArray[i])));
                }
                _jobResult.Add(costsForCandidateLayout);
            }
            _job = false;
        }


        public List<List<float>> EvaluateLayouts(EvaluationRequest evaluationRequest)
        {
            _layoutJob = evaluationRequest.layouts; 
            _job = true;
            
            while (_job) {} 
            
            return _jobResult;
        }

        #endregion

        public void RegisterMultiElementObjective(MultiElementObjective multiElementObjective)
        {
            if (MultiElementObjectives.Contains(multiElementObjective))
                return;

            MultiElementObjectives.Add(multiElementObjective);
        }

        public void UnregisterMultiElementObjective(MultiElementObjective multiElementObjective)
        {
            if (!MultiElementObjectives.Contains(multiElementObjective))
                return;

            MultiElementObjectives.Remove(multiElementObjective);
        }
    }
    
    public enum Backend
    {
        Unity,
        Python
    }
    
    public enum SolverUnity
    {
        SimulatedAnnealing,
        ExhaustiveSearch
    }

    public enum SolverPython
    {
        GeneticAlgorithm
    }
    
    [Serializable]
    public class BackendSolver
    {
        public Backend backend;
        public string solver;
    }
    
    [CustomPropertyDrawer(typeof(BackendSolver))]
    public class BackendSolverDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty backendProp = property.FindPropertyRelative("backend");
            SerializedProperty solverProp = property.FindPropertyRelative("solver");

            // Define rect areas for the two fields
            Rect mainEnumRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            Rect subEnumRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width, EditorGUIUtility.singleLineHeight);

            // Draw Main Enum
            EditorGUI.PropertyField(mainEnumRect, backendProp);

            // Determine the corresponding sub-enum type
            Type subEnumType = null;
            switch ((Backend)backendProp.enumValueIndex)
            {
                case Backend.Unity:
                    subEnumType = typeof(SolverUnity);
                    break;
                case Backend.Python:
                    subEnumType = typeof(SolverPython);
                    break;
            }

            if (subEnumType != null)
            {
                // Get all enum names from the selected sub-enum type
                string[] subEnumNames = Enum.GetNames(subEnumType);
                int currentIndex = Array.IndexOf(subEnumNames, solverProp.stringValue);

                if (currentIndex == -1) currentIndex = 0; // Default to first value if invalid

                // Draw Sub Enum Dropdown
                int selectedIndex = EditorGUI.Popup(subEnumRect, "Solver", currentIndex, subEnumNames);
                solverProp.stringValue = subEnumNames[selectedIndex];
            }
            else
            {
                EditorGUI.LabelField(subEnumRect, "No Solver Available");
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2 + 4; // Adjust height to fit two fields
        }
    }
}
