using AUIT.AdaptationObjectives.Definitions;
using AUIT.AdaptationObjectives.Extras;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AUIT.AdaptationObjectives
{
    public class ConstantViewSizeObjective : LocalObjective
    {
        [SerializeField]
        private float scalingFactor = 1.0f;
        [SerializeField]
        private float maxScaleThreshold = 0.5f;

        [SerializeField]
        private float scalingFactorInterval = 0.1f;

        [SerializeField]
        private float initialDist = 0.5f;
        private Vector3 initialScale;

        public void Reset()
        {
            ContextSource = ContextSource.Gaze;
        }

        protected override void Start()
        {
            base.Start();
            // Ensure that ContextSource is a Transform
            if (ContextSource == ContextSource.PlayerPose)
            {
                ContextSource = ContextSource.Gaze;
            }

            // Ensure that ContextSource is a Transform
            Transform contextSourceTransform = ContextSourceTransformTarget as Transform;
            if (contextSourceTransform == null)
                return;

            // Get initial distance to context source; Desired distance should not be dependent on the scene layout
            // initialDist = (transform.position - contextSourceTransform.position).magnitude;
            // Get inistal local scale
            initialScale = transform.localScale;
        }

        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            // Ensure that ContextSource is a Transform
            Transform contextSourceTransform = ContextSourceTransformTarget as Transform;
            if (contextSourceTransform == null)
                return 1.0f;
            
            // Get current distance to context source
            float currentDistance = (transform.position - contextSourceTransform.position).magnitude;
            
            // Get ideal local scale
            Vector3 idealScale = initialScale * (currentDistance / initialDist * scalingFactor);

            // We get magnitude of the scale vector since in unity it has 3 dimensions
            float cost = (optimizationTarget.Scale - idealScale).magnitude;

            float costTolerance = Mathf.Max(0, cost - scalingFactorInterval);
            
            // Normalize
            cost = Mathf.Min(costTolerance / maxScaleThreshold, 1);
            
            return cost;
        }

        public override Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout = null)
        {
            // Ensure that ContextSource is a Transform
            Transform contextSourceTransform = ContextSourceTransformTarget as Transform;
            if (contextSourceTransform == null)
                return optimizationTarget;

            Layout result = optimizationTarget.Clone();
            Vector3 scale = optimizationTarget.Scale;
            if (Random.value < 0.5f)
            {
                // Get current distance to context source
                float currentDistance = (transform.position - contextSourceTransform.position).magnitude;
                // Get ideal local scale
                Vector3 idealScale = (currentDistance / initialDist) * initialScale;
                scale = idealScale;
            }
            else {
                // Multiply the scale based on randomness
                float randomValue = Mathf.Abs(HelperMath.SampleNormalDistribution(0f, 0.5f));
                scale *= randomValue;
            }

            result.Scale = scale;
            return result;
        }

        public override Layout DirectRule(Layout optimizationTarget)
        {
            // Ensure that ContextSource is a Transform
            Transform contextSourceTransform = ContextSourceTransformTarget as Transform;
            if (contextSourceTransform == null)
                return optimizationTarget;

            // Get current distance to context source
            float currentDistance = (transform.position - contextSourceTransform.position).magnitude;
            // Get ideal local scale
            Vector3 idealScale = (currentDistance / initialDist) * initialScale * scalingFactor;
            return new Layout(optimizationTarget.Position, optimizationTarget.Rotation, idealScale);
        }
    }
}