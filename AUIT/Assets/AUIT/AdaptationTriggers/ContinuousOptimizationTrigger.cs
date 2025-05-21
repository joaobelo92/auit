using System;
using System.Collections;
using AUIT.Extras;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AUIT.AdaptationTriggers
{
    public class ContinuousOptimizationTrigger : AdaptationTrigger
    {
        // [SerializeField, Tooltip("Running asynchronously will spread computations over multiple frames, making the result come later but not heavily impact framerate.")]
        // private bool runAsynchronous = false;

        [Header("Thresholds")]
        [SerializeField]
        private float optimizationThreshold = 0.05f;
        [SerializeField]
        private float adaptationThreshold = 0.1f;

        private float previousCost;

        protected void Start()
        {
            ApplyContinuously();
        }


        private async UniTaskVoid ApplyContinuously()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(1), DelayType.Realtime);

            while (enabled)
            {
                ApplyStrategy();

                await UniTask.Delay(TimeSpan.FromSeconds(0.5), DelayType.Realtime);
            }
        }

        private bool ShouldApplyAdaptation()
        {
            previousCost = Auit.ComputeCost();
            return enabled && previousCost > optimizationThreshold;
        }

        public override async void ApplyStrategy()
        {
            if (!Auit.isActiveAndEnabled)
                return;

            // if (!ShouldApplyAdaptation())
            //     return;

            OptimizationResponse response = await Auit.OptimizeLayout();

            bool shouldAdapt = true;
            if (shouldAdapt)
                Auit.Adapt(response.solutions);
        }
    }
}