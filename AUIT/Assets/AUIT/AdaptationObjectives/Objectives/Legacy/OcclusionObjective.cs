using System;
using System.Collections.Generic;
using System.Linq;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.AdaptationObjectives.Extras;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace AUIT.AdaptationObjectives.Objectives
{
    public class OcclusionObjective : LocalObjective
    {

        [Header("Options")]
        [SerializeField]
        private ContextSource<Transform> userContextSource;

        [SerializeField]
        private LayerMask occlusionMask = Physics.DefaultRaycastLayers;
        [SerializeField]
        private int keyPointSubdivisions = 3;

        public Collider occlusionCollider;

        private List<Transform> keyPointTransforms = new List<Transform>();

        private bool[,] keyPointsOccluded;

        [Header("Debugging")]
        public bool showDebugLines;

        // private float _minX = float.PositiveInfinity;
        // private float _minY = float.PositiveInfinity;
        // private float _minZ = float.PositiveInfinity;
        // private float _maxX = float.NegativeInfinity;
        // private float _maxY = float.NegativeInfinity;
        // private float _maxZ = float.NegativeInfinity;

        private Vector3 _boundMin;
        private Vector3 _boundMax;

        private float _prevCost = 1f;

        private void Reset()
        {
        }

        protected override void Start()
        {
            base.Start();
            objectiveType = ObjectiveType.AvoidOcclusion;
            occlusionMask &= ~(1 << gameObject.layer);

            if (occlusionCollider != null)
            {
                Bounds bounds = occlusionCollider.bounds;

                _boundMin = bounds.min;
                _boundMax = bounds.max;
            }
            else
            {
                // Currently not working!!
                Debug.LogError("OcclusionObjective: No occlusion collider set, object's renderers are not implemented atm. Set an occlusion collider to avoid issues.");
                // Renderer[] renderers = GetComponentsInChildren<Renderer>();
                // if (GetComponent<Renderer>() != null)
                // {
                //     renderers.Append(GetComponent<Renderer>());
                // }

                // for (int i = 0; i < renderers.Length; i++)
                // {
                //     _minX = renderers[i].bounds.min.x < _minX ? renderers[i].bounds.min.x : _minX;
                //     _minY = renderers[i].bounds.min.y < _minY ? renderers[i].bounds.min.y : _minY;
                //     _minZ = renderers[i].bounds.min.z < _minZ ? renderers[i].bounds.min.z : _minZ;

                //     _maxX = renderers[i].bounds.max.x > _maxX ? renderers[i].bounds.max.x : _maxX;
                //     _maxY = renderers[i].bounds.max.y > _maxY ? renderers[i].bounds.max.y : _maxY;
                //     _maxZ = renderers[i].bounds.max.z > _maxZ ? renderers[i].bounds.max.z : _maxZ;
                // }

                // _boundMin = new Vector3(_minX, _minY, _minZ);
                // _boundMax = new Vector3(_maxX, _maxY, _maxZ);

            }


            InitializeKeyPointsGrid();
            keyPointsOccluded = new bool[keyPointSubdivisions + 2, keyPointSubdivisions + 2];
        }

        private void Update()
        {
            if (showDebugLines)
                DrawDebugLines();
        }

        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            Transform contextSourceTransform = userContextSource.GetValue();

            float cost = 0.0f;
            // Store a copy of the keys of the Dictionary
            // List<Vector3> keypointsKeys = new List<Vector3>(keyPoints.Keys);
            // Iterate through all keypoints and check for occlusion.
            for (int i = 0; i < keyPointSubdivisions + 2; i++)
            {

                for (int j = 0; j < keyPointSubdivisions + 2; j++)
                {
                    Vector3 targetKeyPointPos = keyPointTransforms[i * (keyPointSubdivisions + 2) + j].position;
                    bool isOccluded = CheckIfCornerIsOccluded(contextSourceTransform.position, targetKeyPointPos);
                    cost += isOccluded ? 1 : 0;
                    keyPointsOccluded[i, j] = isOccluded;
                }
            }

            
            _prevCost = cost / keyPointTransforms.Count;
            return _prevCost;
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
                // Use the occluded points array to determine move direction
                Vector3 center = Vector3.zero;
                int count = 0;
                // Calculate the center of all key points
                for (int i = 0; i < keyPointsOccluded.GetLength(0); i++)
                {
                    for (int j = 0; j < keyPointsOccluded.GetLength(1); j++)
                    {
                        center += keyPointTransforms[i * keyPointsOccluded.GetLength(1) + j].position;
                        count++;
                    }
                }
                center /= count;

                // Move away from occluded points
                for (int i = 0; i < keyPointsOccluded.GetLength(0); i++)
                {
                    for (int j = 0; j < keyPointsOccluded.GetLength(1); j++)
                    {
                        if (!keyPointsOccluded[i, j])
                        {
                            Vector3 occludedPoint = keyPointTransforms[i * keyPointsOccluded.GetLength(1) + j].position;
                            moveDirection += (occludedPoint - center).normalized;
                        }
                    }
                }
                if (moveDirection != Vector3.zero)
                    moveDirection.Normalize();
            }
            else if (moveStrategy < 0.66)
            {
                moveDirection = GetPlanarDirection(optimizationTarget);
            }
            else
            {
                moveDirection = Random.onUnitSphere;
            }

            result.Position += Random.Range(0f, 0.1f) * moveDirection;

            return result; 

        }

        // Go in random direction in perpendicular plane to the user
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

        // Vector3 topRightPos = _boundMax;
        // Vector3 botLeftPos = _boundMin;
        // Vector3 topLeftPos = new Vector3(_boundMin.x, _boundMax.y, _boundMin.z);
        // Vector3 botRightPos = new Vector3(_boundMax.x, _boundMin.y, _boundMax.z);

        // keyPoints = new Dictionary<Vector3, bool>();
        // for (int i = 0; i < keyPointSubdivisions + 2; i++)
        // {
        //     float t1 = i / (1.0f + keyPointSubdivisions);
        //     Vector3 topPos = Vector3.Lerp(topLeftPos, topRightPos, t1);
        //     Vector3 bottomPos = Vector3.Lerp(botLeftPos, botRightPos, t1);

        //     for (int j = 0; j < keyPointSubdivisions + 2; j++)
        //     {
        //         float t2 = j / (1.0f + keyPointSubdivisions);
        //         Vector3 keyPoint = Vector3.Lerp(topPos, bottomPos, t2);
        //         keyPoints.Add(keyPoint, false);
        //     }
        // }

        private void InitializeKeyPointsGrid()
        {

            Vector3 topRightPos = _boundMax;
            Vector3 botLeftPos = _boundMin;
            Vector3 topLeftPos = new Vector3(_boundMin.x, _boundMax.y, _boundMax.z);
            Vector3 botRightPos = new Vector3(_boundMax.x, _boundMin.y, _boundMax.z);

            topRightPos.z = _boundMax.z;

            keyPointTransforms = new List<Transform>();

            for (int i = 0; i < keyPointSubdivisions + 2; i++)
            {
                float t1 = i / (1.0f + keyPointSubdivisions);
                Vector3 topPos = Vector3.Lerp(topLeftPos, topRightPos, t1);
                Vector3 bottomPos = Vector3.Lerp(botLeftPos, botRightPos, t1);

                for (int j = 0; j < keyPointSubdivisions + 2; j++)
                {
                    float t2 = j / (1.0f + keyPointSubdivisions);
                    Vector3 keyPoint = Vector3.Lerp(topPos, bottomPos, t2);

                    // Create phantom object
                    GameObject phantom = new GameObject($"KeyPoint_{i}_{j}");
                    phantom.transform.SetParent(transform, false);
                    phantom.transform.position = keyPoint;

                    keyPointTransforms.Add(phantom.transform);
                }
            }
        }

        private bool CheckIfCornerIsOccluded(Vector3 origin, Vector3 endPoint)
        {
            Vector3 originToEndPoint = endPoint - origin;
            bool isOccluded = Physics.Raycast(origin, originToEndPoint.normalized, out RaycastHit hit, originToEndPoint.magnitude * 1f, occlusionMask);
            // if (isOccluded)
            // {
            //     print($"Occluded! {hit.collider.transform.name}");
            // }
            return isOccluded;
        }

        private void DrawDebugLines()
        {
            Transform contextSourceTransform = userContextSource.GetValue();

            for (int i = 0; i < keyPointSubdivisions + 2; i++)
            {

                for (int j = 0; j < keyPointSubdivisions + 2; j++)
                {
                    Vector3 targetKeyPointPos = keyPointTransforms[i * (keyPointSubdivisions + 2) + j].position;
                    Debug.DrawLine(contextSourceTransform.position, targetKeyPointPos, Color.red);
                }
            }
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
