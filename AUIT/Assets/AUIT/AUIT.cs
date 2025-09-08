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

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            AsyncIO.ForceDotNet.Force();
            // Start by gathering all the game objects to optimize
            int size = gameObjectsToOptimize.Count;
            _gameObjects = new (GameObject, LocalObjectiveHandler)[size];
            GameObject[] gameObjectsArray = gameObjectsToOptimize.ToArray();
            // Collect adaptation objectives from the game objects to optimize
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

        /// <summary>
        /// Retrieves all the local adaptation objectives and their corresponding layouts.
        /// </summary>
        /// <returns>A tuple consisting of a list of lists of local objectives and a list of layouts.
        /// Each inner list of local objectives corresponds to a layout at the same index.</returns>
        public (List<List<LocalObjective>>, List<Layout>) GetLayoutsAndLocalObjectives(bool currentLayout = true, List<ObjectiveType> evaluationObjectives = null)
        {
            List<List<LocalObjective>> objectives = new List<List<LocalObjective>>();
            List<Layout> layouts = new List<Layout>();
            
            for (int i = 0; i < _gameObjects.Length; i++)
            {
                if (_gameObjects[i].Item2 != null)
                {
                    CoordinateSystem coordinateSystem = CoordinateSystem.World;
                    if (_gameObjects[i].Item2.GetComponent<CoordinateSystemTransition>() != null)
                    {
                        // Ensure the coordinate system transition is updated before getting the layout
                        coordinateSystem = _gameObjects[i].Item2.GetComponent<CoordinateSystemTransition>().CurrentCoordinateSystem;
                    }
                    // Avoid unnecessary overhead if computing a specific layout
                    if (currentLayout)
                        layouts.Add(new
                            Layout(
                                _gameObjects[i].Item2.Id,
                                _gameObjects[i].Item1.transform,
                                coordinateSystem
                            ));
                    
                    List<LocalObjective> objs = new();
                    foreach (var obj in _gameObjects[i].Item2.Objectives)
                    {
                        if (evaluationObjectives == null || evaluationObjectives.Count == 0 || evaluationObjectives.Contains(obj.ObjectiveType))
                        {
                            // print("Adding objective " + obj.ObjectiveType + " to evaluation: " + obj.name);
                            objs.Add(obj);
                        }
                    }
                    objectives.Add(objs);
                }
            }
            
            return (objectives, layouts);
        }

        /// <summary>
        /// Asynchronously computes the optimal layout for a set of UI elements managed by the <see cref="AdaptationManager"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="UniTask{OptimizationResponse}"/> representing the asynchronous operation,
        /// containing the optimization result if successful; otherwise, <c>null</c>.
        /// </returns>
        /// <remarks>
        /// This method performs the following steps:
        /// <list type="number">
        /// <item>Checks if the component is active and enabled; logs an error and returns <c>null</c> if not.</item>
        /// <item>Gathers local objectives and current layouts from the UI elements.</item>
        /// <item>If no objectives are found, logs a warning and returns <c>null</c>.</item>
        /// <item>Uses an asynchronous solver to compute the optimal layout solution.</item>
        /// </list>
        /// </remarks>
        public async UniTask<OptimizationResponse> OptimizeLayout()
        {
            if (isActiveAndEnabled == false)
            {
                Debug.LogError($"[AdaptationManager.OptimizeLayout()]: " +
                               $"AdaptationManager on " +
                               $"{gameObject.name} is disabled!");
                return null;
            }

            (List<List<LocalObjective>> objectives, List<Layout> layouts) = GetLayoutsAndLocalObjectives();

            if (objectives.Count == 0)
            {
                Debug.LogWarning($"[AdaptationManager.OptimizeLayout()]: " +
                                 $"Unable to find any objectives on " +
                                 $"adaptation manager game objects...");
                return null;
            }

            (OptimizationResponse response, _, _) = await _asyncSolver.
                OptimizeCoroutine(layouts, objectives, MultiElementObjectives);

            return response;
        }
        
        /// <summary>
        /// Computes the layout cost for a given layout configuration.
        /// </summary>
        /// <param name="layouts">Optional solution to compute cost for. If null, use the current layout.</param>
        /// <param name="verbose">If true, enables detailed logging for debugging or analysis.</param>
        /// <returns>The total computed cost as a float value.</returns>
        public float ComputeCost(List<Layout> layouts = null, List<ObjectiveType> evaluationObjectives = null, bool verbose = false)
        {
            (List<List<LocalObjective>> objectives, List<Layout> l) = GetLayoutsAndLocalObjectives(layouts == null, evaluationObjectives: evaluationObjectives);
            
            // If no list of layouts is provided, compute the cost of current configuration
            if (layouts == null)
            {
                layouts = l;
            }
            
            float cost = 0;
            string costsLog = "";

            for (int i = 0; i < layouts.Count; i++)
            {
                for (int j = 0; j < objectives[i].Count; j++)
                {
                    if (!objectives[i][j].isActiveAndEnabled) continue;
                    float objectiveCost = objectives[i][j].Weight * objectives[i][j].CostFunction(layouts[i]);
                    cost += objectiveCost;
                    costsLog += $"Objective {objectives[i][j].ObjectiveType} Cost: {objectiveCost}\n";
                }
            }

            cost += MultiElementObjectives.Sum(
                objective => objective.Weight * objective.CostFunction(layouts.ToArray()));

            if (verbose)
            {
                Debug.Log($"[AdaptationManager.ComputeCost()]: Total Cost: {cost}\n" +
                          $"Costs Breakdown:\n{costsLog}");
            }


            return cost;
        }

        private bool _isSelectionStrategyNotNull;

        #region Adaptation Logic
        
        /// <summary>
        /// Applies UI layout adaptations based on the provided layout configurations.
        /// </summary>
        /// <param name="layouts">An array of <see cref="UIConfiguration"/> representing the layout solutions 
        /// to be applied to the UI elements.</param>
        /// <remarks>
        /// This method checks whether the <see cref="AdaptationManager"/> is active and enabled before proceeding.
        /// If a selection strategy is defined, it delegates the adaptation to that strategy.
        /// Otherwise, it applies the first layout configuration directly to the associated UI elements, 
        /// logging a warning if multiple layouts are provided without a selection strategy.
        /// </remarks>
        public void Adapt(UIConfiguration[] layouts)
        {
            if (!isActiveAndEnabled)
            {
                Debug.LogError($"[AdaptationManager.Adapt(layout)]: AdaptationManager on " +
                               $"{gameObject.name} is disabled!");
                return;
            }

            // If a selection strategy exists, execute it
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
