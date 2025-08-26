using System;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;
using UnityEngine.Serialization;

namespace AUIT.Extras
{
    [Serializable]
    public class OptimizationResponse
    {
        public UIConfiguration[] solutions;
        public float[] costs;
        public UIConfiguration suggested;

        public OptimizationResponse(UIConfiguration suggested)
        {
            this.suggested = suggested;
            solutions = new[] { suggested };
        }

        public OptimizationResponse(UIConfiguration suggested, float suggestedCost)
        {
            this.suggested = suggested;
            solutions = new[] { suggested };
            costs = new[] { suggestedCost };
        }
    }
}