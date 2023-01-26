using System;
using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine.Serialization;

namespace AUIT.Extras
{
    [Serializable]
    public class EvaluationResponse
    {
        public List<List<float>> costs;

        public override string ToString()
        {
            // Convert to JSON string without using JsonUtility
            string json = "{ \"costs\": [";
            for (int i = 0; i < costs.Count; i++)
            {
                json += "[";
                for (int j = 0; j < costs[i].Count; j++)
                {
                    json += costs[i][j];
                    if (j < costs[i].Count - 1)
                    {
                        json += ", ";
                    }
                }
                json += "]";
                if (i < costs.Count - 1)
                {
                    json += ", ";
                }
            }
            json += "] }";
            return json;
        }
    }
}