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
    public class InstantMovementTransition : PropertyTransition, IPositionAdaptation
    {

        // Property to store the duplicate GameObjects
        private List<GameObject> duplicates = new List<GameObject>();

        public bool rotateBasedOnTarget = false;
        public bool scaleDownDuplicates = true;

        public void Adapt(Transform objectTransform, Vector3 target)
        {
            transform.position = target;
        }

        public void Adapt(GameObject ui, List<Layout> targets)
        {
            if (targets.Count > 0)
            {
                ui.transform.position = targets[0].Position;
            }

            GameObject duplicatesParent = GetDuplicatesParent();

            // Loop through all potential adaptations (i.e., all target positions except the first one)
            // and adapt the position of the already existing GameObject duplicates until
            // all duplicates are at suggested positions. Then, create new duplicates at the remaining
            // potential target positions and store them in the duplicates list.
            for (int i = 1; i < targets.Count; i++)
            {
                if (i < duplicates.Count)
                {
                    duplicates[i].transform.position = targets[i].Position;
                }
                else
                {
                    GameObject duplicate = CreateDuplicate(ui, targets[i], duplicatesParent);
                    // Store the duplicate in the duplicates list
                    duplicates.Add(duplicate);
                }
            }

            // If there are more duplicates than potential target positions, destroy the remaining duplicates
            // and remove them from the duplicates list
            if (duplicates.Count > targets.Count)
            {
                for (int i = targets.Count; i < duplicates.Count; i++)
                {
                    Destroy(duplicates[i]);
                }

                duplicates.RemoveRange(targets.Count, duplicates.Count - targets.Count);
            }
        }

        private GameObject CreateDuplicate(GameObject ui, Layout target, GameObject duplicatesParent)
        {
            // Create a duplicate of the GameObject
            GameObject duplicate = Instantiate(ui, target.Position, rotateBasedOnTarget ? target.Rotation : ui.transform.rotation);
            duplicate.name = ui.name + " (Potential Adaptation)";
            duplicate.transform.SetParent(ui.transform.parent);
            duplicate.transform.localScale = scaleDownDuplicates ? new Vector3(0.5f, 0.5f, 0.5f) : ui.transform.localScale;


            // Disable all scripts related to the AUIT framework
            var scripts = duplicate.GetComponents<MonoBehaviour>();
            foreach (var script in scripts)
            {
                // If script's namespace includes AUIT, disable it
                if (script.GetType().Namespace.Contains("AUIT"))
                {
                    script.enabled = false;
                }
            }

            // Add the duplicate to the duplicates parent
            duplicate.transform.SetParent(duplicatesParent.transform);

            return duplicate;
        }

        private static GameObject GetDuplicatesParent()
        {
            // If none exists, create a new parent for the duplicates
            GameObject duplicatesParent = GameObject.Find("Potential Adaptations");
            if (duplicatesParent == null)
            {
                duplicatesParent = new GameObject();
                duplicatesParent.name = "Potential Adaptations";
            }

            return duplicatesParent;
        }
    }
}