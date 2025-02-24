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
using AUIT.Extras;
using Cysharp.Threading.Tasks;
using UnityEditor;

namespace AUIT
{
    public sealed class AUIT : MonoBehaviour
    {
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

        // flag to signal that the manager has been initialized
        [NonSerialized]
        public bool initialized = false;
        
        #region MonoBehaviour Implementation
        
        public AUIT()
        {
            // Ensure that .NET is completely initialized to make sure
            // async methods work as expected
            AsyncIO.ForceDotNet.Force();
            
            _asyncSolver = new SimulatedAnnealingSolver();
            solverSettings = _asyncSolver;
        }
        
        private void InitializeSolver()
        {
            Debug.Log("Initializing solver");
            
            // make sure that the old solver is destroyed
            _asyncSolver.Destroy();
            switch (backendSolver.backend)
            {
                case Backend.Unity:
                    // Right now there is only one unity solver
                    _asyncSolver = new SimulatedAnnealingSolver();
                    
                    break;
                case Backend.Python:
                    _asyncSolver = new ParetoFrontierSolver();
                    
                    // AsyncIO.ForceDotNet.Force();
                    _asyncSolver.Auit = this;
                    _asyncSolver.Initialize();
                    InvokeRepeating(nameof(RunJobs), 0, 0.0001f);
                    break;
            }
            solverSettings = _asyncSolver;
            _previousSolver = backendSolver.solver;

            initialized = true;
        }

        private void OnValidate()
        {
            if (_previousSolver != backendSolver.solver)
                InitializeSolver();
        }

        private void Start()
        {
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
                    Debug.LogError("No handler found in " +
                                   $"{gameObjectsArray[i].name}!");
                }
                _gameObjects[i] = (gameObjectsArray[i],
                    gameObjectsArray[i].GetComponent<LocalObjectiveHandler>());
            }

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

        public async UniTask<OptimizationResponse> OptimizeLayout()
        {
            if (isActiveAndEnabled == false)
            {
                Debug.LogError($"[AdaptationManager.OptimizeLayout()]: " +
                               $"AdaptationManager on " +
                               $"{gameObject.name} is disabled!");
                return null;
            }
            
            // The adaptation manager is responsible for knowing the layout 
            // (e.g. what to optimize). The properties to be optimized should 
            // be obtained dynamically in the future, but for now we hardcode 
            // the properties we want to optimize.
            List<List<LocalObjective>> objectives = new List<List<LocalObjective>>();
            List<Layout> currentLayouts = new List<Layout>();

            for (int i = 0; i < _gameObjects.Length; i++)
            {
                objectives.Add(_gameObjects[i].Item2.Objectives);
                currentLayouts.Add(new 
                    Layout(
                        _gameObjects[i].Item2.Id, 
                        _gameObjects[i].Item1.transform
                        ));
            }
            
            if (objectives.Count == 0)
            {
                Debug.LogWarning($"[AdaptationManager.OptimizeLayout()]: " +
                                 $"Unable to find any objectives on " +
                                 $"adaptation manager game objects...");
                return null;
            }

            Debug.Log($"Invoking solver: {backendSolver.solver}");
            OptimizationResponse response = await _asyncSolver.
                OptimizeCoroutine(currentLayouts, objectives);
            
            Debug.Log($"First res: {response.suggested.elements[0].Position}");
            return response;
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

            List<List<LocalObjective>> globalObjectives = new List<List<LocalObjective>>();
            List<Layout> layouts = new List<Layout>();
            foreach (var element in gameObjectsToOptimize)
            {
                AUIT auit = element.GetComponent<AUIT>();
                globalObjectives.Add(auit._localObjectiveHandler.Objectives);
                layouts.Add(auit._layout);
            }

            float cost = 0;
            for (int i = 0; i < gameObjectsToOptimize.Count; i++)
            {
                cost += globalObjectives[i].Sum(objective =>
                            objective.Weight * objective.CostFunction(layouts[i])) /
                        globalObjectives[i].Count;
            }
            cost /= globalObjectives.Count;
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
                    elementArray[i].GetComponent<LocalObjectiveHandler>().Transition(layoutArray[i]);
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
            throw new NotImplementedException();
        }

        public void UnregisterMultiElementObjective(MultiElementObjective multiElementObjective)
        {
            throw new NotImplementedException();
        }
    }
    
    public enum Backend
    {
        Unity,
        Python
    }
    
    public enum SolverUnity
    {
        SimulatedAnnealing
    }

    public enum SolverPython
    {
        GeneticAlgorithm
    }
    
    [Serializable]
    public class BackendSolver
    {
        public Backend backend = Backend.Unity;
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
