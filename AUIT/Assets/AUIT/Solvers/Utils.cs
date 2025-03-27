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
            }

            // Here is where we compute the multi-element objectives
            for (int j = 0; j < multiElementObjectives.Count; j++)
            {
                // Yi Fei: Updating to include conderation of weight
                float objectiveCost = multiElementObjectives[j].Weight * multiElementObjectives[j].CostFunction(currentLayout.ToArray());
                multiObjectiveCosts.Add(objectiveCost);
            }
            
            return (objectiveCosts, multiObjectiveCosts);
        }
    }
}