using AUIT.Extras;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace AUIT.AdaptationTriggers
{
    public class CallbackOptimizationTrigger : AdaptationTrigger
    {
        public override async void ApplyStrategy()
        {
            if (enabled == false)
                return;

            OptimizationResponse response = await Auit.OptimizeLayout();

            Auit.Adapt(response.solutions);
        }

    }
}