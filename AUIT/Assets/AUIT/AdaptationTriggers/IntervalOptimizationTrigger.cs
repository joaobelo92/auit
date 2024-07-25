using System;
using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AUIT.AdaptationTriggers
{
    public class IntervalOptimizationTrigger : AdaptationTrigger
    {

        [SerializeField]
        private float interval = 5f;

        void Start()
        {
            ApplyStrategy();
        }

        public override async void ApplyStrategy()
        {
            if (enabled == false)
                return;

            Debug.Log("Interval Optimization Running...");
            
            var (layouts, cost) = await AdaptationManager.OptimizeLayout();
            
            AdaptationManager.Adapt(layouts);
            await UniTask.Delay(TimeSpan.FromSeconds(interval));
            ApplyStrategy();
        }
    }
}