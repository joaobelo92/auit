using System.Collections;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.AdaptationObjectives
{
    public class AvoidAdaptWhileMovingObjective : LocalObjective
    {
        private Vector3? lastPosition;
        
        [SerializeField]
        private float movementTolerance = 0.1f;

        private bool isMoving;
        
        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            return isMoving ? 1 : 0;
        }

        public override Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout = null)
        {
            return optimizationTarget;
        }

        public override Layout DirectRule(Layout optimizationTarget)
        {
            throw new System.NotImplementedException();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            StartCoroutine(CheckForMovement());
        }

        private IEnumerator CheckForMovement()
        {
            while (true)
            {
                Vector3 contextSourcePosition = ((Transform)ContextSourceTransformTarget).position;
                lastPosition ??= contextSourcePosition;
                isMoving = Vector3.Distance(lastPosition.Value, contextSourcePosition) > movementTolerance;
                lastPosition = contextSourcePosition;
                yield return new WaitForSeconds(0.2f);
            }
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
