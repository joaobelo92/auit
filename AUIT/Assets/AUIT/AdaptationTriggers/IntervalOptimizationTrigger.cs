using System;
using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.Extras;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AUIT.AdaptationTriggers
{
    public class IntervalOptimizationTrigger : AdaptationTrigger
    {

        [SerializeField]
        [Tooltip("Interval in seconds between each optimization")]
        private float interval = 5f;

        async void Start()
        {
            // wait till AdaptationManager is initialized
            while (AdaptationManager.initialized != true)
            {
                // wait for 100ms
                await UniTask.Delay(100);
            }

            ApplyStrategy();
        }

        public override async void ApplyStrategy()
        {
            if (enabled == false)
                return;

            Debug.Log("Interval Optimization Running...");
            
            OptimizationResponse response = await AdaptationManager.OptimizeLayout();
            
            AdaptationManager.Adapt(response.solutions);
            await UniTask.Delay(TimeSpan.FromSeconds(interval));
            ApplyStrategy();
        }
    }
}