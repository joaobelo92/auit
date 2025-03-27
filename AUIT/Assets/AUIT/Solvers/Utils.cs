using System.Collections.Generic;
using AUIT.AdaptationObjectives;
using AUIT.AdaptationObjectives.Definitions;
using NUnit.Framework;

namespace AUIT.Solvers
{
    public static class Utils
    {
        public static (List<List<float>>, List<float>) ComputeCosts(List<Layout> currentLayout, List<List<LocalObjective>> objectives,
            List<MultiElementObjective> multiElementObjectives)
        {
            List<List<float>> objectiveCosts = new List<List<float>>();
            List<float> multiObjectiveCosts = new List<float>();
            float totalWeights = 0;
            for (int j = 0; j < currentLayout.Count; j++)
            {
                List<float> costs = new List<float>();
                for (int k = 0; k < objectives[j].Count; k++)
                {
                    float weight = objectives[j][k].Weight;
                    float objectiveCost = weight * objectives[j][k].CostFunction(currentLayout[j]);
                    costs.Add(objectiveCost);
                    totalWeights += weight;
                }
                objectiveCosts.Add(costs);
            }

            // Here is where we compute the multi-element objectives
            for (int j = 0; j < multiElementObjectives.Count; j++)
            {
                // Yi Fei: Updating to include conderation of weight
                float weight = multiElementObjectives[j].Weight;
                float objectiveCost = weight * multiElementObjectives[j].CostFunction(currentLayout.ToArray());
                multiObjectiveCosts.Add(objectiveCost);
                totalWeights += weight;
            }

            if (totalWeights > 0)
            {
                for (int j = 0; j < currentLayout.Count; j++)
                {
                    for (int k = 0; k < objectives[j].Count; k++)
                    {
                        objectiveCosts[j][k] /= totalWeights;
                    }
                }
                for (int j = 0; j < multiObjectiveCosts.Count; j++)
                {
                    multiObjectiveCosts[j] /= totalWeights;
                }
            }

            return (objectiveCosts, multiObjectiveCosts);
        }

        public static (List<List<float>>, List<float>) ComputeCostsUnweighted(List<Layout> currentLayout, List<List<LocalObjective>> objectives,
            List<MultiElementObjective> multiElementObjectives)
        {
            List<List<float>> objectiveCosts = new List<List<float>>();
            List<float> multiObjectiveCosts = new List<float>();
            for (int j = 0; j < currentLayout.Count; j++)
            {
                List<float> costs = new List<float>();
                for (int k = 0; k < objectives[j].Count; k++)
                {
                    costs.Add(objectives[j][k].CostFunction(currentLayout[j]));
                }
                objectiveCosts.Add(costs);
            }

            // Here is where we compute the multi-element objectives
            for (int j = 0; j < multiElementObjectives.Count; j++)
            {
                multiObjectiveCosts.Add(multiElementObjectives[j].CostFunction(currentLayout.ToArray()));
            }

            return (objectiveCosts, multiObjectiveCosts);
        }
    }
}