using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using AUIT.PropertyTransitions;
using AUIT.AdaptationTriggers;
using AUIT.AdaptationObjectives;
using AUIT.Solvers;
using AUIT.Solvers.Experimental;
using AUIT.AdaptationObjectives.Definitions;
using Newtonsoft.Json;

namespace AUIT
{
    public class AdaptationManager : MonoBehaviour
    {

        /*
         * Adaptation managers must always be connected to a objective (1-1 relation) 
         * While a objective has the responsibility of providing better ui layouts, the adaptation manager will be
         * responsible for deciding when and how that information will be applied.
         * Managers follow one adaptation trigger (for now at least) and can support multiple property transitions.
         * Because the adaptation triggers are "triggered" by the property transition, these could be dependencies
         * of an adaptation trigger that are added automatically to make the creator's task easier.
        */

        [HideInInspector]
        private LocalObjectiveHandler LocalObjectiveHandler;
        [HideInInspector]
        private AdaptationTrigger adaptationTrigger;
        [HideInInspector]
        private List<PropertyTransition> propertyTransitions = new List<PropertyTransition>();
        [HideInInspector]
        protected List<AdaptationListener> adaptationListeners = new List<AdaptationListener>();

        public ISolver solver = new SimulatedAnnealingSolver();
        public List<float> hyperparameters = new List<float> { 1500f, 0.000001f, 10000f, 0.98f, 0.02f };

        private IAsyncSolver asyncSolver = new ParetoFrontierSolver();
        public Coroutine AsyncSolverOptimizeCoroutine { get; private set; }

        private bool waitingForOptimization = false;

        [HideInInspector]
        public Layout layout;

        // public Camera camera;

        public List<GameObject> UIElements;

        public bool isGlobal = false;
        private bool isAdapting;
        public bool IsAdapting
        {
            get
            {
                if (isGlobal)
                {
                    foreach (var element in UIElements)
                    {
                        AdaptationManager adaptationManager = element.GetComponent<AdaptationManager>();
                        if (adaptationManager.isGlobal)
                        {
                            Debug.LogWarning($"A global manager was found were it should be local at {adaptationManager}");
                        }
                        if (adaptationManager.IsAdapting)
                        {
                            return true;
                        }
                    }
                    return false;
                }
                return isAdapting;
            }
            set
            {
                isAdapting = value;
            }
        }

        #region MonoBehaviour Implementation

        protected virtual void Awake()
        {
            if (!isGlobal && LocalObjectiveHandler == null)
            {
                LocalObjectiveHandler = GetComponent<LocalObjectiveHandler>();
            }

            if (!isGlobal && LocalObjectiveHandler == null)
            {
                Debug.LogError("No Local Objective Handler component found on " + name);
                this.enabled = false;
            }

        }

        // Start is called before the first frame update
        protected virtual void Start()
        {
            layout = new Layout(transform);
            asyncSolver.adaptationManager = this;
            asyncSolver.Initialize();
        }

        // Update is called once per frame
        void Update()
        {
            // if (Time.frameCount % 100 == 0)
            // {
            //     print(LocalObjectiveHandler.Objectives.First().name);
            //     print(LocalObjectiveHandler.Objectives.First().CostFunction(layout));
            // }
            if (isGlobal && Input.GetKeyDown(KeyCode.V))
            {
                for (int i = 0; i < 100; i++)
                {
                    Camera.main.transform.position = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
                    Camera.main.transform.rotation = new Quaternion();

                    // Benchmarking mode has to be hardcoded currently
                    var (layouts, cost) = OptimizeLayout();

                    for (int k = 0; k < UIElements.Count; k++)
                    {
                        UIElements[k].GetComponent<AdaptationManager>().layout = layouts[k];
                        UIElements[k].GetComponent<AdaptationManager>().Adapt(layouts[k]);
                    }
                }
            }
            if (isGlobal && Input.GetKeyDown(KeyCode.B))
            {
                List<double> times = new List<double>();

                for (int i = 0; i < 100; i++)
                {
                    Camera.main.transform.position = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
                    Camera.main.transform.rotation = new Quaternion();

                    // Benchmarking mode has to be hardcoded currently
                    double time = ComputeCost();
                    times.Add(time);
                }

                print(string.Join(", ", times));
            }
        }

        #endregion

        public List<LocalObjective> GetObjectives()
        {
            return LocalObjectiveHandler.Objectives;
        }

        public void RegisterAdaptationTrigger(AdaptationTrigger adaptationTrigger)
        {
            this.adaptationTrigger = adaptationTrigger;
        }

