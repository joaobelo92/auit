using System;
using System.Collections;
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

        private float _previousCost;

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

                    await UniTask.Delay(TimeSpan.FromSeconds(0.4), DelayType.Realtime, PlayerLoopTiming.Update, token);
                }
            }
            catch (OperationCanceledException)
            {
                
            }
        }

        private bool ShouldApplyAdaptation()
        {
            _previousCost = Auit.ComputeCost();
            return enabled && _previousCost > optimizationThreshold;
        }

        public override async void ApplyStrategy()
        {
            if (!Auit.isActiveAndEnabled)
                return;

            if (!ShouldApplyAdaptation())
                return;

            var response = await Auit.OptimizeLayout();
            
            Auit.Adapt(response.solutions);
        }
    }
}