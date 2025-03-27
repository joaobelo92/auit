using AUIT.AdaptationObjectives;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.AdaptationObjectives.Extras;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace AUIT.AdaptationObjectives
{
    public class PhysicalOcclusionObjective : LocalObjective
    {
        [SerializeField]
        private ContextSource<Transform> userContextSource;

        [SerializeField]
        private LayerMask physicalLayerMask;

        private Vector3[] GetCheckPoints(Layout layout)
        {
            // Assuming x = width, y = height
            // Checking center and corners of the layout element
            Vector3[] checkTargets = new Vector3[] {
                new Vector3 (-0.5f, -0.5f, 0),
                new Vector3 (-0.5f, 0.5f, 0),
                new Vector3 (0.5f, -0.5f, 0),
                new Vector3 (0.5f, 0.5f, 0),
                new Vector3 (0, 0, 0),
            };
            Matrix4x4 trs = Matrix4x4.TRS(layout.Position, layout.Rotation, layout.Scale);
            for (int i = 0; i < checkTargets.Length; i++)
            {
                checkTargets[i] = trs.MultiplyPoint(checkTargets[i]);
            }

            return checkTargets;
        }

        private bool IsOccluding(Vector3 optimizationTarget)
        {
            Transform contextSourceTransform = userContextSource.GetValue();
            Vector3 toElement = optimizationTarget - contextSourceTransform.position;
            Vector3 direction = toElement.normalized;
            float distance = toElement.magnitude;
            bool occluding = Physics.Raycast(contextSourceTransform.position, direction, distance, physicalLayerMask);
            return occluding;
        }


        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            if (userContextSource == null)
            {
                Debug.LogError("PhysicalOcclusionObjective.CostFunction(): User context source is not set.");
            }
            
            float overlaps = 0;
            Vector3[] checkTargets = GetCheckPoints(optimizationTarget);
            foreach (Vector3 target in checkTargets)
            {
                if (IsOccluding(target))
                {
                    overlaps += 1; 
                }
            }
            return overlaps / checkTargets.Length;
        }

        private Vector3 GetPlanarDirection(Layout optimizationTarget)
        {
            Transform contextSourceTransform = userContextSource.GetValue();
            Vector3 occludedDirection = (optimizationTarget.Position - contextSourceTransform.position).normalized;
            Vector3 tangent = Vector3.Cross(occludedDirection, Vector3.up);
            Vector3 bitangent = Vector3.Cross(occludedDirection, tangent);
            float angle = Random.Range(0, 2 * Mathf.PI);
            Vector3 randomDirection = (tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle)).normalized;
            return randomDirection;

        }

        public override Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout = null)
        {
            if (userContextSource == null)
            {
                Debug.LogError("PhysicalOcclusionObjective.OptimizationRule(): User context source is not set.");
            }

            Layout result = optimizationTarget.Clone();

            Vector3 moveDirection = Vector3.zero;

            float moveStrategy = Random.value;
            if (moveStrategy < 0.33)
            {
                Vector3[] checkPoints = GetCheckPoints(optimizationTarget);
                Vector3 center = checkPoints[4];
                for (int i = 0; i < 4; i++)
                {
                    Vector3 corner = checkPoints[i];
                    if (IsOccluding(corner))
                    {
                        moveDirection += (corner - center).normalized;
                    }
                }
                moveDirection.Normalize();
            } else if (moveStrategy < 0.66) {
                moveDirection = GetPlanarDirection(optimizationTarget);
            } else
            {
                moveDirection = Random.onUnitSphere;
            }

            result.Position += 0.05f * HelperMath.SampleNormalDistribution(1, 0.5f) * moveDirection;

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

            physicalLayerMask = LayerMask.GetMask("Physical Environment");
        }
    }

}
