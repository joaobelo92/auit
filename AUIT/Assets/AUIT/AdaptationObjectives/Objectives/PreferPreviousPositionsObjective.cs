using AUIT.AdaptationObjectives.Definitions;
using AUIT.AdaptationTriggers;
using AUIT.Extras;
using AUIT.PropertyTransitions;
using Oculus.Interaction;
using UnityEngine;


namespace AUIT.AdaptationObjectives.Objectives
{

    public class PreviousPosition
    {
        public Vector3 localPosition;

        public Transform contextSource;

        public PreviousPosition(Vector3 worldPosition, ContextSource<Transform> cs)
        {
            contextSource = cs.GetValue();
            localPosition = contextSource.InverseTransformPoint(worldPosition);
        }

        public Vector3 GetWorldPosition()
        {
            return contextSource.TransformPoint(localPosition);
        }
    }

    public class PreferPreviousPositionsObjective : LocalObjective
    {
        [SerializeField]
        private GrabbableContextSource grabbableEventContextSource;

        [SerializeField]
        private AdaptationTriggerContextSource adaptationEventContextSource;

        [SerializeField]
        private ContextSource<Transform> userHeadContextSource;

        [SerializeField]
        private ContextSource<Transform> userTorsoContextSource;

        
        [SerializeField]
        private CoordinateSystemContextSource coordinateSystemTransitionContextSource;

        [SerializeField]
        private float adaptationDefinedPenalty = 0.25f;

        
        [SerializeField]
        private float maxCostThreshold = 0.25f; 

        private PreviousPosition previousPositionUserDefined;

        private PreviousPosition previousPositionAdaptationDefined;



        public override ObjectiveType ObjectiveType => ObjectiveType.PreferPreviousPositions;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (grabbableEventContextSource == null || grabbableEventContextSource.GetValue() == null || adaptationEventContextSource == null || adaptationEventContextSource.GetValue() == null)
            {
                Debug.LogError("GrabbableContextSource or AdaptationEventContextSource is not assigned.");
                return;
            }

            grabbableEventContextSource.GetValue().WhenPointerEventRaised += OnPointerEvent;
            adaptationEventContextSource.GetValue().WhenAdaptationTriggered += onAdaptationTriggered;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            grabbableEventContextSource.GetValue().WhenPointerEventRaised -= OnPointerEvent;
            adaptationEventContextSource.GetValue().WhenAdaptationTriggered -= onAdaptationTriggered;
        }

        private void OnPointerEvent(PointerEvent evt)
        {
            // Whenever the user manually selects a new position, save it and prioritize it for future adaptations
            if (evt.Type == PointerEventType.Unselect)
            {
                bool isHeadBased = coordinateSystemTransitionContextSource.GetValue().CurrentCoordinateSystem == CoordinateSystem.Head;
                previousPositionUserDefined = new PreviousPosition(
                    transform.position,
                    isHeadBased ? userHeadContextSource : userTorsoContextSource
                );
                // print("User defined new position for " + gameObject.name + " at " + previousPositionUserDefined.localPosition + " in " + (isHeadBased ? "head" : "torso") + " coordinate system.");
                // print("Costs for this pos: " + FindFirstObjectByType<AUIT>().ComputeCost(verbose: true));
            }
        }

        private void onAdaptationTriggered(UIConfiguration[] layouts)
        {
            Layout[] layoutArray = layouts[0].elements;
            for (int i = 0; i < layoutArray.Length; i++)
            {
                if (layoutArray[i].Id == GetComponent<LocalObjectiveHandler>().Id)
                {
                    previousPositionAdaptationDefined = new PreviousPosition(
                        transform.position,
                        coordinateSystemTransitionContextSource.GetValue().CurrentCoordinateSystem == CoordinateSystem.Head ? userHeadContextSource : userTorsoContextSource
                    );
                    print("Adaptation defined new position for at " + previousPositionAdaptationDefined.localPosition + " in " + (coordinateSystemTransitionContextSource.GetValue().CurrentCoordinateSystem == CoordinateSystem.Head ? "head" : "torso") + " coordinate system.");
                }
            }
        }


        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            if (previousPositionUserDefined == null && previousPositionAdaptationDefined == null)
            {
                return 0f; // No previous position to compare to
            }

            float costUserDefined = 1f;

            if (previousPositionUserDefined != null)
            {
                Vector3 positionInLocalCoords = previousPositionUserDefined.contextSource.InverseTransformPoint(optimizationTarget.Position);
                costUserDefined = Vector3.Distance(previousPositionUserDefined.localPosition, positionInLocalCoords);

                costUserDefined = Mathf.Clamp01(costUserDefined / maxCostThreshold);
            }

            float costAdaptationDefined = 1f;

            if (previousPositionAdaptationDefined != null)
            {
                Vector3 positionInLocalCoords = previousPositionAdaptationDefined.contextSource.InverseTransformPoint(optimizationTarget.Position);
                costAdaptationDefined = Vector3.Distance(previousPositionAdaptationDefined.localPosition, positionInLocalCoords);

                costAdaptationDefined += adaptationDefinedPenalty * maxCostThreshold;
                costAdaptationDefined = Mathf.Clamp01(costAdaptationDefined / maxCostThreshold);
            }

            return costAdaptationDefined > costUserDefined ? costUserDefined : costAdaptationDefined;
        }

        public override Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout)
        {
            Layout result = optimizationTarget.Clone();

            float moveStrategy = Random.value;
            if (moveStrategy < 0.3f && previousPositionUserDefined != null)
            {
                result.Position = previousPositionUserDefined.GetWorldPosition();
            }
            else if (moveStrategy < 0.5f && previousPositionAdaptationDefined != null)
            {
                result.Position = previousPositionAdaptationDefined.GetWorldPosition();
            } else
            {
                result.Position += Random.Range(0.01f, 0.1f) * Random.onUnitSphere;;
            }
            return result;
        }

        public override Layout DirectRule(Layout optimizationTarget)
        {
            // Do nothing for now.
            return optimizationTarget;
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