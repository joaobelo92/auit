using System.Collections.Generic;
using AUIT.AdaptationObjectives;
using AUIT.AdaptationTriggers;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;
using System;

namespace AUIT.PropertyTransitions
{
    /// <summary>
    /// This transition is used to move the object to the target position instantly.
    /// If more than one target position is provided, the GameObject is duplicated at the potential target positions.
    /// The GameObject is moved to the first target position in the provided list.
    /// </summary>
    public class ProposalTransition : PropertyTransition, IPositionAdaptation
    {

        // Property to store the adaptation placeholder GameObjects
        private List<GameObject> adaptationPlaceholders;

        /// <summary>
        /// Set the placeholder GameObject to be used for a potential adaptation.
        /// By default, it is a small sphere.
        /// </summary>
        [SerializeField]
        private GameObject adaptationPlaceholder = null;

        public void Adapt(Transform objectTransform, Vector3 target)
        {
            transform.position = target;
        }

        public void Adapt(GameObject ui, List<Layout> targets)
        {
            print("attempting adaptation...");
            adaptationPlaceholders = new List<GameObject>();

            foreach (Layout target in targets)
            {
                GameObject proposal = Instantiate(adaptationPlaceholder);
                proposal.transform.position = target.Position;
                proposal.SetActive(true);
                adaptationPlaceholders.Add(proposal);
            }
        }

        public void TriggerProposal(GameObject trigger)
        {
            transform.position = trigger.transform.position;
            transform.LookAt(Camera.main.transform);
            transform.forward = -transform.forward;
            foreach (var placeholder in adaptationPlaceholders)
            {
                Destroy(placeholder);
            }
        }
    }
}