        public void UnregisterAdaptationTrigger(AdaptationTrigger adaptationTrigger)
        {
            if (this.adaptationTrigger == adaptationTrigger)
            {
                this.adaptationTrigger = null;
            }
        }

        public void RegisterPropertyTransition(PropertyTransition proeprtyTransition)
        {
            if (!propertyTransitions.Contains(proeprtyTransition))
            {
                propertyTransitions.Add(proeprtyTransition);
            }
        }

        public void UnregisterPropertyTransition(PropertyTransition propertyTransition)
        {
            propertyTransitions.Remove(propertyTransition);
        }

        public List<(string, List<(string, float)>)> EvaluateLayout(string inc)
        {
            
            print("triggered");
            print(LocalObjectiveHandler.Objectives.Count);
            print(LocalObjectiveHandler.Objectives.First().name);
            print(LocalObjectiveHandler.Objectives.First().CostFunction(layout));
            List<(string, List<(string, float)>)> result = new List<(string, List<(string, float)>)>();
            // TODO: evaluate actual layout from python 
            if (!isGlobal)
            {
                List<(string, float)> costs = new List<(string, float)>();
                print(LocalObjectiveHandler.Objectives.Count);
                print(LocalObjectiveHandler.Objectives.First().name);
                print(LocalObjectiveHandler.Objectives.First().CostFunction(layout));
                foreach (var objective in LocalObjectiveHandler.Objectives)
                {
                    print(objective.CostFunction(layout));
                    costs.Add((objective.name, objective.CostFunction(layout)));
                }
                result.Add((gameObject.name, costs));
            } else {
                foreach (var element in UIElements)
                {
                    AdaptationManager adaptationManager = element.GetComponent<AdaptationManager>();
                    List<(string, float)> costs = new List<(string, float)>();
                    foreach (var objective in adaptationManager.LocalObjectiveHandler.Objectives)
                    {
                        costs.Add((objective.name, objective.CostFunction(adaptationManager.layout)));
                    }
                    result.Add((element.name, costs));
                }
            }

            print(result);
            return result;
        }

        public (List<Layout>, float) OptimizeLayout()
        {
            if (isActiveAndEnabled == false)
            {
                Debug.LogError($"[AdaptationManager.OptimizeLayout()]: AdaptationManager on {gameObject.name} is disabled!");
                return (new List<Layout> { layout }, 0.0f);
            }

            // The adaptation manager is responsible for knowing the layout (e.g. what to optimize)
            // The properties to be optimized should be obtained dynamically in the future, but for now we hardcode 
            // the properties we want to optimize.
            if (isGlobal)
            {
                List<List<LocalObjective>> objectives = new List<List<LocalObjective>>();
                List<Layout> layouts = new List<Layout>();
                foreach (var element in UIElements)
                {
                    AdaptationManager adaptationManager = element.GetComponent<AdaptationManager>();
                    objectives.Add(adaptationManager.LocalObjectiveHandler.Objectives);
                    layouts.Add(adaptationManager.layout);
                }
                
                if (objectives.Count == 0)
                {
                    Debug.LogWarning($"[AdaptationManager.OptimizeLayout()]: Unable to find any objectives on adaptation manager game objects...");
                    return (new List<Layout> { layout }, 0.0f);
                }
                
                // async starts
                if (waitingForOptimization == false)
                {
                    waitingForOptimization = true;
                    if (AsyncSolverOptimizeCoroutine != null)
                        StopCoroutine(AsyncSolverOptimizeCoroutine);
                    AsyncSolverOptimizeCoroutine = StartCoroutine(asyncSolver.OptimizeCoroutine(layouts, objectives, hyperparameters));
                }

                if (asyncSolver.Result.Item1 != null)
                {
                    waitingForOptimization = false;
                }

                return (asyncSolver.Result.Item1, asyncSolver.Result.Item2);
                
                // return solver.Optimize(layouts, objectives, hyperparameters);
            }
            if (LocalObjectiveHandler.Objectives.Count == 0)
            {
                Debug.LogWarning($"[AdaptationManager.OptimizeLayout()]: Unable to find any objectives on adaptation manager game object...");
                return (new List<Layout> { layout }, 0.0f);
            }
            // return solver.Optimize(layout, LocalObjectiveHandler.Objectives, hyperparameters);
            
            if (waitingForOptimization == false)
            {
                waitingForOptimization = true;
                if (AsyncSolverOptimizeCoroutine != null)
                    StopCoroutine(AsyncSolverOptimizeCoroutine);
                AsyncSolverOptimizeCoroutine = StartCoroutine(asyncSolver.OptimizeCoroutine(layout, LocalObjectiveHandler.Objectives, hyperparameters));
            }

            if (asyncSolver.Result.Item1 != null)
            {
                waitingForOptimization = false;
            }

            return (asyncSolver.Result.Item1, asyncSolver.Result.Item2);
        }

