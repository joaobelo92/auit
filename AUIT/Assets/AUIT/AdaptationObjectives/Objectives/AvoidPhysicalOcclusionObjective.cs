using AUIT.AdaptationObjectives;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.AdaptationObjectives.Extras;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace AUIT.AdaptationObjectives
{
    public class AvoidPhysicalOcclusionObjective : LocalObjective
    {
        [SerializeField]
        private ContextSource<Transform> userContextSource;

        [SerializeField]
        private LayerMask physicalLayerMask;

        public override ObjectiveType ObjectiveType => throw new System.NotImplementedException();

        private Vector3[] GetCheckPoints(Layout layout)
        {
            // Assuming x = width, y = height
            // Checking center and corners of the layout element
            List<Vector3> checkTargetsLocal = new List<Vector3>();
            // pick the same odd number of points per axis to yield a meaningful mean and square root in the second alternative of the OptimizationRule
            for (float x = -0.5f; x <= 0.5f; x += 0.25f)
            {
                for (float y = -0.5f; y <= 0.5f; y += 0.25f)
                {
                    checkTargetsLocal.Add(new Vector3(x, y, 0));
                }
            }
            

            Matrix4x4 trs = Matrix4x4.TRS(layout.Position, layout.Rotation, layout.Scale);
            Vector3[] checkTargets = new Vector3[checkTargetsLocal.Count];
            for (int i = 0; i < checkTargetsLocal.Count; i++)
            {
                checkTargets[i] = trs.MultiplyPoint(checkTargetsLocal[i]);
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
            Vector3[] checkTargets = GetCheckPoints(optimizationTarget);
            foreach (Vector3 target in checkTargets)
            {
                if (IsOccluding(target))
                {
                    return 1;
                }
            }
            return 0; 
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
                int checkPointsCount = checkPoints.Length;
                int mean = (checkPointsCount - 1) / 2;            // this is only meaningful for an odd checkPointsCount 
                Vector3 center = checkPoints[mean];

                Vector3 ComputeMoveDirection(Vector3 moveDirection, int position)
                {
                    Vector3 corner = checkPoints[position];
                    if (IsOccluding(corner))
                    {
                        moveDirection += (center - corner).normalized;
                    }
                    return moveDirection;
                }

                // Alternative 1: invocation for all checkpoints (-> refactor this, if Alternative 2 is excluded)
                /*
                for (int i = 0; i < numberOfCheckpoints; i++)
                {
                    moveDirection = MoveDirection(moveDirection, i);
                }
                */
                
                // Alternative 2: invocation only for the corners of the object's plane (saving calls to IsOccluding())
                int sqrtOfCheckPointsCount = (int) Mathf.Sqrt(checkPointsCount);            // this is only meaningful for a square grid of checkPoints
                
                int bottomLeft = 0;
                int topLeft = sqrtOfCheckPointsCount - 1;
                int bottomRight = checkPointsCount - sqrtOfCheckPointsCount;
                int topRight = checkPointsCount - 1;
                
                moveDirection = ComputeMoveDirection(moveDirection, bottomLeft);
                moveDirection = ComputeMoveDirection(moveDirection, topLeft);
                moveDirection = ComputeMoveDirection(moveDirection, bottomRight);
                moveDirection = ComputeMoveDirection(moveDirection, topRight);
                
                moveDirection.Normalize();
            } else if (moveStrategy < 0.66) {
                moveDirection = GetPlanarDirection(optimizationTarget);
            } else
            {
                moveDirection = Random.onUnitSphere;
            }

            result.Position += Random.Range(0f, 0.1f) * moveDirection;

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

        public override float[] GetParameters()
        {
            return new float[] { weight };
        }

        public override void SetParameters(float[] parameters)
        {
            weight = parameters[0];
        }
    }

}
