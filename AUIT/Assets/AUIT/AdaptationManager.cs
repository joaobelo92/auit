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
using Cysharp.Threading.Tasks;
// using UnityEngine.Serialization;
using UnityEngine;

namespace AUIT
{
    public sealed class AdaptationManager : MonoBehaviour
    {
        public string Id { get; } = Guid.NewGuid().ToString();

        private LocalObjectiveHandler _localObjectiveHandler;
        private AdaptationTrigger _adaptationTrigger;
        private readonly List<PropertyTransition> _propertyTransitions = new();
        private readonly List<AdaptationListener> _adaptationListeners = new();
        
        public enum Solver
        {
            SimulatedAnnealing,
            GeneticAlgorithm
        }

        [SerializeField]
        private Solver solverType;
        private IAsyncSolver _asyncSolver;

        [SerializeReference] public IAsyncSolver solverSettings;
        public bool developmentMode = true;
        private bool _waitingForOptimization;

        private bool _job;
        private List<List<Layout>> _layoutJob;
        private List<List<float>> _jobResult;

        private SelectionStrategy _selectionStrategy;

        // To be phased out for multiple layouts
        private Layout _layout;

        public List<GameObject> gameObjectsToOptimize;

        private (GameObject, LocalObjectiveHandler)[] _gameObjects;

        // flag to signal that the manager has been initialized
        [NonSerialized]
        public bool initialized = false;
        
        AdaptationManager()
        {
            solverType = Solver.SimulatedAnnealing;
            // Ensure that .NET is completely initialized to make sure
            // async methods work as expected
            AsyncIO.ForceDotNet.Force();
            
            _asyncSolver = new SimulatedAnnealingSolver();
            solverSettings = _asyncSolver;
        }
        
        // callback to when some values might have changed
        public void OnValidate()
        {
            // make sure that the old solver is destroyed
            _asyncSolver.Destroy();
            switch (solverType)
            {
                case Solver.SimulatedAnnealing:
                    _asyncSolver = new SimulatedAnnealingSolver();
                    break;
                case Solver.GeneticAlgorithm:
                    _asyncSolver = new ParetoFrontierSolver();
                    break;
            }
            // TODO: merge solverSettings with _asyncSolver
            solverSettings = _asyncSolver;
        }

        #region MonoBehaviour Implementation

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

            // Set flag to signal that the manager has been initialized
            _asyncSolver.Initialize();
            Debug.Log("Starting solver...");
            // TODO: understand why its now just called on the GeneticAlgorithmSolver
            //  and why its running at 10000Hz instead of 100Hz
            InvokeRepeating(nameof(RunJobs), 0, 0.0001f);
            initialized = true;
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

        public async UniTask<(List<List<Layout>>, float)> OptimizeLayout()
        {
            if (isActiveAndEnabled == false)
            {
                Debug.LogError($"[AdaptationManager.OptimizeLayout()]: " +
                               $"AdaptationManager on " +
                               $"{gameObject.name} is disabled!");
                return (null, 0.0f);
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
                return (null, 0.0f);
            }

            Debug.Log($"Invoking solver: {solverType}");
            
            (List<List<Layout>> result, float costs) = await _asyncSolver.
                OptimizeCoroutine(currentLayouts, objectives);
            
            Debug.Log($"First res: {result[0][0].Position}");
            return (result, costs);
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
                AdaptationManager adaptationManager = element.GetComponent<AdaptationManager>();
                globalObjectives.Add(adaptationManager._localObjectiveHandler.Objectives);
                layouts.Add(adaptationManager._layout);
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
        
        public void Adapt(List<List<Layout>> layouts)
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
                if (layouts.Count > 1)
                    Debug.LogWarning("Solver is computing multiple layouts but there is no " +
                                     "solution selection strategy. Applying the first solution " +
                                     $"by default. GameObject: {name}");
                // pick first layout and apply property transitions
                Layout[] layoutArray = layouts.First().ToArray();
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
                Layout[] candidateLayoutArray = candidateLayout.ToArray();
                for (int i = 0; i < candidateLayoutArray.Length; i++)
                {
                    if (developmentMode && _gameObjects[i].Item2.Id !=
                        candidateLayout[i].Id)
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


        public List<List<float>> EvaluateLayouts(string payload)
        {
            // Debug.Log("san");
            Wrapper<string> evaluationRequest =
                JsonUtility.FromJson<Wrapper<string>>(payload);
            List<List<Layout>> layouts = new List<List<Layout>>();
            foreach (var l in evaluationRequest.items)
            {
                Wrapper<Layout> e = JsonUtility.FromJson<Wrapper<Layout>>(l);
                layouts.Add(e.items.ToList());
            }

            _layoutJob = layouts; 
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
}
