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
        private float distanceThreshold = 0.3f;

        
        
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
                moveDirection += Random.insideUnitSphere * Random.Range(0f, 0.3f);
                result.Position = position + moveDirection * distance * HelperMath.SampleNormalDistribution(1f, 0.5f);
            }

            return result;
        }

        public override Layout DirectRule(Layout optimizationTarget)
        {
            throw new System.NotImplementedException();
        }
    }
}