using System;
using System.Collections.Generic;
using System.Linq;
using AUIT.Extras;
using AUIT.AdaptationObjectives;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.Constraints;
using Cysharp.Threading.Tasks;
using Numpy;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AUIT.Solvers
{
    [Serializable]
    public class SimulatedAnnealingSolver : IAsyncSolver
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
        public int iterationsPerFrame = 100;

        private Vector3 ConstrainPosition(Vector3 position)
        {
            Vector3 constrainedPosition = position;
            List<Constraint> constraints = AUIT.Instance.GetConstraints();
            foreach (Constraint constraint in constraints)
            {
                switch (constraint.type)
                {
                    case ConstraintType.SpatialXAxis:
                        constrainedPosition.x = Mathf.Clamp(constrainedPosition.x, constraint.minimum, constraint.maximum);
                        break;
                    case ConstraintType.SpatialYAxis:
                        constrainedPosition.y = Mathf.Clamp(constrainedPosition.y, constraint.minimum, constraint.maximum);
                        break;
                    case ConstraintType.SpatialZAxis:
                        constrainedPosition.z = Mathf.Clamp(constrainedPosition.z, constraint.minimum, constraint.maximum);
                        break;
                }
            }
            return constrainedPosition;
        }

        public override async UniTask<(OptimizationResponse, NDarray, NDarray)> OptimizeCoroutine(
            List<Layout> initialLayouts, 
            List<List<LocalObjective>> objectives,
            List<MultiElementObjective> multiElementObjectives,
            bool saveCosts=false
            )
        {
            float cost = float.PositiveInfinity;
            List<Layout> bestLayout = initialLayouts.Select(item => item.Clone()).ToList();

            List<List<float>> objectiveCosts = new List<List<float>>();
            List<float> totalObjectiveCosts = new List<float>();
            List<float> multiObjectiveCosts = new List<float>();
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
                
                for (int j = 0; j < multiElementObjectives.Count; j++)
                {
                    // Yi Fei: Updating to include conderation of weight
                    float objectiveCost = multiElementObjectives[j].Weight * multiElementObjectives[j].CostFunction(bestLayout.ToArray());
                    multiObjectiveCosts.Add(objectiveCost);
                }
            }

            for (int i = 0; i < iterations; i++)
            {
                float temperature = Mathf.Max(minimumTemperature, initialTemperature * Mathf.Pow(annealingSchedule, i));
                List<Layout> currentLayout = bestLayout.Select(item => item.Clone()).ToList();

                // get highest objective and use its optimization rule
                // A lot of possible optimizations here (e.g. iterating multiple times through costs)... for now this will do.

                int maxCostElementIndex = -1;
                int maxCostObjectiveIndex = -1;
                int maxMultiObjectiveIndex = -1;
                float maxCostElement = 0f;
                float maxCostObjective = 0f;
                float maxCostMultiObjective = 0f;

                if (objectives.Count > 0)
                {
                    maxCostElement = totalObjectiveCosts.Max();
                    maxCostElementIndex = totalObjectiveCosts.IndexOf(maxCostElement);
                    if (objectiveCosts[maxCostElementIndex].Count > 0)
                    {
                        maxCostObjective = objectiveCosts[maxCostElementIndex].Max();
                        maxCostObjectiveIndex = objectiveCosts[maxCostElementIndex].IndexOf(maxCostObjective);
                    }
                }

                if (multiElementObjectives.Count > 0)
                {
                    maxCostMultiObjective = multiObjectiveCosts.Max();
                    maxMultiObjectiveIndex = multiObjectiveCosts.IndexOf(maxCostMultiObjective);
                }
                
                // Optimal solution found 
                if (maxCostMultiObjective <= 0 && maxCostElement <= 0)
                {
                    bestLayout = currentLayout;
                    break;
                }

                if (maxCostObjective > maxCostMultiObjective)
                {
                    Layout maxCostLayout = objectives[maxCostElementIndex][maxCostObjectiveIndex].OptimizationRule(currentLayout[maxCostElementIndex]);
                    maxCostLayout.Position = ConstrainPosition(maxCostLayout.Position); // Constrain the position of the layout
                    currentLayout[maxCostElementIndex] = maxCostLayout;
                }
                else
                {
                    currentLayout = multiElementObjectives[maxMultiObjectiveIndex].OptimizationRule(currentLayout);
                    for (int j = 0; j < currentLayout.Count; j++)
                    {
                        if (currentLayout[j] != null)
                        {
                            currentLayout[j].Position = ConstrainPosition(currentLayout[j].Position); // Constrain the position of the layout
                        }
                    }
                }
                
                objectiveCosts = new List<List<float>>();
                totalObjectiveCosts = new List<float>();
                multiObjectiveCosts = new List<float>();
                for (int j = 0; j < currentLayout.Count; j++)
                {
                    List<float> costs = new List<float>();
                    float totalCost = 0;
                    for (int k = 0; k < objectives[j].Count; k++)
                    {
                        float objectiveCost = objectives[j][k].Weight * objectives[j][k].CostFunction(currentLayout[j]);
                        totalCost += objectiveCost;
                        costs.Add(objectiveCost);
                    }
                    objectiveCosts.Add(costs);
                    totalObjectiveCosts.Add(totalCost);
                }
                
                // Here is where we compute the multi-element objectives
                for (int j = 0; j < multiElementObjectives.Count; j++)
                {
                    float objectiveCost = multiElementObjectives[j].Weight * multiElementObjectives[j].CostFunction(currentLayout.ToArray());
                    multiObjectiveCosts.Add(objectiveCost);
                }

                // (objectiveCosts, multiObjectiveCosts) = Utils.ComputeCosts(currentLayout, objectives, multiElementObjectives);
                
                totalObjectiveCosts = objectiveCosts.Select(item => item.Sum()).ToList();
                
                float currentCost = totalObjectiveCosts.Sum() + multiObjectiveCosts.Sum();

                // Early stopping 
                if (currentCost <= earlyStopping)
                {
                    bestLayout = currentLayout;
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
            return (new OptimizationResponse(best, cost), null, null);
            // return (new List<List<Layout>> { bestLayout }, cost);
        }
    }
}