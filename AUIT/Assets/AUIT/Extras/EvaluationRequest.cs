using System;
using System.Linq;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine.Serialization;

namespace AUIT.Extras
{
    [Serializable]
    public class EvaluationRequest
    {
        public UIConfiguration[] layouts;


        public override string ToString()
        {
            return layouts.First().ToString();
        }
    }
    
}