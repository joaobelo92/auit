using AUIT.AdaptationObjectives.Definitions;
using AUIT.AdaptationObjectives.Extras;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AUIT.AdaptationObjectives
{
    public class DistanceIntervalObjective : LocalObjective
    {
        // In this case optimization target must be a transform
        // Add validation in the future
        [SerializeField]
        private float goalXYDistance = 0.3f;
        [SerializeField]
        private float yInterval = 0.25f;
        [SerializeField]
        private float distanceXZInterval = 0.1f;
        [SerializeField]
        private float maxDistance = 2f;

        public void Reset()
        {
            ContextSource = ContextSource.PlayerPose;
        }

        protected override void Start()
        {
            base.Start();
            if (ContextSource == ContextSource.Gaze)
            {
                ContextSource = ContextSource.PlayerPose;
            }
        }

        private Vector3 GetDistanceVector(Layout optimizationTarget)
        {
            // y values should not be changed here, otherwise we have a sphere instead of a radius from the user
            Vector3 targetPosition = (Vector3)ContextSourceTransformTarget;
            Vector3 currentPosition = optimizationTarget.Position;

            Vector3 distanceVector = targetPosition - currentPosition;
            return distanceVector;
        }

        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            // Get distance from user on x and z axis
            Vector3 distanceVector = GetDistanceVector(optimizationTarget);
            // TODO: check if possible to delegate context
            Vector3 targetPosition = (Vector3)ContextSourceTransformTarget;
            Vector3 currentPosition = optimizationTarget.Position;

            // distance y axis
            float height = Mathf.Abs(targetPosition.y - currentPosition.y);
            float heightFromInterval = Mathf.Max(0, height - yInterval);

            // distance xz plane
            distanceVector.y = 0;
            float distanceXZ = Mathf.Abs(distanceVector.magnitude - goalXYDistance);
            float distanceFromInterval = Mathf.Max(0, distanceXZ - distanceXZInterval);
            // Add distance in XZ axis to distance in Y axis 
            float totalDistance = heightFromInterval + distanceFromInterval;
            return Mathf.Min(totalDistance / maxDistance, 1);
        }

        public override Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout)
        {
            Vector3 distanceVector = GetDistanceVector(optimizationTarget);
            distanceVector.y = 0;
            Vector3 targetPosition = (Vector3)ContextSourceTransformTarget;
            Vector3 currentPosition = optimizationTarget.Position;

            float distance = distanceVector.magnitude - goalXYDistance;
            distanceVector = Vector3.Normalize(distanceVector);
            Layout result = optimizationTarget.Clone();
            // Two different strategies
            if (Random.value > 0.5f)
            {
                Vector3 position = optimizationTarget.Position + HelperMath.SampleNormalDistribution(distance, 0.5f) * distanceVector;
                position.y = currentPosition.y + (targetPosition.y - currentPosition.y) * HelperMath.SampleNormalDistribution(1f, 0.5f);
                result.Position = position;
            }
            else // just move at random
            {
                float x = HelperMath.SampleNormalDistribution(0.5f, 0.5f) * 0.01f;
                float y = HelperMath.SampleNormalDistribution(0.5f, 0.5f) * 0.01f;
                float z = HelperMath.SampleNormalDistribution(0.5f, 0.5f) * 0.01f;
                Vector3 position = optimizationTarget.Position + new Vector3(x, y, z);
                result.Position = position;
            }

            // Debug.Log($"{distance}, {distanceVector}, {result.Position} {CostFunction(optimizationTarget)} {CostFunction(result)}");
            return result;
        }

        public override Layout DirectRule(Layout optimizationTarget)
        {
            Vector3 distanceVector = GetDistanceVector(optimizationTarget);
            Layout result = optimizationTarget.Clone();
            result.Position = optimizationTarget.Position + distanceVector;
            return result;
        }
    }
}