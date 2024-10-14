using System.Collections.Generic;
using System.Linq;
using AUIT.AdaptationObjectives;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AUIT.Solvers
{
    public class SimulatedAnnealingSolver
    {
        // hyperparemeters; [0] Iterations [1] Minimum temperature; [2] Initial temperature; [3] alpha
        public (List<Layout>, float) Optimize(Layout layout, List<LocalObjective> objectives, List<float> hyperparameters)
        {
            // Debug.Log($"Optimisation starting: SA, {objectives.Count} objectives");
            // foreach (LocalObjective objective in objectives)
            // {
            //     Debug.Log($"{objective}");
            // }

            float cost = float.PositiveInfinity;
            Layout bestLayout = layout.Clone();
            int iterations = (int)hyperparameters[0];
            float minTemperature = hyperparameters[1];
            float initialTemperature = hyperparameters[2];
            float alpha = hyperparameters[3];
            float earlyStopping = hyperparameters[4];

            for (int i = 0; i < iterations; i++)
            {
                float temperature = Mathf.Max(minTemperature, initialTemperature * Mathf.Pow(alpha, i));
                // Randomly access one of the objectives optimization rule
                // TODO: add way to define how aggressively the rule is applied, to get neighbors that are further away
                int objectiveIndex = Random.Range(0, objectives.Count);
                // float previousCost = objectives.Sum(objective => objective.CostFunction(bestLayout, layout));
                Layout currentLayout = objectives[objectiveIndex].OptimizationRule(bestLayout, layout);
                float currentCost = objectives.Sum(objective => objective.Weight * objective.CostFunction(currentLayout, layout)) / objectives.Count;

                // foreach (var objective in objectives)
                // {
                //     Debug.Log(objective.GetType() + " cost: " + objective.CostFunction(currentLayout, layout));
                // }

                // Early stopping 
                if (currentCost <= earlyStopping)
                {
                    bestLayout = currentLayout;
                    cost = currentCost;
                    break;
                }

                // Debug.Log($"{previousCost}, {currentCost}, {objectiveIndex}");
                float delta = currentCost - cost;
                // Debug.Log($"{delta} limit {Mathf.Exp(-delta / temperature)}");
                if (delta < 0 || Random.value < Mathf.Exp(-delta / temperature))
                {
                    // Debug.Log($"Took step, solver {objectiveIndex} prev l {bestLayout.Position} curr l {currentLayout.Position}");
                    bestLayout = currentLayout;
                    cost = currentCost;
                }
            }

            // Debug.Log($"Optimisation: SA; total cost: {cost}");
            // foreach (var objective in objectives)
            // {
            //     Debug.Log($"cost for {objective}: {objective.CostFunction(bestLayout)}");
            // }
            // float previousCost = objectives.Sum(objective => objective.CostFunction(layout)) / objectives.Count;
            // Debug.LogWarning("BestLayout: " + bestLayout);
            List<Layout> result = new List<Layout>();
            result.Add(bestLayout);
            return (result, cost);
        }

        public (List<Layout>, float) Optimize(List<Layout> initialLayouts, List<List<LocalObjective>> objectives, List<float> hyperparameters)
        {
            // float start = Time.realtimeSinceStartup;
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

            // foreach (var p in objectives[0])
            // {
            //     Debug.Log($"{p}, {p.gameObject.name}");
            // }
            
            // for (int i = 0; i < iterations; i++)
            for (int i = 0; i < iterations; i++)
            {
                float temperature = Mathf.Max(minTemperature, initialTemperature * Mathf.Pow(alpha, i));
                // Randomly access one of the objectives optimization rule
                // TODO: add way to define how aggressively the rule is applied, to get neighbors that are further away
                // float previousCost = objectives.Sum(objective => objective.CostFunction(bestLayout, layout));
                // do move at random
                List<Layout> currentLayout = bestLayout.Select(item => item.Clone()).ToList();
                
                // Random optimization rules
                // for (int j = 0; j < initialLayouts.Count; j++)
                // {
                //     int elementIndex = Random.Range(0, initialLayouts.Count);
                //     int objectiveIndex = Random.Range(0, objectives[elementIndex].Count);
                //     currentLayout[elementIndex] = objectives[elementIndex][objectiveIndex].OptimizationRule(currentLayout[elementIndex]);
                // }
                
                // get highest objective and use its optimization rule
                // A lot of possible optimizations here (e.g. iterating multiple times through costs)... for now this will do.
                float maxCostElement = totalObjectiveCosts.Max();
                int maxCostElementIndex = totalObjectiveCosts.IndexOf(maxCostElement);
                float maxCostObjective = objectiveCosts[maxCostElementIndex].Max();
                int maxCostObjectiveIndex = objectiveCosts[maxCostElementIndex].IndexOf(maxCostObjective);
                
                currentLayout[maxCostElementIndex] = objectives[maxCostElementIndex][maxCostObjectiveIndex].OptimizationRule(currentLayout[maxCostElementIndex]);
                
                // float currentCost = 0;
                // for (int j = 0; j < initialLayouts.Count; j++)
                // {
                //     currentCost += objectives[j].Sum(objective => objective.Weight * objective.CostFunction(currentLayout[j])) / objectives[j].Count;
                // }
                // currentCost /= objectives.Count;
                
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

                // Debug.Log($"{previousCost}, {currentCost}, {objectiveIndex}");
                float delta = currentCost - cost;
                // Debug.Log($"{delta} limit {Mathf.Exp(-delta / temperature)}");
                if (delta < 0 || Random.value < Mathf.Exp(-delta / temperature))
                {
                    // Debug.Log($"Took step, solver {objectiveIndex} prev l {bestLayout.Position} curr l {currentLayout.Position}");
                    bestLayout = currentLayout;
                    cost = currentCost;
                }
            }

            // Debug.Log($"Optimisation: SA; total cost: {cost}");
            // foreach (var objective in objectives)
            // {
            //     Debug.Log($"cost for {objective}: {objective.CostFunction(bestLayout)}");
            // }
            // float previousCost = 0;
            // for (int i = 0; i < initialLayouts.Count; i++)
            // {
            //     previousCost += objectives[i].Sum(objective => objective.Weight * objective.CostFunction(initialLayouts[i])) / objectives.Count;
            // }
            // previousCost /= objectives.Count;
            // Debug.LogWarning("BestLayout: " + bestLayout);
            // float totalTime = Time.realtimeSinceStartup - start;
            // Debug.Log(totalTime);
            return (bestLayout, cost);
        }
    }
}