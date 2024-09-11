using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.AdaptationObjectives
{
    public class SurfaceMagnetismObjective : LocalObjective
    {
        [SerializeField, Tooltip("Layer Mask should contain all surfaces")]
        private LayerMask layerMask = Physics.DefaultRaycastLayers;
        // [SerializeField, Tooltip("The goal distance from the surface of the target.")]
        // private float goalSurfaceDistance = 0.03f;
        
        // [SerializeField, Tooltip("The goal distance from the surface of the target.")]
        // private float distanceTolerance = 0.3f;

        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            Vector3 targetPosition = ((Transform)ContextSourceTransformTarget).position;
            Vector3 optimizationTargetPosition = optimizationTarget.Position;
            
            RaycastHit hit;
            Physics.Raycast(targetPosition, optimizationTarget.Position - targetPosition, out hit, Mathf.Infinity, layerMask);
            float distanceFromSurface = Mathf.Abs(Vector3.Distance(targetPosition, hit.point) -
                             Vector3.Distance(targetPosition, optimizationTargetPosition));
            
            Matrix4x4 trs = Matrix4x4.TRS(optimizationTarget.Position, optimizationTarget.Rotation, transform.lossyScale);
            float angleDifference = Vector3.Angle(hit.normal, -new Vector3(trs.m20, trs.m21, trs.m22));
            
            print($"distance from surface: {distanceFromSurface}; angle difference: {angleDifference}");

            return distanceFromSurface + angleDifference;
        }

        public override Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout)
        {
            Layout result = optimizationTarget.Clone();
            return result;
        }

        public override Layout DirectRule(Layout optimizationTarget)
        {
            throw new System.NotImplementedException();
        }
    }
}