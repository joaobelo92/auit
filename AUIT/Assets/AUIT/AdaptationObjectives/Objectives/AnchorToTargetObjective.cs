using AUIT.AdaptationObjectives.Definitions;
using AUIT.AdaptationObjectives.Extras;
using UnityEngine;

namespace AUIT.AdaptationObjectives
{
    public class AnchorToTargetObjective : LocalObjective
    {
        [SerializeField, Tooltip("Position in Local Coordinates")]
        private Vector3 offset;

        [SerializeField]
        private float distanceThreshold = 0.3f;
        
        // Start is called before the first frame update
        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            Transform contextSourceTransform = (Transform)ContextSourceTransformTarget;
            Vector3 positionLocalCoordinates = contextSourceTransform.worldToLocalMatrix.MultiplyPoint3x4(optimizationTarget.Position);

            float distance = Vector3.Distance(positionLocalCoordinates, offset);
            float cost = Mathf.Min(distance / distanceThreshold, 1);

            return cost;
        }

        public override Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout = null)
        {
            Transform contextSourceTransform = (Transform)ContextSourceTransformTarget;
            Vector3 target = contextSourceTransform.localToWorldMatrix.MultiplyPoint3x4(offset);
            
            Layout result = optimizationTarget.Clone();

            // Return optimal position
            if (Random.value < 0.33f)
            {
                result.Position = target;
            }
            // Move randomly towards desired position
            else
            {
                Vector3 position = contextSourceTransform.position;
                float distance = Vector3.Distance(position, offset);
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