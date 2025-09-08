using AUIT.AdaptationObjectives.Definitions;
using AUIT.ContextSources;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AUIT.AdaptationObjectives
{
    public class TargetDistanceObjective : LocalObjective
    {
        // In this case optimization target must be a transform
        // Add validation in the future
        [SerializeField]
        private TransformContextSource targetContextSource;

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
        private bool debugOptimalArea = false;

        private void Update()
        {
            if (debugOptimalArea)
            {
                DrawDebugLines();
            }
        }

        private void DrawDebugLines()
        {
            Transform userTransform = targetContextSource.GetValue();

            int angleStep = 5; // degrees
            int yStep = 5; // cm
            float minAngle = -angleInterval;
            float maxAngle = angleInterval;
            float minY = yAxisOptimalOrigin - yAxisOptimalRange;
            float maxY = yAxisOptimalOrigin + yAxisOptimalRange;
            float minDist = targetDistance - optimalDistanceRange;
            float maxDist = targetDistance + optimalDistanceRange;
            float distStep = 0.05f; // meters

            for (float angle = minAngle; angle <= maxAngle; angle += angleStep)
            {
                Quaternion rot = Quaternion.Euler(0, angle, 0);
                Vector3 dir = rot * Vector3.forward;

                for (float dist = minDist; dist <= maxDist; dist += distStep)
                {
                    for (float y = minY; y <= maxY; y += yStep * 0.01f)
                    {
                        Vector3 localPoint = new Vector3(dir.x * dist, y, dir.z * dist);
                        Vector3 worldPoint = userTransform.TransformPoint(localPoint);
                        Debug.DrawLine(worldPoint, worldPoint + Vector3.up * 0.05f, Color.green, 0.05f, false);
                    }
                }
            }
        }

        public override ObjectiveType ObjectiveType => ObjectiveType.TargetDistance;
        
        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            if (targetContextSource == null)
            {
                Debug.LogError("DistanceIntervalObjective.CostFunction(): Target context source is not set.");
            }

            // Debug.Log(targetContextSource.name);

            Transform userTransform = targetContextSource.GetValue();
            Vector3 distanceVector = userTransform.InverseTransformPoint(optimizationTarget.Position);
            // In user's coordinate system
            Vector2 distanceVectorXZ = new Vector2(distanceVector.x, distanceVector.z);

            // print(distanceVectorXZ + " magnitude: " + distanceVectorXZ.magnitude);

            float totalCost = 0f;

            float angle = Vector2.Angle(distanceVectorXZ, Vector2.up);

            if (angle > angleInterval)
            {
                float excess = angle - angleInterval;
                float angleCost = excess / angleIntervalCostRange;
                totalCost += angleCost;
            }

            float distance = distanceVectorXZ.magnitude - targetDistance;

            if (Mathf.Abs(distance) > optimalDistanceRange)
            {
                float excess = Mathf.Abs(distance) - optimalDistanceRange;
                float distanceCost = excess / maximumCostDistance;
                totalCost += distanceCost;
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

            Layout result = optimizationTarget.Clone();

            // Pick random position in optimal zone
            if (Random.value > 0.5f)
            {
                Vector3 point = Quaternion.Euler(0, Random.Range(0, angleInterval), 0) * Vector3.forward * targetDistance;
                Transform userTransform = targetContextSource.GetValue();
                Vector3 newPosition = userTransform.TransformPoint(new Vector3(point.x, Random.Range(yAxisOptimalOrigin - yAxisOptimalRange, yAxisOptimalOrigin + yAxisOptimalRange), point.z));
                result.Position = newPosition;
            }
            else
            {
                result.Position += 0.05f * Random.onUnitSphere;
            }

            return result;
        }

        public override Layout DirectRule(Layout optimizationTarget)
        {

            //Vector3 distanceVector = GetDistanceVector(optimizationTarget);
            Layout result = optimizationTarget.Clone();
            //result.Position = optimizationTarget.Position + distanceVector;
            return result;
        }

        // private new void OnEnable()
        // {
        //     base.OnEnable();

        //     if (targetContextSource == null)
        //     {
        //         targetContextSource = GetUserPoseContextSource();
        //     }
        // }

        public override float[] GetParameters()
        {
            return new[] { weight, targetDistance, optimalDistanceRange, maximumCostDistance };
        }

        public override void SetParameters(float[] parameters)
        {
            weight = parameters[0];
            targetDistance = parameters[1];
            optimalDistanceRange = parameters[2];
            maximumCostDistance = parameters[3];
        }
        

    }
}