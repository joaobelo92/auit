using System;
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
        private float targetDistance = 0.5f;

        [SerializeField]
        private int angleInterval = 80;

        [SerializeField]
        private int angleIntervalCostRange = 30;

        [SerializeField]
        private float optimalDistanceRange = 0.1f;

        [SerializeField]
        private float maximumCostDistance = 0.25f;

        [SerializeField]
        private float yAxisOptimalOrigin = 0.15f;

        [SerializeField]
        private float yAxisOptimalRange = 0.2f;

        [SerializeField]

        protected override void Start()
        {
            base.Start();
            objectiveType = ObjectiveType.TargetDistance;
        }

        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            if (targetContextSource == null)
            {
                Debug.LogError("DistanceIntervalObjective.CostFunction(): Target context source is not set.");
            }

            // Debug.Log(targetContextSource.name);

            Transform userTransform = targetContextSource.GetValue().transform;
            Vector3 distanceVector = userTransform.InverseTransformPoint(optimizationTarget.Position);
            // In user's coordinate system
            Vector2 distanceVectorXZ = new Vector2(distanceVector.x, distanceVector.z);

            float totalCost = 0f;

            float angle = Vector2.Angle(distanceVectorXZ, Vector2.right);
            
            if (angle > angleInterval)
            {
                float excess = angle - angleInterval;
                float angleCost = excess / angleIntervalCostRange;
                totalCost += angleCost;
            }

            float yAxis = distanceVector.y - yAxisOptimalOrigin;

            if (Mathf.Abs(yAxis) > yAxisOptimalRange)
            {
                float excess = Mathf.Abs(yAxis) - yAxisOptimalRange;
                float yCost = excess / maximumCostDistance;
                totalCost += yCost;
            }

            totalCost = Mathf.Clamp01(totalCost);
            return totalCost;
        }

        public override Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout)
        {
            // Pick random position in optimal zone
            if (Random.value > 0.5f)
            {
                Vector2 point = Quaternion.Euler(0, 0, Random.Range(0, angleInterval)) * Vector2.up;
                Transform userTransform = targetContextSource.GetValue().transform;
            }
            Transform userTransform = targetContextSource.GetValue().transform;
            Vector3 distanceVector = userTransform.InverseTransformPoint(optimizationTarget.Position);
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