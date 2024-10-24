using System.Collections;
using AUIT.Extras;
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

        // [Header("Limit optimization run time")]
        // [SerializeField]
        // private float optimizationTimeout = 5.0f;
        // private float optimizationTimeStart = 0.0f;

        private float previousCost;

        protected void Start()
        {
            StartCoroutine(ApplyContinuously());
        }

        private IEnumerator ApplyContinuously()
        {
            yield return new WaitForSecondsRealtime(0.5f);

            while (true)
            {
                if (enabled == false)
                {
                    yield break;
                }

                ApplyStrategy();

                yield return new WaitForSecondsRealtime(.5f);
            }
        }

        private bool ShouldApplyAdaptation()
        {
            bool costIsBelowOptiThreshold;
            previousCost = AdaptationManager.ComputeCost();
            costIsBelowOptiThreshold = previousCost <= optimizationThreshold;

            return enabled && !costIsBelowOptiThreshold;
        }

        public override async void ApplyStrategy()
        {
            if (AdaptationManager.isActiveAndEnabled == false)
                return;
                
            if (!ShouldApplyAdaptation())
                return;

            // if (runAsynchronous)
            // {
            //     StartCoroutine(WaitForOptimizedLayout());
            //     return;
            // }

            OptimizationResponse response = await AdaptationManager.OptimizeLayout();

            bool shouldAdapt = true;
            print($"Threshold not working, need to add cost logic in Optimization Response");
            if (shouldAdapt)
                AdaptationManager.Adapt(response.solutions);
        }
    }
}