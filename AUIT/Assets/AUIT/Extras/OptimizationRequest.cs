using System;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine.Serialization;

namespace AUIT.Extras
{
    [Serializable]
    public class OptimizationRequest
    {
        public string managerId;
        public UIConfiguration initialLayout;
        public int nObjectives;
    }
}