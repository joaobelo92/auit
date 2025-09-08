using AUIT.AdaptationObjectives.Definitions;
using AUIT.AdaptationObjectives.Extras;
using UnityEngine;

namespace AUIT.AdaptationObjectives
{
    public class AnchorToTargetObjective : LocalObjective
    {
        [SerializeField]
        private ContextSource<Transform> targetContextSource;

        [SerializeField, Tooltip("Position in Local Coordinates")]
        private Vector3 offset;

        [SerializeField]
        private float distanceThreshold = 2.0f;

        public override ObjectiveType ObjectiveType => throw new System.NotImplementedException();



        // Start is called before the first frame update
        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            if (targetContextSource == null)
            {
                Debug.LogError("AnchorToTargetObjective.CostFunction(): Target context source is not set.");
            }

            // Vector3 positionLocalCoordinates = contextSourceTransform.worldToLocalMatrix.MultiplyPoint3x4(optimizationTarget.Position);

            Vector3 contextSourcePosition = targetContextSource.GetValue().position;
            Vector3 target = contextSourcePosition + offset;
            float distance = Vector3.Distance(optimizationTarget.Position, target);
            float cost = Mathf.Min(distance / distanceThreshold, 1);

            return cost;
        }

        public override Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout = null)
        {
            if (targetContextSource == null) {
                Debug.LogError("AnchorToTargetObjective.OptimizationRule(): Target context source is not set.");
            }

            Vector3 contextSourcePosition = targetContextSource.GetValue().position;
            Vector3 target = contextSourcePosition + offset;

            // Vector3 target = contextSourceTransform.localToWorldMatrix.MultiplyPoint3x4(offset);
            
            Layout result = optimizationTarget.Clone();

            // Return optimal position
            if (Random.value < 0.33f)
            {
                result.Position = target;
            }
            // Move randomly towards desired position
            else
            {
                Vector3 position = optimizationTarget.Position;
                float distance = Vector3.Distance(position, target);
                Vector3 moveDirection = Vector3.Normalize(target - position);
                // Randomize movement a little
                if (Random.value > 0.5)
                {
                    moveDirection += Random.onUnitSphere;
                }
                moveDirection.Normalize();
                result.Position = position + 0.05f * HelperMath.SampleNormalDistribution(1f, 0.5f) * moveDirection;
            }

            return result;
        }

        public override Layout DirectRule(Layout optimizationTarget)
        {
            throw new System.NotImplementedException();
        }

        public override float[] GetParameters()
        {
            return new float[] { weight, offset.x, offset.y, offset.z, distanceThreshold };
        }

        public override void SetParameters(float[] parameters)
        {
            weight = parameters[0];
            offset = new Vector3(parameters[1], parameters[2], parameters[3]);
            distanceThreshold = parameters[4];
        }
    }
}