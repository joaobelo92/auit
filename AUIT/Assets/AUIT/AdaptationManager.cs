using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AUIT.PropertyTransitions;
using AUIT.AdaptationTriggers;
using AUIT.AdaptationObjectives;
using AUIT.SelectionStrategies;
using AUIT.Solvers;
using AUIT.AdaptationObjectives.Definitions;
using NetMQ;

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
        private Solver solver = Solver.SimulatedAnnealing;

        [Tooltip("Number of iterations the solver will run for. A higher number can lead to better" +
                 "solutions but take longer to execute.")]
        public int iterations = 1500;

        public bool developmentMode = true;
        
        // Simulating annealing hyperparameters
        public float minimumTemperature = 0.000001f;
        public float initialTemperature = 10000f;
        public float annealingSchedule = 0.98f;
        public float earlyStopping = 0.02f;
        
        private IAsyncSolver _asyncSolver = new AsyncSimulatedAnnealingSolver();

        private bool _waitingForOptimization;

        private bool _job;
        private List<List<Layout>> _layoutJob;
        private List<List<float>> _jobResult;

        private SelectionStrategy _selectionStrategy;

        // To be phased out for multiple layouts
        private Layout _layout;

        public List<GameObject> gameObjects;
        private (GameObject, LocalObjectiveHandler)[] _gameObjects;

        #region MonoBehaviour Implementation

        private void Start()
        {
            _gameObjects = new (GameObject, LocalObjectiveHandler)[gameObjects.Count];
            GameObject[] gameObjectsArray = gameObjects.ToArray();
            for (int i = 0; i < _gameObjects.Length; i++)
            {
                LocalObjectiveHandler goLocalObjectiveHandler = gameObjectsArray[i]
                    .GetComponent<LocalObjectiveHandler>();
                if (goLocalObjectiveHandler == null)
                {
                    Debug.LogError($"No handler found in {gameObjectsArray[i].name}!");
                }
                _gameObjects[i] = (gameObjectsArray[i], gameObjectsArray[i].GetComponent<LocalObjectiveHandler>());
            }
            
            _isSelectionStrategyNotNull = _selectionStrategy != null;
            if (solver == Solver.GeneticAlgorithm)
            {
                _asyncSolver = new ParetoFrontierSolver();
            
                AsyncIO.ForceDotNet.Force();
                _asyncSolver.AdaptationManager = this;
                Debug.Log("Attempting to start solver");
                _asyncSolver.Initialize();
            }
            InvokeRepeating(nameof(RunJobs), 0, 0.01f);
        }

        private void OnDestroy()
        {
            if (solver != Solver.GeneticAlgorithm) return;
            ParetoFrontierSolver paretoFrontierSolver = (ParetoFrontierSolver) _asyncSolver;
            paretoFrontierSolver.Destroy();
        }

        private void RunJobs()
        {
            // TODO: stop assuming all jobs are evaluations
            if (!_job) return;
            _jobResult = new List<List<float>>();
            foreach (var candidateLayout in _layoutJob)
            {
                var costsForCandidateLayout = new List<float>();
                Layout[] candidateLayoutArray = candidateLayout.ToArray();
                for (int i = 0; i < candidateLayoutArray.Length; i++)
                {
                    if (developmentMode && _gameObjects[i].Item2.Id != candidateLayout[i].Id)
                    {
                        Debug.LogError("Ids do not match in evaluation request!");
                    }
                    costsForCandidateLayout.AddRange(_gameObjects[i].Item2.Objectives
                        .Select(objective => objective.CostFunction(candidateLayoutArray[i])));
                }
                _jobResult.Add(costsForCandidateLayout);
            }
            _job = false;
        }

        #endregion
        
        // Called by the adaptation trigger to find an adaptation. When the solver returns one/multiple solutions
        // the adaptation logic is invoked
        public IEnumerator OptimizeLayoutAndAdapt(float optimizationTimeout, Action<List<List<Layout>>, float> adaptationLogic)
        {
            var optimizationTimeStart = Time.realtimeSinceStartup;
            OptimizeLayout();

            while (true)
            {
                bool timeExceeded = Time.realtimeSinceStartup - optimizationTimeStart >= optimizationTimeout;
                if (timeExceeded)
                {
                    Debug.Log("Optimization timed out");
                    yield break;
                }

                if (_asyncSolver.Result.Item1 != null)
                {
                    Debug.Log("could apply result!");
                    Debug.Log(_asyncSolver.Result.Item1);
                    adaptationLogic(_asyncSolver.Result.Item1, _asyncSolver.Result.Item2);
                    yield break;
                }
                yield return new WaitForEndOfFrame();
            }
        }

        public List<LocalObjective> GetObjectives(GameObject gameObject)
        {
            // TODO Handle exceptions 
            return gameObject.GetComponent<LocalObjectiveHandler>().Objectives;
        }

        public void RegisterAdaptationTrigger(AdaptationTrigger adaptationTrigger)
        {
            _adaptationTrigger = adaptationTrigger;
        }

        public void UnregisterAdaptationTrigger(AdaptationTrigger adaptationTrigger)
        {
            if (_adaptationTrigger == adaptationTrigger)
            {
                _adaptationTrigger = null;
            }
        }

        public void RegisterPropertyTransition(PropertyTransition propertyTransition)
        {
            if (!_propertyTransitions.Contains(propertyTransition))
            {
                _propertyTransitions.Add(propertyTransition);
            }
        }

        public void UnregisterPropertyTransition(PropertyTransition propertyTransition)
        {
            _propertyTransitions.Remove(propertyTransition);
        }

        public List<List<float>> EvaluateLayouts(string payload)
        {
            Wrapper<string> evaluationRequest = JsonUtility.FromJson<Wrapper<string>>(payload);
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

        // public List<List<float>> EvaluateLayouts(List<List<Layout>> ls)
        // {
        //     _layoutJob = ls; // Assign the list of candidate layouts, each of which is a list of elements defined as a Layout, to the current job to be evaluated
        //     _job = true; // Set the job flag to true, which will trigger the job to be evaluated in the next dequeue action
        //
        //     while (_job) {} // Wait for the job to be evaluated
        //     return _jobResult; // Return the result of the job (i.e., the costs of each candidate layout)
        // }

        public (List<Layout>, float) OptimizeLayout()
        {
            if (isActiveAndEnabled == false)
            {
                Debug.LogError($"[AdaptationManager.OptimizeLayout()]: AdaptationManager on {gameObject.name} is disabled!");
                return (new List<Layout> { _layout }, 0.0f);
            }
            
            List<float> hyperparameters = new List<float>();
            if (solver == Solver.SimulatedAnnealing)
            {
                hyperparameters.Add(iterations);
                hyperparameters.Add(minimumTemperature);
                hyperparameters.Add(initialTemperature);
                hyperparameters.Add(annealingSchedule);
                hyperparameters.Add(earlyStopping);
            }
            
            // The adaptation manager is responsible for knowing the layout (e.g. what to optimize)
            // The properties to be optimized should be obtained dynamically in the future, but for now we hardcode 
            // the properties we want to optimize.
            List<List<LocalObjective>> objectives = new List<List<LocalObjective>>();
            List<Layout> currentLayouts = new List<Layout>();

            for (int i = 0; i < _gameObjects.Length; i++)
            {
                objectives.Add(_gameObjects[i].Item2.Objectives);
                currentLayouts.Add(new Layout(_gameObjects[i].Item2.Id, _gameObjects[i].Item1.transform));
            }
            
            if (objectives.Count == 0)
            {
                Debug.LogWarning($"[AdaptationManager.OptimizeLayout()]: Unable to find any objectives on adaptation manager game objects...");
                return (new List<Layout> { _layout }, 0.0f);
            }

            Debug.Log($"Invoking solver: {solver}");
            StartCoroutine(_asyncSolver.OptimizeCoroutine(currentLayouts, objectives, hyperparameters));
            return (null, _asyncSolver.Result.Item2);
        }

        public float ComputeCost(Layout l = null, bool verbose = false)
        {
            l ??= _layout;
            
            if (!isActiveAndEnabled)
            {
                Debug.LogError($"[AdaptationManager.ComputeCost()]: AdaptationManager on {gameObject.name} is disabled!");
                return 0.0f;
            }

            List<List<LocalObjective>> globalObjectives = new List<List<LocalObjective>>();
            List<Layout> layouts = new List<Layout>();
            foreach (var element in gameObjects)
            {
                AdaptationManager adaptationManager = element.GetComponent<AdaptationManager>();
                globalObjectives.Add(adaptationManager._localObjectiveHandler.Objectives);
                layouts.Add(adaptationManager._layout);
            }

            float cost = 0;
            for (int i = 0; i < gameObjects.Count; i++)
            {
                cost += globalObjectives[i].Sum(objective => objective.Weight * objective.CostFunction(layouts[i])) / globalObjectives[i].Count;
            }
            cost /= globalObjectives.Count;
            return cost;
        }
        
        private bool _isSelectionStrategyNotNull;

        #region Adaptation Logic
        // When an adaptation is invoked, the manager will contain the method for doing so. This is necessary as 
        // property transitions might require additional logic in the future (e.g., pareto optimal adaptations). 
        // It will be necessary to support property transitions with more responsibilities such as picking from 
        // various layouts
        
        public void Adapt(List<List<Layout>> layouts)
        {
            if (!isActiveAndEnabled)
            {
                Debug.LogError($"[AdaptationManager.Adapt(layout)]: AdaptationManager on {gameObject.name} is disabled!");
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
                    Debug.LogWarning($"Solver is computing multiple layouts but no there is no solution selection" +
                                     $"strategy. Applying the first solution by default. GameObject: {name}");
                // pick first layout and apply property transitions
                Layout[] layoutArray = layouts.First().ToArray();
                GameObject[] elementArray = gameObjects.ToArray();
                for (int i = 0; i < layoutArray.Length; i++)
                {
                    if (developmentMode)
                    {
                    }
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
