using System;
using System.Linq;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine.Serialization;

namespace AUIT.Extras
{
    [Serializable]
    public class EvaluationRequest
    {
        public string[] items;


        public override string ToString()
        {
            return items.First().ToString();
        }
    }
    
}