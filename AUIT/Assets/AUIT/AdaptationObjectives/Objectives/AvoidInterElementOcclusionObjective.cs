using System;
using System.Collections.Generic;
using System.Linq;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.AdaptationObjectives.Objectives
{
    public class AvoidInterElementOcclusionObjective : MultiElementObjective
    {
        private Camera _occlusionObjectiveCamera;

        private MeshFilter _meshFilter;
        public Vector3 minBounds;
        public Vector3 maxBounds;

        public GameObject[] gameObjectsToAvoid;
        
        public void Reset()
        {
            ContextSource = ContextSource.PlayerPose;
        }
        
        protected override void Start()
        {
            _meshFilter = GetComponent<MeshFilter>();
            ComputeMeshBounds();

            GameObject cameraObject = GameObject.Find("OcclusionObjectiveCamera");
            if (cameraObject == null)
            {
                cameraObject = new GameObject("OcclusionObjectiveCamera");
                _occlusionObjectiveCamera = cameraObject.AddComponent<Camera>();
                _occlusionObjectiveCamera.fieldOfView = 120f;
                _occlusionObjectiveCamera.nearClipPlane = 0.01f;
                _occlusionObjectiveCamera.targetDisplay = 0;
            }
            else
            {
                _occlusionObjectiveCamera = cameraObject.GetComponent<Camera>();
            }
            base.Start();
            
            if(gameObjectsToAvoid.Length > 0) {
                CostFunction(new Layout[]
                {
                    new(ObjectiveHandler.Id, transform),
                    new(GameObject.Find("Cube (1)").GetComponent<LocalObjectiveHandler>().Id,
                        GameObject.Find("Cube (1)").transform)
                });}
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

        public override float CostFunction(Layout[] optimizationTarget, Layout initialLayout = null)
        {
            Layout thisLayout = optimizationTarget.First(l => l.Id == ObjectiveHandler.Id);
            Matrix4x4 trs = Matrix4x4.TRS(thisLayout.Position, thisLayout.Rotation, thisLayout.Scale);

            // multiply bounds by proposal's TRS (need to check 8x, for screen bounds)
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
            
            // here we will use the user's head position to determine occlusion
            var cameraTransform = _occlusionObjectiveCamera.transform;
            cameraTransform.position = (Vector3)ContextSourceTransformTarget;
            cameraTransform.rotation = Quaternion.LookRotation(thisLayout.Position - cameraTransform.position);

            List<Vector2> pointsScreenSpace = new ();
            foreach (Vector3 bound in bounds)
            {
                pointsScreenSpace.Add(_occlusionObjectiveCamera.WorldToScreenPoint(bound));
            }
            
            // convex hull is probably overkill as the min/max x/y would be sufficient for basic functionality
            // now its implemented so let's use it
            List<Vector2> hull = ComputeConvexHull(pointsScreenSpace);

            foreach (var point in hull)
            {
                Debug.Log(point);
            }

            int cost = 0;

            foreach (var go in gameObjectsToAvoid)
            {
                Layout goLayout = optimizationTarget.First(l => l.Id == go.GetComponent<AvoidInterElementOcclusionObjective>().ObjectiveHandler.Id);
                trs = Matrix4x4.TRS(goLayout.Position, goLayout.Rotation, goLayout.Scale);

                AvoidInterElementOcclusionObjective goObj = go.GetComponent<AvoidInterElementOcclusionObjective>();

                // multiply bounds by proposal's TRS (need to check 8x, for screen bounds)
                bounds = new []
                {
                    trs.MultiplyPoint3x4(new Vector3(goObj.minBounds.x, goObj.minBounds.y, goObj.minBounds.z)),
                    trs.MultiplyPoint3x4(new Vector3(goObj.maxBounds.x, goObj.minBounds.y, goObj.minBounds.z)),
                    trs.MultiplyPoint3x4(new Vector3(goObj.maxBounds.x, goObj.maxBounds.y, goObj.minBounds.z)),
                    trs.MultiplyPoint3x4(new Vector3(goObj.minBounds.x, goObj.minBounds.y, goObj.maxBounds.z)),
                    trs.MultiplyPoint3x4(new Vector3(goObj.minBounds.x, goObj.maxBounds.y, goObj.maxBounds.z)),
                    trs.MultiplyPoint3x4(new Vector3(goObj.minBounds.x, goObj.maxBounds.y, goObj.minBounds.z)),
                    trs.MultiplyPoint3x4(new Vector3(goObj.maxBounds.x, goObj.minBounds.y, goObj.maxBounds.z)),
                    trs.MultiplyPoint3x4(new Vector3(goObj.maxBounds.x, goObj.maxBounds.y, goObj.maxBounds.z))
                };
            
                List<Vector2> pointsScreenSpace2 = new ();
                foreach (Vector3 bound in bounds)
                {
                    Vector3 boundToScreenPoint = _occlusionObjectiveCamera.WorldToScreenPoint(bound);
                    if (boundToScreenPoint.z > 0)
                    {
                        pointsScreenSpace2.Add(_occlusionObjectiveCamera.WorldToScreenPoint(bound));
                    }
                }

                if (pointsScreenSpace2.Count >= 3)
                {
                    List<Vector2> hull2 = ComputeConvexHull(pointsScreenSpace2);
                    Debug.Log("obj2");
                    foreach (var point in hull2)
                    {
                        Debug.Log(point);
                    }


                    if (CheckPolygonOverlap(hull.ToArray(), hull2.ToArray()))
                    {
                        cost += 1;
                    }
                }
                
                
            }

            Debug.Log("Cost: " + cost);
            return cost / gameObjectsToAvoid.Length;
        }

        public override Layout OptimizationRule(Layout[] optimizationTarget, Layout initialLayout = null)
        {
            throw new System.NotImplementedException();
        }

        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            throw new NotImplementedException();
        }

        public override Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout = null)
        {
            throw new NotImplementedException();
        }

        public override Layout DirectRule(Layout optimizationTarget)
        {
            throw new System.NotImplementedException();
        }
        
        private List<Vector2> ComputeConvexHull(List<Vector2> points)
        {
            // Find the pivot point (lowest y-coordinate and leftmost if tie)
            Vector2 pivot = points[0];
            foreach (var point in points.Where(point => point.y < pivot.y || 
                                                        (Math.Abs(point.y - pivot.y) < 0.0001f && point.x < pivot.x)))
            {
                pivot = point;
            }

            // Sort the points based on polar angles with respect to the pivot
            points.Sort((a, b) =>
            {
                float angleA = Mathf.Atan2(a.y - pivot.y, a.x - pivot.x);
                float angleB = Mathf.Atan2(b.y - pivot.y, b.x - pivot.x);
                if (angleA < angleB) return -1;
                return angleA > angleB ? 1 : 0;
            });

            // Build the convex hull
            var convexHull = new List<Vector2>
            {
                points[0],
                points[1]
            };

            for (int i = 2; i < points.Count; i++)
            {
                while (convexHull.Count > 1 &&
                       Vector2.SignedAngle(convexHull[^1] - convexHull[^2], points[i] - convexHull[^2]) <= 0)
                {
                    convexHull.RemoveAt(convexHull.Count - 1);
                }
                convexHull.Add(points[i]);
            }

            return convexHull;
        }
        
        private bool CheckPolygonOverlap(Vector2[] polygon1, Vector2[] polygon2)
        {

            for (int i = 0; i < polygon1.Length; i++)
            {
                Vector2 edge = polygon1[(i + 1) % polygon1.Length] - polygon1[i];
                Vector2 axis = new Vector2(-edge.y, edge.x).normalized;

                if (!OverlapOnAxis(axis, polygon1, polygon2))
                {
                    return false;
                }
            }

            for (int i = 0; i < polygon2.Length; i++)
            {
                Vector2 edge = polygon2[(i + 1) % polygon2.Length] - polygon2[i];
                Vector2 axis = new Vector2(-edge.y, edge.x).normalized;

                if (!OverlapOnAxis(axis, polygon1, polygon2))
                {
                    return false;
                }
            }

            return true;
        }

        private bool OverlapOnAxis(Vector2 axis, Vector2[] vertices1, Vector2[] vertices2)
        {
            float min1 = float.MaxValue, max1 = float.MinValue;
            float min2 = float.MaxValue, max2 = float.MinValue;

            foreach (Vector2 vertex in vertices1)
            {
                float projection = Vector2.Dot(axis, vertex);
                min1 = Mathf.Min(min1, projection);
                max1 = Mathf.Max(max1, projection);
            }

            foreach (Vector2 vertex in vertices2)
            {
                float projection = Vector2.Dot(axis, vertex);
                min2 = Mathf.Min(min2, projection);
                max2 = Mathf.Max(max2, projection);
            }

            return !(max1 < min2 || max2 < min1);
        }
    }
}