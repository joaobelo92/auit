
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AUIT.AdaptationObjectives;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AUIT.Solvers
{
    public class AsyncSimulatedAnnealingSolver : IAsyncSolver
    {
        public float FrameTimeThreshold = .005f;
        public float MaxFrameTime = 0.10f;

        (List<Layout>, float, float) result;
        private IAsyncSolver asyncSolverImplementation;
        public AdaptationManager adaptationManager { get; set; }
        
        // todo: fix
        public (List<List<Layout>>, float, float) Result { get; set; }

        // hyperparemeters; [0] Iterations [1] Minimum temperature; [2] Initial temperature; [3] alpha
        public void Initialize()
        {
            
        }

        public IEnumerator OptimizeCoroutine(Layout layout, List<LocalObjective> objectives, List<float> hyperparameters)
        {
            result = (null, 0.0f, 0.0f);

            float cost = float.PositiveInfinity;
            Layout bestLayout = layout.Clone();
            int iterations = (int)hyperparameters[0];
            float minTemperature = hyperparameters[1];
            float initialTemperature = hyperparameters[2];
            float alpha = hyperparameters[3];
            float earlyStopping = hyperparameters[4];

            float startTime = Time.realtimeSinceStartup;
            float frameTime;
            for (int i = 0; i < iterations; i++)
            {
                float temperature = Mathf.Max(minTemperature, initialTemperature * Mathf.Pow(alpha, i));
                // Randomly access one of the objectives optimization rule
                // TODO: add way to define how aggressively the rule is applied, to get neighbors that are further away
                int objectiveIndex = Random.Range(0, objectives.Count);
                
                Layout currentLayout = objectives[objectiveIndex].OptimizationRule(bestLayout, layout);
                float currentCost = objectives.Sum(objective => objective.Weight * objective.CostFunction(currentLayout, layout)) / objectives.Count;

                // Early stopping 
                if (currentCost <= earlyStopping)
                {
                    bestLayout = currentLayout;
                    cost = currentCost;
                    break;
                }

                float delta = currentCost - cost;
                if (delta < 0 || Random.value < Mathf.Exp(-delta / temperature))
                {
                    bestLayout = currentLayout;
                    cost = currentCost;
                }

                frameTime = Time.realtimeSinceStartup - startTime;
                if (i % 50 == 0 || frameTime >= FrameTimeThreshold)
                {
                    // Debug.Log($"Frame time: {frameTime}");
                    yield return new WaitForFixedUpdate();
                    startTime = Time.realtimeSinceStartup;
                }
            }

            float previousCost = objectives.Sum(objective => objective.CostFunction(layout)) / objectives.Count;

            result = (new List<Layout> { bestLayout }, cost, previousCost);
        }

        public IEnumerator OptimizeCoroutine(List<Layout> initialLayouts, List<List<LocalObjective>> objectives, List<float> hyperparameters)
        {
            result = (null, 0.0f, 0.0f);

            float cost = float.PositiveInfinity;
            List<Layout> bestLayout = initialLayouts.Select(item => item.Clone()).ToList();
            int iterations = (int)hyperparameters[0];
            float minTemperature = hyperparameters[1];
            float initialTemperature = hyperparameters[2];
            float alpha = hyperparameters[3];
            float earlyStopping = hyperparameters[4];

            List<List<float>> objectiveCosts = new List<List<float>>();
            List<float> totalObjectiveCosts = new List<float>();
            for (int i = 0; i < bestLayout.Count; i++)
            {
                List<float> costs = new List<float>();
                float totalCost = 0;
                for (int j = 0; j < objectives[i].Count; j++)
                {
                    float objectiveCost = objectives[i][j].Weight * objectives[i][j].CostFunction(bestLayout[i]);
                    totalCost += objectiveCost;
                    costs.Add(objectiveCost);
                }
                objectiveCosts.Add(costs);
                totalObjectiveCosts.Add(totalCost);
            }

            float startTime = Time.realtimeSinceStartup;
            float frameTime = 0.0f;
            for (int i = 0; i < iterations; i++)
            {
                float temperature = Mathf.Max(minTemperature, initialTemperature * Mathf.Pow(alpha, i));
                List<Layout> currentLayout = bestLayout.Select(item => item.Clone()).ToList();

                // get highest objective and use its optimization rule
                // A lot of possible optimizations here (e.g. iterating multiple times through costs)... for now this will do.
                float maxCostElement = totalObjectiveCosts.Max();
                int maxCostElementIndex = totalObjectiveCosts.IndexOf(maxCostElement);
                float maxCostObjective = objectiveCosts[maxCostElementIndex].Max();
                int maxCostObjectiveIndex = objectiveCosts[maxCostElementIndex].IndexOf(maxCostObjective);

                currentLayout[maxCostElementIndex] = objectives[maxCostElementIndex][maxCostObjectiveIndex].OptimizationRule(currentLayout[maxCostElementIndex]);

                objectiveCosts = new List<List<float>>();
                totalObjectiveCosts = new List<float>();
                for (int j = 0; j < currentLayout.Count; j++)
                {
                    List<float> costs = new List<float>();
                    float totalCost = 0;
                    for (int k = 0; k < objectives[j].Count; k++)
                    {
                        float objectiveCost = objectives[j][k].Weight * objectives[j][k].CostFunction(currentLayout[j]) / objectives[j].Count;
                        totalCost += objectiveCost;
                        costs.Add(objectiveCost);
                    }
                    objectiveCosts.Add(costs);
                    totalObjectiveCosts.Add(totalCost);
                }

                float currentCost = totalObjectiveCosts.Sum() / totalObjectiveCosts.Count;

                // Early stopping 
                if (currentCost <= earlyStopping)
                {
                    bestLayout = currentLayout;
                    cost = currentCost;
                    break;
                }

                float delta = currentCost - cost;
                if (delta < 0 || Random.value < Mathf.Exp(-delta / temperature))
                {
                    bestLayout = currentLayout;
                    cost = currentCost;
                }

                frameTime = Time.realtimeSinceStartup - startTime;
                if (i % 50 == 0 || frameTime >= FrameTimeThreshold)
                {
                    yield return new WaitForFixedUpdate();
                    startTime = Time.realtimeSinceStartup;
                }
            }

            float previousCost = 0;
            for (int i = 0; i < initialLayouts.Count; i++)
            {
                previousCost += objectives[i].Sum(objective => objective.Weight * objective.CostFunction(initialLayouts[i])) / objectives.Count;
            }
            previousCost /= objectives.Count;
            
            result = (bestLayout, cost, previousCost);
        }
    }
}