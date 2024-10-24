using System.Collections.Generic;
using System.Linq;
using AUIT.AdaptationObjectives;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.Extras;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AUIT.Solvers
{
    public class AsyncSimulatedAnnealingSolver : IAsyncSolver
    {
        private IAsyncSolver asyncSolverImplementation;
        public AdaptationManager AdaptationManager { get; set; }
        
        // todo: fix
        public (List<List<Layout>>, float, float) Result { get; set; }

        // hyperparemeters; [0] Iterations [1] Minimum temperature; [2] Initial temperature; [3] alpha
        // [4] early stopping [5] iterations per frame
        public void Initialize()
        {
            
        }

        public async UniTask<OptimizationResponse> OptimizeCoroutine(List<Layout> initialLayouts, List<List<LocalObjective>> objectives, List<float> hyperparameters)
        {
            float cost = float.PositiveInfinity;
            List<Layout> bestLayout = initialLayouts.Select(item => item.Clone()).ToList();
            int iterations = (int)hyperparameters[0];
            float minTemperature = hyperparameters[1];
            float initialTemperature = hyperparameters[2];
            float alpha = hyperparameters[3];
            float earlyStopping = hyperparameters[4];
            int iterationsPerFrame = (int)hyperparameters[5];

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

                if (i % iterationsPerFrame == 0)
                {
                    await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                }
            }

            UIConfiguration best = new UIConfiguration(bestLayout.ToArray());
            return new OptimizationResponse(best);
            
        }
    }
}