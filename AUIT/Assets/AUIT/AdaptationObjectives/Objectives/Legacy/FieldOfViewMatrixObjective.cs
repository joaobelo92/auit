using System;
using System.Collections;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.AdaptationObjectives.Extras;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AUIT.AdaptationObjectives.Objectives
{
    public class FieldOfViewMatrixObjective : LocalObjective
    {
        private MeshFilter _meshFilter;
        private Camera _userCamera;
        
        public Vector3 minBounds;
        public Vector3 maxBounds;
        public float viewportPercentageX = 1f;
        public float viewportPercentageY = 1f;

        public bool topLeft = true;
        public bool topCenter = true;
        public bool topRight = true;
        public bool centerLeft = true;
        public bool centerCenter = true;
        public bool centerRight = true;
        public bool bottomLeft = true;
        public bool bottomCenter = true;
        public bool bottomRight = true;

        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            Matrix4x4 trs = Matrix4x4.TRS(optimizationTarget.Position, optimizationTarget.Rotation, 
                optimizationTarget.Scale);

            bool[][] fovGoal = 
            {
                new[] { bottomLeft, bottomCenter, bottomRight },
                new[] { centerLeft, centerCenter, centerRight },
                new[] { topLeft, topCenter, topRight },
            };

            Vector3[] bounds =
            {
                trs.MultiplyPoint3x4(new Vector3(minBounds.x, minBounds.y, minBounds.z)),
                trs.MultiplyPoint3x4(new Vector3(maxBounds.x, minBounds.y, minBounds.z)),
                trs.MultiplyPoint3x4(new Vector3(maxBounds.x, maxBounds.y, minBounds.z)),
                trs.MultiplyPoint3x4(new Vector3(minBounds.x, minBounds.y, maxBounds.z)),
                trs.MultiplyPoint3x4(new Vector3(minBounds.x, maxBounds.y, maxBounds.z)),
                trs.MultiplyPoint3x4(new Vector3(minBounds.x, maxBounds.y, minBounds.z)),
                trs.MultiplyPoint3x4(new Vector3(maxBounds.x, minBounds.y, maxBounds.z)),
                trs.MultiplyPoint3x4(new Vector3(maxBounds.x, maxBounds.y, maxBounds.z))
            };

            float cost = 0f;
            float borderX = _userCamera.pixelWidth * (1 - viewportPercentageX) / 2;
            float borderY = _userCamera.pixelHeight * (1 - viewportPercentageY) / 2;

            foreach (Vector3 bound in bounds)
            {
                Vector3 screenPoint = _userCamera.WorldToScreenPoint(bound);
                if (screenPoint.z < 0)
                {
                    cost += 1;
                    continue;
                }

                if (screenPoint.x < borderX || screenPoint.x > _userCamera.pixelWidth - borderX ||
                    screenPoint.y < borderY || screenPoint.y > _userCamera.pixelHeight - borderY)
                {
                    cost += 1;
                    continue;
                }

                int viewportWidth = (int)((_userCamera.pixelWidth - borderX * 2) / 3);
                int xIndex = (int)(screenPoint.x - borderX) / viewportWidth;
                int viewportHeight = (int)((_userCamera.pixelHeight - borderY * 2) / 3);
                int yIndex = (int)(screenPoint.y - borderY) / viewportHeight;
                if (!fovGoal[yIndex][xIndex])
                {
                    cost += 1;
                }
            }

            return cost / 8;
        }

        public override Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout = null)
        {
            Layout result = optimizationTarget.Clone();
            
            result.Position += Random.insideUnitSphere * (HelperMath.SampleNormalDistribution(0.5f, 0.5f) * 0.01f);
            return result;
        }

        public override Layout DirectRule(Layout optimizationTarget)
        {
            throw new System.NotImplementedException();
        }

        protected override void Start()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _userCamera = Camera.main;
            ComputeMeshBounds();
            base.Start();
            InvokeRepeating(nameof(TestCost), 1f, 5f);
        }

        private void TestCost()
        {
            Debug.Log(CostFunction(new Layout("0", transform)));
        }

        private void ComputeMeshBounds()
        {
            if (_meshFilter == null || _meshFilter.sharedMesh == null)
            {
                Debug.LogError($"Mesh or MeshFilter is missing in {transform.name} with an " +
                               "avoid inter element occlusion objective");
                return;
            }

            Mesh mesh = _meshFilter.sharedMesh;
            Vector3[] vertices = mesh.vertices;

            minBounds = vertices[0];
            maxBounds = vertices[0];

            for (int i = 1; i < vertices.Length; i++)
            {
                minBounds = Vector3.Min(minBounds, vertices[i]);
                maxBounds = Vector3.Max(maxBounds, vertices[i]);
            }
        }

    }
}
