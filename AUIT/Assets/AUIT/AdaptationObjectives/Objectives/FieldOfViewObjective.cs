using AUIT.AdaptationObjectives.Definitions;
using AUIT.AdaptationObjectives.Extras;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AUIT.AdaptationObjectives
{
    public class FieldOfViewObjective : LocalObjective
    {
        [SerializeField]
        private PeripheralVisionBoundary peripheralVisionBoundary = PeripheralVisionBoundary.Near;
        [SerializeField]
        private float maxAngle = 90f;

        private Quaternion quaternion;
        private float[] boundaryOrigin = { 0f, 3.5f, 17.5f, 45f };
        private float[] boundaryInterval = { 2f, 1.5f, 12.5f, 18f };

        [SerializeField]
        private bool useCustomBoundary = false;
        [SerializeField]
        private float customBoundaryOrigin = 12.0f;
        [SerializeField]
        private float customBoundaryInterval = 3.0f;

        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            // Idea: get angle between gaze and object vectors on y and x axis
            // Then we define intervals that are acceptable, e.g. comprising near/mid/far peripheral view
            // Cost function increases the further is is from that interval
            Transform contextSourceTransform = (Transform)ContextSourceTransformTarget;
            Vector3 target = contextSourceTransform.worldToLocalMatrix.MultiplyPoint3x4(optimizationTarget.Position);

            float angle = Vector3.Angle(Vector3.forward, target);

            // Inspired by https://en.wikipedia.org/wiki/Peripheral_vision
            // float cost = Mathf.Max(Mathf.Abs(rotation - boundaryOrigin[index]), boundaryDifference[index]) - boundaryDifference[index];
            float angleDiff;
            
            if (useCustomBoundary)
            {
                angleDiff = Mathf.Max(Mathf.Abs(angle - customBoundaryOrigin), customBoundaryInterval) - customBoundaryInterval;
            }
            else
            {
                int index = (int)peripheralVisionBoundary;
                angleDiff = Mathf.Max(Mathf.Abs(angle - boundaryOrigin[index]), boundaryInterval[index]) - boundaryInterval[index];
            }

            float cost = Mathf.Min(angleDiff / maxAngle, 1);
            return cost;
        }

        public override Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout)
        {
            Transform contextSourceTransform = (Transform)ContextSourceTransformTarget;
            // Would be efficient to cache rotation when cost function is computed
            Vector3 target = contextSourceTransform.worldToLocalMatrix.MultiplyPoint3x4(optimizationTarget.Position);

            float angle = Mathf.Acos(Vector3.Dot(Vector3.forward, target) / target.magnitude) * Mathf.Rad2Deg;

            int index = (int)peripheralVisionBoundary;
            float dir = angle - boundaryInterval[index] > 0 ? 1 : -1;

            Layout result = optimizationTarget.Clone();

            if (Random.value < 0.5f)
            {
                Vector3 move = contextSourceTransform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(0, 0, target.magnitude)) - optimizationTarget.Position;
                result.Position = optimizationTarget.Position + move * HelperMath.SampleNormalDistribution(0.1f, 0.1f) * dir;
            }
            else // move some cm at random
            {
                result.Position = optimizationTarget.Position + Random.insideUnitSphere * HelperMath.SampleNormalDistribution(0.5f, 0.5f) * 0.01f;
            }

            return result;
        }

        public override Layout DirectRule(Layout optimizationTarget)
        {
            Transform contextSourceTransform = (Transform)ContextSourceTransformTarget;
            // Would be efficient to cache rotation when cost function is computed
            Vector3 target = contextSourceTransform.worldToLocalMatrix.MultiplyPoint3x4(optimizationTarget.Position);
            quaternion = Quaternion.FromToRotation(target, Vector3.forward);

            Layout result = optimizationTarget.Clone();
            result.Position = contextSourceTransform.localToWorldMatrix.MultiplyPoint(quaternion * target);

            return result;
        }
    }
}