        public (List<Layout>, float, float) AsyncOptimizeLayout()
        {
            if (isActiveAndEnabled == false)
            {
                Debug.LogError($"[AdaptationManager.AsyncOptimizeLayout()]: AdaptationManager on {gameObject.name} is disabled!");
                return (new List<Layout> { layout }, 0.0f, 0.0f);
            }

            // The adaptation manager is responsible for knowing the layout (e.g. what to optimize)
            // The properties to be optimized should be obtained dynamically in the future, but for now we hardcode 
            // the properties we want to optimize.
            if (isGlobal)
            {
                List<List<LocalObjective>> objectives = new List<List<LocalObjective>>();
                List<Layout> layouts = new List<Layout>();
                foreach (var element in UIElements)
                {
                    AdaptationManager adaptationManager = element.GetComponent<AdaptationManager>();
                    objectives.Add(adaptationManager.LocalObjectiveHandler.Objectives);
                    layouts.Add(adaptationManager.layout);
                }

                if (waitingForOptimization == false)
                {
                    waitingForOptimization = true;
                    if (AsyncSolverOptimizeCoroutine != null)
                        StopCoroutine(AsyncSolverOptimizeCoroutine);
                    AsyncSolverOptimizeCoroutine = StartCoroutine(asyncSolver.OptimizeCoroutine(layouts, objectives, hyperparameters));
                }

                if (asyncSolver.Result.Item1 != null)
                {
                    waitingForOptimization = false;
                }

                return (asyncSolver.Result);
            }

            if (LocalObjectiveHandler.Objectives.Count == 0)
                return (new List<Layout> { layout }, 0.0f, 0.0f);

            // if (waitingForOptimization == false)
            // {
            //     waitingForOptimization = true;
            //     if (asyncSolverOptimizeCoroutine != null)
            //         StopCoroutine(asyncSolverOptimizeCoroutine);
            //     asyncSolverOptimizeCoroutine = StartCoroutine(asyncSolver.OptimizeCoroutine(layout, LocalObjectiveHandler.Objectives, hyperparameters));
            // }
            //
            // if (asyncSolver.Result.Item1 != null)
            // {
            //     waitingForOptimization = false;
            // }

            // return (asyncSolver.Result);
            return (new List<Layout> { layout }, 0.0f, 0.0f);
        }

        public float ComputeCost()
        {
            if (!isActiveAndEnabled)
            {
                Debug.LogError($"[AdaptationManager.ComputeCost()]: AdaptationManager on {gameObject.name} is disabled!");
                return 0.0f;
            }
            if (isGlobal)
            {
                float startTime = Time.realtimeSinceStartup;
                float frameTime = 0.0f;

                List<List<LocalObjective>> globalObjectives = new List<List<LocalObjective>>();
                List<Layout> layouts = new List<Layout>();
                foreach (var element in UIElements)
                {
                    AdaptationManager adaptationManager = element.GetComponent<AdaptationManager>();
                    if (adaptationManager == null || adaptationManager.layout == null)
                        throw new Exception($"No local adaptation manager found on {element.name}");
                    globalObjectives.Add(adaptationManager.LocalObjectiveHandler.Objectives);
                    layouts.Add(adaptationManager.layout);
                }

                float cost = 0;
                for (int i = 0; i < UIElements.Count; i++)
                {
                    cost += globalObjectives[i].Sum(objective => objective.Weight * objective.CostFunction(layouts[i])) / globalObjectives[i].Count;
                }
                cost /= globalObjectives.Count;
                frameTime = Time.realtimeSinceStartup - startTime;
                return cost;
            }

            List<LocalObjective> objectives = LocalObjectiveHandler.Objectives;
            print("Total weighted cost: " + LocalObjectiveHandler.Objectives.Sum(objective => objective.Weight * objective.CostFunction(layout)) / objectives.Count);
            return LocalObjectiveHandler.Objectives.Sum(objective => objective.Weight * objective.CostFunction(layout)) / objectives.Count;
        }

        Coroutine computeCostCorutine;
        private float? ComputedCost = null;
        private bool waitingForComputeCost = false;

        public float? AsyncComputeCost()
        {
            if (this.isActiveAndEnabled == false)
            {
                Debug.LogError($"[AdaptationManager.AsyncComputeCost()]: AdaptationManager on {gameObject.name} is disabled!");
                return 0.0f;
            }

            if (waitingForComputeCost == false)
            {
                waitingForComputeCost = true;
                if (computeCostCorutine != null)
                    StopCoroutine(computeCostCorutine);
                computeCostCorutine = StartCoroutine(ComputeCostCoroutine());
            }

            if (ComputedCost != null)
            {
                waitingForComputeCost = false;
            }
            return ComputedCost;
        }

