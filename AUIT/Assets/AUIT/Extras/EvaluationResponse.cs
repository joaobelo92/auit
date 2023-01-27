using System;
using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine.Serialization;
using Newtonsoft.Json;

namespace AUIT.Extras
{
    [Serializable]
    public class EvaluationResponse
    {
        [JsonProperty]
        public List<List<float>> costs;
    }
}