using System.Collections.Generic;
using System.Linq;
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
                for (int k = 0; k < objectives[j].Count; k++)
                {
                    float weight = objectives[j][k].Weight;
                    float objectiveCost = weight * objectives[j][k].CostFunction(currentLayout[j]);
                    costs.Add(objectiveCost);
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
            }

            return (objectiveCosts, multiObjectiveCosts);
        }

        public static float ComputeCost(Layout[] layout, List<List<LocalObjective>> objectives,
            List<MultiElementObjective> multiElementObjectives)
        {
            float cost = 0;
            float weight = 0;
            for (int li = 0; li < layout.Length; li++)
            {
                List<LocalObjective> localObjectives = objectives[li];
                Layout layoutElement = layout[li];
                cost += localObjectives.Sum(objective => objective.Weight * objective.CostFunction(layoutElement));
                weight += localObjectives.Sum(objective => objective.Weight);
            }
            cost += multiElementObjectives.Sum(objective => objective.Weight * objective.CostFunction(layout));
            weight += multiElementObjectives.Sum(objective => objective.Weight);
            return cost / weight; 
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