        public IEnumerator ComputeCostCoroutine()
        {
            if (!isActiveAndEnabled)
            {
                Debug.LogError($"[AdaptationManager.ComputeCostCoroutine()]: AdaptationManager on {gameObject.name} is disabled!");
                yield break;
            }
            ComputedCost = null;
            float startTime = Time.realtimeSinceStartup;
            float frameTime = 0.0f;
            float cost = 0;

            if (isGlobal)
            {
                List<List<LocalObjective>> globalObjectives = new List<List<LocalObjective>>();
                List<Layout> layouts = new List<Layout>();
                foreach (var element in UIElements)
                {
                    AdaptationManager adaptationManager = element.GetComponent<AdaptationManager>();
                    if (adaptationManager == null || adaptationManager.layout == null)
                        throw new Exception($"No local adaptation manager found on {element.name}");
                    globalObjectives.Add(adaptationManager.LocalObjectiveHandler.Objectives);
                    layouts.Add(adaptationManager.layout);
                }

                for (int i = 0; i < UIElements.Count; i++)
                {
                    foreach (var objective in globalObjectives[i])
                    {
                        cost += objective.Weight * objective.CostFunction(layouts[i]);
                        frameTime = Time.realtimeSinceStartup - startTime;
                        if (frameTime >= 0.01f)
                        {
                            Debug.Log($"Frame time: {frameTime}");
                            yield return new WaitForFixedUpdate();
                            startTime = Time.realtimeSinceStartup;
                        }
                    }
                    cost /= globalObjectives[i].Count;
                }
                cost /= globalObjectives.Count;
                ComputedCost = cost;
                yield break;
            }

            List<LocalObjective> objectives = LocalObjectiveHandler.Objectives;
            foreach (var objective in objectives)
            {
                cost += objective.Weight * objective.CostFunction(layout);
                frameTime = Time.realtimeSinceStartup - startTime;
                if (frameTime >= 0.01f)
                {
                    Debug.Log($"Frame time: {frameTime}");
                    yield return new WaitForFixedUpdate();
                    startTime = Time.realtimeSinceStartup;
                }
            }
        }
        
        public void Adapt(Layout layout)
        {
            if (!isActiveAndEnabled)
            {
                Debug.LogError($"[AdaptationManager.Adapt(layout)]: AdaptationManager on {gameObject.name} is disabled!");
                return;
            }

            // Strategy chooses what to adapt, so we will probably create multiple of these per supported property
            // To start, we follow one property transition per property
            // To allow multiple property transitions per property we should implement an extension to this later, 
            // allowing cases such as rule-based property transitions for a property (e.g.: replicate if previous
            // distance > x meters or use smooth movement otherwise.

            Adapt<IPositionAdaptation>(layout);
            Adapt<IRotationAdaptation>(layout);
            Adapt<IScaleAdaptation>(layout);

            // Tell registered adaptation listeners that a new adaptation occured
            InvokeAdaptationListerners(layout);
        }

        private void Adapt<T>(Layout layout)
        {
            List<T> adaptations = propertyTransitions.OfType<T>().ToList();
            if (adaptations.Count == 0)
            {
                // Debug.LogWarning($"No '{typeof(T)}' found on {this.name}");
                return;
            }

            T adaptation = adaptations[0];
            if (adaptation == null)
            {
                Debug.LogWarning($"No property transition of type {typeof(T)} found on {name}");
                return;
            }

            if (adaptation as IPositionAdaptation != null)
            {
                (adaptation as IPositionAdaptation).Adapt(transform, layout.Position);
                return;
            }
    
            if ((adaptation as IRotationAdaptation) != null)
            {
                (adaptation as IRotationAdaptation).Adapt(transform, layout.Rotation);
                return;
            }

            if (adaptation as IScaleAdaptation != null)
            {
                (adaptation as IScaleAdaptation).Adapt(transform, layout.Scale);
                return;
            }
        }

        public void RegisterAdaptationListener(AdaptationListener adaptationListener)
        {
            if (adaptationListeners.Contains(adaptationListener))
                return;

            adaptationListeners.Add(adaptationListener);
        }

        public void UnregisterAdaptationListener(AdaptationListener adaptationListener)
        {
            adaptationListeners.Remove(adaptationListener);
        }

        protected virtual void InvokeAdaptationListerners(Layout adaptation)
        {
            foreach (var adaptationListener in adaptationListeners)
            {
                adaptationListener.AdaptationUpdated(adaptation);
            }
        }
    }
}
