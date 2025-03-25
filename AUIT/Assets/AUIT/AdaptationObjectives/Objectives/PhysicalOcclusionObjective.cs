using AUIT.AdaptationObjectives.Definitions;
using AUIT.AdaptationObjectives.Extras;
using System.Linq;
using UnityEngine;

namespace AUIT.AdaptationObjectives
{
    public class PhysicalOcclusionObjective : LocalObjective
    {
        [SerializeField]
        private ContextSource<Transform> userContextSource;

        [SerializeField]
        private LayerMask physicalLayerMask;

        private bool IsOccluding(Layout optimizationTarget)
        {
            Transform contextSourceTransform = userContextSource.GetValue();
            Vector3 toElement = optimizationTarget.Position - contextSourceTransform.position;
            Vector3 direction = toElement.normalized;
            float distance = direction.magnitude;
            return Physics.Raycast(contextSourceTransform.position, direction, distance, physicalLayerMask);
        }

        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            if (userContextSource == null)
            {
                Debug.LogError("PhysicalOcclusionObjective.CostFunction(): User context source is not set.");
            }

            if (IsOccluding(optimizationTarget))
            {
                return 1.0f; // High cost if occluded
            }
            else
            {
                return 0;
            }
        }

        public override Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout = null)
        {
            if (userContextSource == null)
            {
                Debug.LogError("PhysicalOcclusionObjective.OptimizationRule(): User context source is not set.");
            }

            Layout result = optimizationTarget.Clone();

            Vector3 moveDirection = Random.insideUnitSphere;
            moveDirection *= Random.Range(0, 0.3f);

            if (IsOccluding(optimizationTarget))
            {
                Transform contextSourceTransform = userContextSource.GetValue();
                Vector3 occludedDirection = (optimizationTarget.Position - contextSourceTransform.position).normalized;
                // Get perpendicular plane to the occluded direction
                Vector3 tangent = Vector3.Cross(occludedDirection, Vector3.up);
                Vector3 bitangent = Vector3.Cross(occludedDirection, tangent);
                float angle = Random.Range(0, 2 * Mathf.PI);
                Vector3 randomDirection = (tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle)).normalized;

                moveDirection += randomDirection; 
            }

            result.Position += 0.05f * HelperMath.SampleNormalDistribution(1f, 0.5f) * moveDirection;

            return result; 
        }

        public override Layout DirectRule(Layout optimizationTarget)
        {
            throw new System.NotImplementedException();
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