using System;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine.Serialization;

namespace AUIT.Extras
{
    [Serializable]
    public class OptimizationRequest
    {
        public Layout layout;
        public int nObjectives;
    }
}