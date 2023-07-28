using AUIT.AdaptationObjectives.Definitions;
using AUIT.AdaptationObjectives.Extras;
using UnityEngine;
using Random = UnityEngine.Random;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AUIT.AdaptationObjectives
{
    /// <summary>
    /// This class is used to store the association between a game object and its semantic score.
    /// </summary>
    [Serializable]
    public class GameObjectAssociation
    {
        public GameObject gameObject;
        public float positiveScore;
        public float negativeScore;

        public GameObjectAssociation(GameObject gameObject, float positiveScore, float negativeScore)
        {
            this.gameObject = gameObject;
            this.positiveScore = positiveScore;
            this.negativeScore = negativeScore;
        }
    }

    /// <summary>
    /// This objective is used to calculate the semantic cost of an element (i.e., a cost that is based on the distance
    /// between the closest semantically related object in the user's environment
    /// and the element as weighted by the association score).
    /// </summary>
    public class SemanticObjective : LocalObjective
    {
        [SerializeField]
        private float positiveAssociationWeight = 0.375f;
        [SerializeField]
        private float negativeAssociationWeight = 0.625f;
        [SerializeField]
        private List<GameObjectAssociation> associations = new List<GameObjectAssociation>();

        public void Reset()
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
        }

        private float GetSemanticAgreement(float[] positiveAssociations, float[] negativeAssociations, float[] distances, float positiveWeight, float negativeWeight)
        {
            // Replace zeros with a tiny value to avoid division by zero
            float smallFloat = 0.001f; // If distance is zero, add 1mm to avoid division by zero
            float[] squaredDistancesWithoutZero = distances.Select(d => d == 0 ? smallFloat : d * d).ToArray();
            float maxPositiveAssociationDistance = (positiveAssociations.Zip(squaredDistancesWithoutZero, (a, b) => a / b)).Max();
            float maxNegativeAssociationDistance = (negativeAssociations.Zip(squaredDistancesWithoutZero, (a, b) => a / b)).Max();
            return positiveWeight * maxPositiveAssociationDistance - negativeWeight * maxNegativeAssociationDistance;
        }

        private float GetSemanticMismatch(float[] positiveAssociations, float[] negativeAssociations, float[] distances, float positiveWeight, float negativeWeight)
        {
            float semanticAgreement = GetSemanticAgreement(positiveAssociations, negativeAssociations, distances, positiveWeight, negativeWeight);
            return 2 / (1 + Mathf.Exp(Mathf.Clamp(semanticAgreement, -1e2f, 1e2f)));
        }

        /// <summary>
        /// Returns the semantic cost of an element (i.e., a cost that is based on the distance
        /// between the closest semantically related object in the user's environment
        /// and the element as weighted by the association score).
        /// </summary>
        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            // If the UI is not associated with any semantic object, return 1
            if (associations.Count == 0)
            {
                return 1.0f;
            }

            // Get the distances between the element and the associated objects
            Vector3 targetPosition = optimizationTarget.Position;
            float[] distances = associations.Select(association => {
                return Mathf.Sqrt(
                    Mathf.Pow(targetPosition.x - association.gameObject.transform.position.x, 2) +
                    Mathf.Pow(targetPosition.y - association.gameObject.transform.position.y, 2) +
                    Mathf.Pow(targetPosition.z - association.gameObject.transform.position.z, 2)
                );
            }).ToArray();

            // Get the positive and negative associations
            float[] positiveAssociations = associations.Select(association => association.positiveScore).ToArray();
            float[] negativeAssociations = associations.Select(association => association.negativeScore).ToArray();

            // Calculate the semantic cost
            float semanticMismatch = GetSemanticMismatch(positiveAssociations, negativeAssociations, distances, positiveAssociationWeight, negativeAssociationWeight);
            return semanticMismatch;
        }

        public override Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout)
        {
            // Do nothing for now.
            return optimizationTarget;
        }

        public override Layout DirectRule(Layout optimizationTarget)
        {
            // Do nothing for now.
            return optimizationTarget;
        }
    }
}