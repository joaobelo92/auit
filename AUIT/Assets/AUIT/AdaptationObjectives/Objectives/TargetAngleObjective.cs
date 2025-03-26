using AUIT.AdaptationObjectives.Definitions;
using AUIT.AdaptationObjectives.Extras;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AUIT.AdaptationObjectives
{
    public class TargetAngleObjective : LocalObjective
    {
        [SerializeField]
        private ContextSource<Transform> userContextSource;

        [SerializeField]
        [Min(0)]
        private float targetAngle = 0f;

        [SerializeField]
        [Min(0)]
        private float innerAngleDistance = 10f;

        [SerializeField]
        [Min(0)]
        private float outerAngleDistance = 45f;

        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            if (userContextSource == null)
            {
                Debug.LogError("FieldOfViewObjective.CostFunction(): User context source is not set.");
            }

            // Idea: get angle between gaze and object vectors on y and x axis
            // Then we define intervals that are acceptable, e.g. comprising near/mid/far peripheral view
            // Cost function increases the further is is from that interval
            Transform contextSourceTransform = userContextSource.GetValue();
            Vector3 target = contextSourceTransform.worldToLocalMatrix.MultiplyPoint3x4(optimizationTarget.Position);

            float angle = Vector3.Angle(Vector3.forward, target);

            // Inspired by https://en.wikipedia.org/wiki/Peripheral_vision
            // float cost = Mathf.Max(Mathf.Abs(rotation - boundaryOrigin[index]), boundaryDifference[index]) - boundaryDifference[index];
            float angleDiff = Mathf.Abs(angle - targetAngle);

            float cost = (angleDiff - innerAngleDistance) / outerAngleDistance;
            cost = Mathf.Clamp01(cost);
            return cost;
        }

        public override Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout)
        {
            if (userContextSource == null)
            {
                Debug.LogError("FieldOfViewObjective.OptimizationRule(): User context source is not set.");
            }

            Transform contextSourceTransform = userContextSource.GetValue();
            // Would be efficient to cache rotation when cost function is computed
            Vector3 target = contextSourceTransform.worldToLocalMatrix.MultiplyPoint3x4(optimizationTarget.Position);

            Layout result = optimizationTarget.Clone();

            if (Random.value < 0.5f)
            {
                Vector3 move = contextSourceTransform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(0, 0, target.magnitude)) - optimizationTarget.Position;
                result.Position = optimizationTarget.Position + move * HelperMath.SampleNormalDistribution(0.1f, 0.1f);
            }
            else // move some cm at random
            {
                result.Position = optimizationTarget.Position + Random.insideUnitSphere * HelperMath.SampleNormalDistribution(0.5f, 0.5f) * 0.01f;
            }

            return result;
        }

        public override Layout DirectRule(Layout optimizationTarget)
        {
            // Transform contextSourceTransform = userContextSource.GetValue();
            // Would be efficient to cache rotation when cost function is computed
            // Vector3 target = contextSourceTransform.worldToLocalMatrix.MultiplyPoint3x4(optimizationTarget.Position);
            // Quaternion quaternion = Quaternion.FromToRotation(target, Vector3.forward);

            Layout result = optimizationTarget.Clone();
            // result.Position = contextSourceTransform.localToWorldMatrix.MultiplyPoint(quaternion * target);

            return result;
        }

        private new void OnEnable()
        {
            base.OnEnable();

            if (userContextSource == null)
            {
                userContextSource = GetUserPoseContextSource();
            }
        }
    }
}