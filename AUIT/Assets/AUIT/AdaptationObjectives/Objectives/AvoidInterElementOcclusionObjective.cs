using System;
using System.Collections.Generic;
using System.Linq;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.AdaptationObjectives.Extras;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AUIT.AdaptationObjectives.Objectives
{
    public class AvoidInterElementOcclusionObjective : MultiElementObjective
    {
        [SerializeField]
        private ContextSource<Camera> userContextSource;

        private List<(Vector3, Vector3)> _bounds = new List<(Vector3, Vector3)>();
        private bool _boundsInitialized = false;
        private List<int> elementsColliding = new List<int>();
        
        private void InitializeMeshBounds(Layout[] layouts)
        {
            foreach (var layout in layouts)
            {
                GameObject go = auit.gameObjectsToOptimize
                    .First(l => layout.Id == l.GetComponent<LocalObjectiveHandler>().Id);
            
                MeshFilter[] meshFilter = go.GetComponentsInChildren<MeshFilter>();
                if (meshFilter.Length == 0 || !meshFilter[0].mesh)
                {
                    Debug.LogError($"Mesh or MeshFilter is missing in {go.name}" +
                                   $"This is required for the AvoidInterElementOcclusionObjective component.");
                    return;
                }

                if (meshFilter.Length != 1)
                {
                    Debug.LogWarning($"Multiple Mesh or MeshFilter in {go.name}. Picking first found.");
                }

                Mesh mesh = meshFilter[0].mesh;
                Vector3[] vertices = mesh.vertices;

                float minX = vertices[0].x, minY = vertices[0].y, minZ = vertices[0].z;
                float maxX = vertices[0].x, maxY = vertices[0].y, maxZ = vertices[0].z;

                for (int i = 1; i < vertices.Length; i++)
                {
                    Vector3 v = vertices[i];

                    if (v.x < minX) minX = v.x;
                    if (v.y < minY) minY = v.y;
                    if (v.z < minZ) minZ = v.z;

                    if (v.x > maxX) maxX = v.x;
                    if (v.y > maxY) maxY = v.y;
                    if (v.z > maxZ) maxZ = v.z;
                }
                
                _bounds.Add((new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ)));
            }
        }


        public override float CostFunction(Layout[] optimizationTarget, Layout initialLayout = null)
        {
            float cost = 0f;
            elementsColliding = new List<int>();
            List<List<Vector2>> polygonsScreenSpace = new ();
            
            if (!_boundsInitialized)
            {
                InitializeMeshBounds(optimizationTarget);
                _boundsInitialized = true;
            }

            for (int i = 0; i < optimizationTarget.Length; i++)
            {
                Layout layout = optimizationTarget[i];
                
                // GameObject go = auit.gameObjectsToOptimize
                //     .First(l => layout.Id == l.GetComponent<LocalObjectiveHandler>().Id);
                Matrix4x4 trs = Matrix4x4.TRS(layout.Position, layout.Rotation, layout.Scale);
                // Debug.Log($"Layout: {layout.Position} {layout.Rotation.eulerAngles} {layout.Scale}");

                // multiply bounds by proposal's TRS (need to check 8x, for screen bounds)
                Vector3[] bounds =
                {
                    trs.MultiplyPoint3x4(new Vector3(_bounds[i].Item1.x, _bounds[i].Item1.y, _bounds[i].Item1.z)),
                    trs.MultiplyPoint3x4(new Vector3(_bounds[i].Item2.x, _bounds[i].Item1.y, _bounds[i].Item1.z)),
                    trs.MultiplyPoint3x4(new Vector3(_bounds[i].Item2.x, _bounds[i].Item2.y, _bounds[i].Item1.z)),
                    trs.MultiplyPoint3x4(new Vector3(_bounds[i].Item1.x, _bounds[i].Item1.y, _bounds[i].Item2.z)),
                    trs.MultiplyPoint3x4(new Vector3(_bounds[i].Item1.x, _bounds[i].Item2.y, _bounds[i].Item2.z)),
                    trs.MultiplyPoint3x4(new Vector3(_bounds[i].Item1.x, _bounds[i].Item2.y, _bounds[i].Item1.z)),
                    trs.MultiplyPoint3x4(new Vector3(_bounds[i].Item2.x, _bounds[i].Item1.y, _bounds[i].Item2.z)),
                    trs.MultiplyPoint3x4(new Vector3(_bounds[i].Item2.x, _bounds[i].Item2.y, _bounds[i].Item2.z))
                };
            
                List<Vector2> pointsScreenSpace = new ();
                foreach (Vector3 bound in bounds)
                {
                    Vector3 boundToScreenPoint = userContextSource.GetValue().WorldToScreenPoint(bound);
                    if (boundToScreenPoint.z > 0)
                    {
                        pointsScreenSpace.Add(new Vector2(boundToScreenPoint.x, boundToScreenPoint.y));
                    }
                }
                
                if (pointsScreenSpace.Count >= 3)
                {
                    
                    pointsScreenSpace = ComputeConvexHull(pointsScreenSpace);
                    if (pointsScreenSpace.Count >= 3)
                        polygonsScreenSpace.Add(pointsScreenSpace);
                }
                
                
            }

            for (int i = 0; i < polygonsScreenSpace.Count; i++)
            {
                for (int j = i + 1; j < polygonsScreenSpace.Count; j++)
                {
                    // Debug.Log($"checking {i} {j} {PolygonsOverlap(polygonsScreenSpace[i], polygonsScreenSpace[j])}");
                    if (PolygonsOverlap(polygonsScreenSpace[i], polygonsScreenSpace[j]))
                    {
                        elementsColliding.Add(i);
                        elementsColliding.Add(j);
                        cost += 1;
                    }
                }
            }

            float combinations = auit.gameObjectsToOptimize.Count * (auit.gameObjectsToOptimize.Count - 1) / 2;
            Debug.Log("Cost: " + cost / combinations);
            return cost / combinations;
        }

        public override List<Layout> OptimizationRule(List<Layout> optimizationTarget, Layout initialLayout = null)
        {
            Vector3 position = optimizationTarget[elementsColliding.Last()].Position;
            optimizationTarget[elementsColliding.Last()].Position = position + Random.insideUnitSphere * 
                HelperMath.SampleNormalDistribution(0.5f, 0.5f) * 0.05f;
            return optimizationTarget;
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
        
        private static bool PolygonsOverlap(List<Vector2> poly1, List<Vector2> poly2)
        {
            return !HasSeparatingAxis(poly1, poly2) && !HasSeparatingAxis(poly2, poly1);
        }

        private static bool HasSeparatingAxis(List<Vector2> poly1, List<Vector2> poly2)
        {
            for (int i = 0; i < poly1.Count; i++)
            {
                Vector2 p1 = poly1[i];
                Vector2 p2 = poly1[(i + 1) % poly1.Count];

                Vector2 edge = p2 - p1;
                Vector2 axis = new Vector2(-edge.y, edge.x).normalized;

                // Project both polygons onto the axis
                ProjectPolygon(axis, poly1, out float min1, out float max1);
                ProjectPolygon(axis, poly2, out float min2, out float max2);

                // Check for overlap
                if (max1 < min2 || max2 < min1)
                    return true; // Found a separating axis
            }

            return false; // No separating axis found
        }

        private static void ProjectPolygon(Vector2 axis, List<Vector2> polygon, out float min, out float max)
        {
            float dot = Vector2.Dot(axis, polygon[0]);
            min = max = dot;

            for (int i = 1; i < polygon.Count; i++)
            {
                dot = Vector2.Dot(axis, polygon[i]);
                if (dot < min) min = dot;
                if (dot > max) max = dot;
            }
        }
    }
}