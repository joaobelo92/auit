using AUIT.AdaptationObjectives.Definitions;
using AUIT.AdaptationObjectives.Extras;
using System.Linq;
using UnityEngine;

namespace AUIT.AdaptationObjectives
{
    public class PhysicalIntersectionObjective : LocalObjective
    {
        [SerializeField]
        private LayerMask physicalLayerMask;

        private Collider layoutCollider;

        private bool IsIntersecting(Layout optimizationTarget)
        {
            Bounds bounds = layoutCollider.bounds;
            return Physics.CheckBox(optimizationTarget.Position, bounds.extents, optimizationTarget.Rotation, physicalLayerMask);
        }

        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            if (IsIntersecting(optimizationTarget))
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
            Layout result = optimizationTarget.Clone();

            Bounds bounds = layoutCollider.bounds;
            Collider[] overlapping = Physics.OverlapBox(optimizationTarget.Position, bounds.extents, optimizationTarget.Rotation, physicalLayerMask);

            Vector3 moveDirection = Random.insideUnitSphere;
            moveDirection *= Random.Range(0, 0.3f);

            foreach (Collider overlap in overlapping)
            {
                moveDirection += (optimizationTarget.Position - overlap.transform.position).normalized;
            }

            result.Position += 0.05f * HelperMath.SampleNormalDistribution(1f, 0.5f) * moveDirection;

            return result; 
        }

        public override Layout DirectRule(Layout optimizationTarget)
        {
            throw new System.NotImplementedException();
        }

        protected override void Start()
        {
            layoutCollider = GetComponent<Collider>();

            if (layoutCollider == null)
            {
                Debug.LogError("PhysicalIntersectionObjective.Start(): No collider found in children.");
            }
        }

        private new void OnEnable()
        {
            base.OnEnable();

            physicalLayerMask = LayerMask.GetMask("Physical Environment");
        }
    }

}