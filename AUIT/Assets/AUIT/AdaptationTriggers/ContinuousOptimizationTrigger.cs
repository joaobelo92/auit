using System;
using System.Collections;
using System.Collections.Generic;
using AUIT.AdaptationObjectives;
using AUIT.Extras;
using Cysharp.Threading.Tasks;
using Unity.Multiplayer.Center.Common;
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

        [SerializeField]
        private bool verbose = true;

        private float _previousCost;

        public List<ObjectiveType> evaluationObjectives;

        protected void Start()
        {
            ApplyContinuously();
        }


        private async UniTaskVoid ApplyContinuously()
        {
            try
            {
                var token = this.GetCancellationTokenOnDestroy();
                await UniTask.Delay(TimeSpan.FromSeconds(1), DelayType.Realtime, PlayerLoopTiming.Update, token);

                while (enabled)
                {
                    ApplyStrategy();

                    await UniTask.Delay(TimeSpan.FromSeconds(0.2), DelayType.Realtime, PlayerLoopTiming.Update, token);
                }
            }
            catch (OperationCanceledException)
            {
                
            }
        }

        private bool ShouldApplyAdaptation()
        {
            _previousCost = Auit.ComputeCost(evaluationObjectives: evaluationObjectives, verbose: verbose);
            return enabled && _previousCost > optimizationThreshold;
        }

        public override async void ApplyStrategy()
        {
            if (!Auit.isActiveAndEnabled)
                return;

            if (!ShouldApplyAdaptation())
                return;

            var response = await Auit.OptimizeLayout();
            
            if (response.costs[0] >= _previousCost)
                return;
            
            Auit.Adapt(response.solutions);
        }
    }
}