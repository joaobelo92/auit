
using System;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.AdaptationObjectives.Extras;
using UnityEngine;

namespace AUIT.AdaptationObjectives
{
    public class LookTowardsObjective : LocalObjective
    {
        [SerializeField]
        private ContextSource<Transform> targetContextSource;

        [SerializeField]
        private float angleThreshold = 90f;

        private enum Direction
        {
            LookAt,
            LookAway
        }
        
        [SerializeField]
        private Direction lookTowards = Direction.LookAway;

        public override ObjectiveType ObjectiveType => ObjectiveType.LookTowards;

        private void Reset()
        {
        }

        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            if (targetContextSource == null)
            {
                Debug.LogError("LookTowardsObjective.CostFunction(): Target context source is not set.");
            }

            Vector3 targetPosition = targetContextSource.GetValue().position;

            Matrix4x4 TRS = Matrix4x4.TRS(optimizationTarget.Position, optimizationTarget.Rotation, transform.lossyScale);
            Vector3 orientationVector = new Vector3(TRS.m02, TRS.m12, TRS.m22);
            float angle = Vector3.Angle(optimizationTarget.Position - targetPosition, orientationVector);

            float goalAngle = lookTowards == Direction.LookAway ? 0 : 180f;

            float cost = Mathf.Abs(angle - goalAngle) / angleThreshold;
            return Mathf.Clamp01(cost);

        }

        public override Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout)
        {
            if (targetContextSource == null)
            {
                Debug.LogError("LookTowardsObjective.OptimizationRule(): Target context source is not set.");
            }

            Vector3 targetPosition = targetContextSource.GetValue().position;
            Vector3 forward = (optimizationTarget.Position - targetPosition).normalized;
            Quaternion rotationAligned = Quaternion.LookRotation(lookTowards == Direction.LookAway ? forward : -forward, Vector3.up);

            Layout result = optimizationTarget.Clone();
            result.Rotation = Quaternion.Lerp(optimizationTarget.Rotation, rotationAligned, HelperMath.SampleNormalDistribution(1f, 0.5f));

            return result;
        }

        public override Layout DirectRule(Layout optimizationTarget)
        {
            throw new NotImplementedException();
        }

        public override float[] GetParameters()
        {
            throw new System.NotImplementedException();
        }

        public override void SetParameters(float[] parameters)
        {
            throw new System.NotImplementedException();
        }
    }
}
