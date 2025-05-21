using AUIT.AdaptationObjectives.Definitions;
using AUIT.AdaptationObjectives.Extras;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AUIT.AdaptationObjectives
{
    public class TargetDistanceObjective : LocalObjective
    {
        // In this case optimization target must be a transform
        // Add validation in the future
        [SerializeField]
        private ContextSource<Transform> targetContextSource;

        [SerializeField]
        private float targetDistance = 0.3f;

        [SerializeField]
        private float optimalDistanceRange = 0.1f;

        [SerializeField]
        private float maximumCostDistanceRange = 0.5f;

        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            if (targetContextSource == null)
            {
                Debug.LogError("DistanceIntervalObjective.CostFunction(): Target context source is not set.");
            }
            
            // Debug.Log(targetContextSource.name);

            Vector3 targetPosition = targetContextSource.GetValue().position;
            Vector3 currentPosition = optimizationTarget.Position;

            Vector3 distanceVector = targetPosition - currentPosition;
            distanceVector.y = 0;
            float distance = Mathf.Abs(distanceVector.magnitude - targetDistance);

            float cost = (distance - optimalDistanceRange) / maximumCostDistanceRange;
            cost = Mathf.Clamp01(cost);
            return cost;
        }

        public override Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout)
        {
            if (targetContextSource == null)
            {
                Debug.LogError("DistanceIntervalObjective.OptimizationRule(): Target context source is not set.");
            }

            Vector3 targetPosition = targetContextSource.GetValue().position;
            Vector3 currentPosition = optimizationTarget.Position;
            Vector3 displacement = targetPosition - currentPosition;
            displacement.y = 0;
            float distance = displacement.magnitude - targetDistance;
            Vector3 direction = Mathf.Sign(distance) * displacement.normalized;

            Layout result = optimizationTarget.Clone();


            // Two different strategies
            if (Random.value > 0.5f)
            {
                direction.y = Mathf.Sign(targetPosition.y - currentPosition.y);
                direction.Normalize();
                Vector3 position = optimizationTarget.Position + 0.05f * HelperMath.SampleNormalDistribution(1.0f, 0.5f) * direction;
                //position.y = currentPosition.y + (targetPosition.y - currentPosition.y) * HelperMath.SampleNormalDistribution(0.1f, 0.1f);
                result.Position = position;
            }
            else // just move at random
            {
                Vector3 position = optimizationTarget.Position + 0.05f * HelperMath.SampleNormalDistribution(1.0f, 0.5f) * Random.onUnitSphere;
                result.Position = position;
            }

            // Debug.Log($"{distance}, {distanceVector}, {result.Position} {CostFunction(optimizationTarget)} {CostFunction(result)}");
            return result;
        }

        public override Layout DirectRule(Layout optimizationTarget)
        {

            //Vector3 distanceVector = GetDistanceVector(optimizationTarget);
            Layout result = optimizationTarget.Clone();
            //result.Position = optimizationTarget.Position + distanceVector;
            return result;
        }

        private new void OnEnable()
        {
            base.OnEnable();

            if (targetContextSource == null)
            {
                targetContextSource = GetUserPoseContextSource();
            }
        }

        public override float[] GetParameters()
        {
            return new[] { weight, targetDistance, optimalDistanceRange, maximumCostDistanceRange };
        }

        public override void SetParameters(float[] parameters)
        {
            weight = parameters[0];
            targetDistance = parameters[1];
            optimalDistanceRange = parameters[2];
            maximumCostDistanceRange = parameters[3];
        }
    }
}