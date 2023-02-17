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

        // Property to store the adaptation placeholder GameObjects
        private List<GameObject> adaptationPlaceholders = new List<GameObject>();

        /// <summary>
        /// Set the placeholder GameObject to be used for a potential adaptation.
        /// By default, it is a small sphere.
        /// </summary>
        [SerializeField]
        private GameObject adaptationPlaceholder = null;

        /// <summary>
        /// Set the scale down factor for the adaptation placeholders.
        /// By default, it is 0.5.
        /// </summary>
        [SerializeField]
        private float scaleDownFactor = 0.05f;

        [SerializeField]
        /// <summary>
        /// If true, the GameObject is rotated based on the info in the target layout.
        /// </summary>
        private bool rotateBasedOnTarget = false;

        public void Adapt(Transform objectTransform, Vector3 target)
        {
            transform.position = target;
        }

        public void Adapt(GameObject ui, List<Layout> targets)
        {
            if (targets.Count > 0)
            {
                ui.transform.position = targets[0].Position;
                if (rotateBasedOnTarget)
                {
                    ui.transform.rotation = targets[0].Rotation;
                }
            }
            if (targets.Count == 1) return;

            GameObject adaptationPlaceholdersParent = GetAdaptationsParent();

            // Loop through all potential adaptations (i.e., all target positions except the first one)
            // and adapt the position of the already existing GameObject duplicates until
            // all duplicates are at suggested positions. Then, create new duplicates at the remaining
            // potential target positions and store them in the duplicates list.
            for (int i = 1; i < targets.Count; i++)
            {
                if (i < adaptationPlaceholders.Count)
                {
                    adaptationPlaceholders[i].transform.position = targets[i].Position;
                    if (rotateBasedOnTarget)
                    {
                        adaptationPlaceholders[i].transform.rotation = targets[i].Rotation;
                    }
                }
                else
                {
                    GameObject placeholder = GetPlaceholder(ui.name + " (Potential Adaptation)", targets[i], adaptationPlaceholdersParent);
                    AddSelectEventsToPlaceholder(ui, placeholder);
                    // Store the duplicate in the duplicates list
                    adaptationPlaceholders.Add(placeholder);
                }
            }

            // If there are more duplicates than potential target positions, destroy the remaining duplicates
            // and remove them from the duplicates list
            if (adaptationPlaceholders.Count > targets.Count)
            {
                for (int i = targets.Count; i < adaptationPlaceholders.Count; i++)
                {
                    Destroy(adaptationPlaceholders[i]);
                }

                adaptationPlaceholders.RemoveRange(targets.Count, adaptationPlaceholders.Count - targets.Count);
            }

            // If the adaptation placeholder is enabled and not ui (no duplicate UIs), disable it
            // This is the case when the user has selected a different adaptation placeholder in the scene
            if (adaptationPlaceholder != null && adaptationPlaceholder != ui)
            {
                adaptationPlaceholder.SetActive(false);
            }
        }

        private void AddSelectEventsToPlaceholder(GameObject ui, GameObject placeholder)
        {
            // Attach the SwapPositionsOnTouch script to the placeholder object
            SwapPositionsOnTouch swapPositionsOnTouch = placeholder.AddComponent<SwapPositionsOnTouch>();

            // Set the ui object of the SwapPositionsOnTouch script
            swapPositionsOnTouch.ui = ui;
        }


        /// <summary>
        /// Create a placeholder GameObject at the target position.
        /// If no placeholder GameObject is provided, a small grey sphere is created.
        /// If a placeholder GameObject is provided, it is instantiated and all scripts related to the AUIT framework are disabled.
        /// The placeholder GameObject is added to the adaptation placeholders parent GameObject.
        /// </summary>
        private GameObject GetPlaceholder(string name, Layout target, GameObject adaptationPlaceholdersParent)
        {
            GameObject placeholder = null;

            if (adaptationPlaceholder == null)
            {
                placeholder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Material material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.2f, 0.2f, 0.2f);
                placeholder.GetComponent<Renderer>().material = material;
            }
            else
            {
                placeholder = Instantiate(adaptationPlaceholder);
                // Disable all scripts related to the AUIT framework
                var scripts = placeholder.GetComponents<MonoBehaviour>();
                foreach (var script in scripts)
                {
                    // If script's namespace includes AUIT, disable it
                    if (script.GetType().Namespace != null && script.GetType().Namespace.Contains("AUIT"))
                    {
                        script.enabled = false;
                    }
                }
            }
          
            placeholder.name = name;
            placeholder.transform.localScale = new Vector3(scaleDownFactor, scaleDownFactor, scaleDownFactor);
            placeholder.transform.SetParent(adaptationPlaceholdersParent.transform);
            BoxCollider boxCollider = placeholder.AddComponent<BoxCollider>();
            boxCollider.size = placeholder.transform.localScale;

            return placeholder;
        }

        private static GameObject GetAdaptationsParent()
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

        private class SwapPositionsOnTouch : MonoBehaviour
        {
            public GameObject ui;

            private void SwapPositions(GameObject ui)
            {
                Vector3 temp = ui.transform.position;
                ui.transform.position = transform.position;
                transform.position = temp;
            }

            private void OnMouseDown()
            {
                SwapPositions(ui);
            }

            private void OnTouchDown()
            {
                SwapPositions(ui);
            }
        }
    }
}