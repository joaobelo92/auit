using System;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;
using UnityEngine.Serialization;

namespace AUIT.Extras
{
    [Serializable]
    public class OptimizationResponse
    {
        public string solutions;  // will be UIConfiguration[]
        public string suggested;  // will be UIConfiguration
    }
}