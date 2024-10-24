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
        public UIConfiguration suggested;

        public OptimizationResponse(UIConfiguration suggested)
        {
            this.suggested = suggested;
        }
    }
}