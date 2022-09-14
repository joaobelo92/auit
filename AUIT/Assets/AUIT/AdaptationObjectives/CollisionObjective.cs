using AUIT.AdaptationObjectives.Definitions;
using AUIT.AdaptationObjectives.Extras;
using UnityEngine;

namespace AUIT.AdaptationObjectives
{
    public class CollisionObjective : LocalObjective
    {
        [SerializeField]
        private float collisonSphereSize = 1.0f;
        [SerializeField]
        private float moveAwayDistance = 1.0f;

        private Vector3? GetDistanceVector(Vector3 currentPosition)
        {
            // Can the contextSource somehow be a list of overlapping colliders?
            // For now, I'm ignoring contextSource...

            // TODO: overlapping collider may be collider attached to this game object...
            Collider[] overlappingColliders = Physics.OverlapSphere(currentPosition, collisonSphereSize);
            if (overlappingColliders.Length == 0)
                return null;

            // TODO: add implementation for multiple colliders...
            Vector3 closestPoint = overlappingColliders[0].ClosestPoint(currentPosition);
            return closestPoint - currentPosition;
        }

        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            Vector3? distanceVector = GetDistanceVector(optimizationTarget.Position);
            if (distanceVector == null) // No overlapping colliders where found.
                return 0.0f;

            float distance = Vector3.Magnitude(distanceVector ?? Vector3.one);
            float cost = Mathf.Max(0.0f, -1 * Mathf.Log(distance));
            return cost;
        }

        public override Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout)
        {
            Vector3 distanceVector = GetDistanceVector(optimizationTarget.Position) ?? Vector3.zero;
            Layout result = optimizationTarget.Clone();
            result.Position += distanceVector * (HelperMath.SampleNormalDistribution(1.0f, 0.5f) * -1 * moveAwayDistance);
            return result;
        }

        public override Layout DirectRule(Layout optimizationTarget)
        {
            throw new System.NotImplementedException();
        }
    }
}