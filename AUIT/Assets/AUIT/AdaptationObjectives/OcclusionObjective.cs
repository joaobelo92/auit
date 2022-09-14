using System;
using System.Collections.Generic;
using System.Linq;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.AdaptationObjectives.Extras;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AUIT.AdaptationObjectives
{
    public class OcclusionObjective : LocalObjective
    {

        [Header("Options")]
        [SerializeField]
        private LayerMask occlusionMask = Physics.DefaultRaycastLayers;
        [SerializeField]
        private float stepMovement = 0.02f;
        [SerializeField]
        private int keyPointSubdivisions = 10;

        private Dictionary<Vector3, bool> keyPoints;

        [Header("Debugging")]
        public bool ShowDebugLines = false;

        private bool topRightIsOccluded;
        private bool bottomRightIsOccluded;
        private bool bottomLeftIsOccluded;
        private bool topLeftIsOccluded;

        private float minX = float.PositiveInfinity;
        private float minY = float.PositiveInfinity;
        private float minZ = float.PositiveInfinity;
        private float maxX = float.NegativeInfinity;
        private float maxY = float.NegativeInfinity;
        private float maxZ = float.NegativeInfinity;

        private Vector3 boundMin;
        private Vector3 boundMax;

        private float prevCost = 1f;

        private void Reset()
        {
            ContextSource = ContextSource.PlayerPose;
        }

        protected override void Start()
        {
            base.Start();
            if (ContextSource == ContextSource.Gaze)
            {
                ContextSource = ContextSource.PlayerPose;
            }
            occlusionMask &= ~(1 << this.gameObject.layer);

            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (GetComponent<Renderer>() != null)
            {
                renderers.Append(GetComponent<Renderer>());
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                minX = renderers[i].bounds.min.x < minX ? renderers[i].bounds.min.x : minX;
                minY = renderers[i].bounds.min.y < minY ? renderers[i].bounds.min.y : minY;
                minZ = renderers[i].bounds.min.z < minZ ? renderers[i].bounds.min.z : minZ;

                maxX = renderers[i].bounds.max.x > maxX ? renderers[i].bounds.max.x : maxX;
                maxY = renderers[i].bounds.max.y > maxY ? renderers[i].bounds.max.y : maxY;
                maxZ = renderers[i].bounds.max.z > maxZ ? renderers[i].bounds.max.z : maxZ;
            }

            Matrix4x4 objInv = transform.worldToLocalMatrix;
            boundMin = objInv.MultiplyPoint(new Vector3(minX, minY, minZ));
            boundMax = objInv.MultiplyPoint(new Vector3(maxX, maxY, maxZ));

            InitializeKeyPointsGrid();
        }

        private void Update()
        {
            DrawDebugLines();
        }

        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            Vector3 contextSourcePosition = (Vector3)ContextSourceTransformTarget;
            // Compute the TRS matrix of the optimization target
            Matrix4x4 TRS = Matrix4x4.TRS(optimizationTarget.Position, optimizationTarget.Rotation, transform.lossyScale);

            float cost = 0.0f;
            // Store a copy of the keys of the Dictionary
            List<Vector3> keypointsKeys = new List<Vector3>(keyPoints.Keys);
            // Iterate through all keypoints and check for occlusion.
            foreach (Vector3 keyPoint in keypointsKeys)
            {
                // TODO: consider transform in Layout
                Vector3 targetKeyPointPos = TRS.MultiplyPoint3x4(keyPoint);
                keyPoints[keyPoint] = CheckIfCornerIsOccluded(contextSourcePosition, targetKeyPointPos, Color.red);
                cost += keyPoints[keyPoint] ? 1 : 0;
            }

            prevCost = cost / keyPoints.Count;
            return prevCost;
        }

        public override Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout = null)
        {
            Layout result = optimizationTarget.Clone();
            // this only makes sense if the object is partially occluded
            Vector3 contextSourcePosition = (Vector3)ContextSourceTransformTarget;

            Vector3 positionChange = Vector3.zero;
            if (prevCost > 0 && Random.value < .5f)
            {
                // Transform contextSourceTransform = (Transform)ContextSourceTransformTarget;
                // Vector3 contextSourceToUI = transform.position - contextSourceTransform.position;

                // Iterate through all key value pairs of the keyPoints dictionary
                // Make the adaptive UI move in the opposite direction of the keypoints local position
                // Great idea, but I think using the scale of the distance hmd - ui is not good in this case
                
                Matrix4x4 trs = Matrix4x4.TRS(optimizationTarget.Position, optimizationTarget.Rotation, transform.lossyScale);
                
                var randomlyOrdered = keyPoints.OrderBy(g => Guid.NewGuid());
                foreach (var i in randomlyOrdered)
                {
                    if (i.Value)
                    {
                        RaycastHit hit;
                        Vector3 targetKeyPointPos = trs.MultiplyPoint3x4(i.Key);
                        Physics.Raycast(contextSourcePosition, i.Key, out hit, (contextSourcePosition - targetKeyPointPos).magnitude, occlusionMask);
                        result.Position = hit.point + hit.normal * HelperMath.SampleNormalDistribution(1f, 0.5f) * stepMovement;
                        break;
                    }
                }

            }
            else if (prevCost > 0 && Random.value < .8f) // move towards user 
            {
                Vector3 targetPosition = (Vector3)ContextSourceTransformTarget;
                Vector3 currentPosition = optimizationTarget.Position;

                Vector3 currentToTarget = (targetPosition - currentPosition).normalized;
                positionChange = currentToTarget * HelperMath.SampleNormalDistribution(1f, 0.5f) * stepMovement;
                
                result.Position += positionChange * stepMovement;
            }
            else
            {
                positionChange = new Vector3(
                    HelperMath.SampleNormalDistribution(1f, 0.5f) * 0.05f,
                    HelperMath.SampleNormalDistribution(1f, 0.5f) * 0.05f,
                    HelperMath.SampleNormalDistribution(1f, 0.5f) * 0.05f
                );
                
                result.Position += positionChange * stepMovement;
            }

            return result;

        }

        private void InitializeKeyPointsGrid()
        {
            Vector3 topRightPos = boundMax;
            Vector3 botLeftPos = boundMin;
            Vector3 topLeftPos = new Vector3(boundMin.x, boundMax.y, boundMin.z);
            Vector3 botRightPos = new Vector3(boundMax.x, boundMin.y, boundMax.z);

            keyPoints = new Dictionary<Vector3, bool>();
            for (int i = 0; i < keyPointSubdivisions + 2; i++)
            {
                float t1 = i / (1.0f + keyPointSubdivisions);
                Vector3 topPos = Vector3.Lerp(topLeftPos, topRightPos, t1);
                Vector3 bottomPos = Vector3.Lerp(botLeftPos, botRightPos, t1);

                for (int j = 0; j < keyPointSubdivisions + 2; j++)
                {
                    float t2 = j / (1.0f + keyPointSubdivisions);
                    Vector3 keyPoint = Vector3.Lerp(topPos, bottomPos, t2);
                    keyPoints.Add(keyPoint, false);
                }
            }
        }

        private bool CheckIfCornerIsOccluded(Vector3 origin, Vector3 endPoint, Color debugLineColor)
        {
            Vector3 originToEndPoint = endPoint - origin;
            bool isOccluded = Physics.Raycast(origin, originToEndPoint.normalized, originToEndPoint.magnitude * 1f, occlusionMask);
            return isOccluded;
        }

        private void DrawDebugLines()
        {
            if (ShowDebugLines == false)
                return;

            Vector3 contextSourcePosition = (Vector3)ContextSourceTransformTarget;

            foreach (KeyValuePair<Vector3, bool> keyValuePair in keyPoints)
            {
                Vector3 originToKeyPoint = transform.localToWorldMatrix.MultiplyPoint3x4(keyValuePair.Key);
                Debug.DrawLine(contextSourcePosition, originToKeyPoint.normalized * originToKeyPoint.magnitude * 1f, keyValuePair.Value ? Color.black : Color.red);
            }
        }

        public override Layout DirectRule(Layout optimizationTarget)
        {
            throw new NotImplementedException();
        }
    }
}
