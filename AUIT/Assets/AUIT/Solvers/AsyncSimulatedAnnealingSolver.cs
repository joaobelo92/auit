using System.Collections.Generic;
using System.Linq;
using AUIT.AdaptationObjectives;
using AUIT.AdaptationObjectives.Definitions;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AUIT.Solvers
{
    [System.Serializable]
    public class AsyncSimulatedAnnealingSolver : IAsyncSolver
    {
        [Tooltip("Number of iterations the solver will run for. A higher " +
                 "number can lead to better solutions but take longer to " +
                 "execute.")]
        public int iterations = 1500;
        public float minimumTemperature = 0.000001f;
        public float initialTemperature = 10000f;
        // annealingSchedule = alpha
        public float annealingSchedule = 0.98f;
        public float earlyStopping = 0.02f;
        public int iterationsPerFrame = 50;

        public override async UniTask<(List<List<Layout>>, float)> OptimizeCoroutine(
            List<Layout> initialLayouts, 
            List<List<LocalObjective>> objectives
            )
        {
            float cost = float.PositiveInfinity;
            List<Layout> bestLayout = initialLayouts.Select(item => item.Clone()).ToList();

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
                float temperature = Mathf.Max(minimumTemperature, initialTemperature * Mathf.Pow(annealingSchedule, i));
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
            
            return (new List<List<Layout>> { bestLayout }, cost);
        }
    }
}