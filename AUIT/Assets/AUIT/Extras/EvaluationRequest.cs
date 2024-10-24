using System;
using System.Collections.Generic;
using System.Linq;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine.Serialization;

namespace AUIT.Extras
{
    [Serializable]
    public class EvaluationRequest
    {
        public string manager_id { get; set; }
        public UIConfiguration[] layouts { get; set; }

        public override string ToString()
        {
            return layouts.First().ToString();
        }
    }
    
}