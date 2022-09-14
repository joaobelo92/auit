using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using AUIT.AdaptationObjectives;
using UnityEngine;

namespace AUIT.Objectives
{
    public class ObjectiveMenu : MonoBehaviour
    {
        [SerializeField]
        private GameObject objectiveHolder;
        [SerializeField]
        private GameObject buttonCollection;
        [SerializeField]
        private bool addMissingObjectives = true;

        private LocalObjectiveHandler localObjectiveHandler;
        private LocalObjective[] objectives;

        void Start()
        {
            if (objectiveHolder == null)
            {
                Debug.LogWarning("Objective Holder is not specified...");
                enabled = false;
                return;
            }
            if (buttonCollection == null)
            {
                Debug.LogWarning("Button Collection is not specified...");
                enabled = false;
                return;
            }

            localObjectiveHandler = objectiveHolder.GetComponent<LocalObjectiveHandler>();
            if (localObjectiveHandler == null)
            {
                Debug.LogWarning("Unable to find LocalObjectiveHandler on '" + objectiveHolder.name + "'...");
                enabled = false;
                return;
            }

            objectives = objectiveHolder.GetComponents<LocalObjective>();
            if (objectives.Length == 0)
            {
                Debug.LogWarning("Unable to find any local objectives on '" + objectiveHolder.name + "'...");
                enabled = false;
                return;
            }

        }

        void LateUpdate()
        {
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            buttonCollection.transform.GetChild(0).GetComponent<Interactable>().IsToggled = localObjectiveHandler.enabled;

            foreach (Transform objectiveButtons in buttonCollection.transform)
            {
                if (objectiveButtons == buttonCollection.transform.GetChild(0))
                    continue;
                // Set all objective toggle's game objects to match 
                // the state of the local objective handler game object.
                objectiveButtons.gameObject.SetActive(localObjectiveHandler.enabled);
            }

            foreach (var objective in objectives)
            {
                // This makes assumptions about the structure of the buttons in the buttonCollection game object...
                if (objective is DistanceIntervalObjective)
                    buttonCollection.transform.GetChild(1).GetComponent<Interactable>().IsToggled = objective.enabled;
                if (objective is FieldOfViewObjective)
                    buttonCollection.transform.GetChild(2).GetComponent<Interactable>().IsToggled = objective.enabled;
                if (objective is CollisionObjective)
                    buttonCollection.transform.GetChild(3).GetComponent<Interactable>().IsToggled = objective.enabled;
                if (objective is LookTowardsObjective)
                    buttonCollection.transform.GetChild(4).GetComponent<Interactable>().IsToggled = objective.enabled;
            }
        }

        public void ToggleMenu()
        {
            // This works even when the gameObject is not active! :D
            gameObject.SetActive(!gameObject.activeSelf);
        }

        public void ToggleSolverHandler() { localObjectiveHandler.enabled = !localObjectiveHandler.enabled; }
        public void ToggleRadialViewSolver() { ToggleLocalSolverOfType<DistanceIntervalObjective>(); }
        public void ToggleFieldOfViewSolver() { ToggleLocalSolverOfType<FieldOfViewObjective>(); }
        public void ToggleCollisionSolver() { ToggleLocalSolverOfType<CollisionObjective>(); }
        public void ToggleRotationSolver() { ToggleLocalSolverOfType<LookTowardsObjective>(); }

        private void ToggleLocalSolverOfType<T>() where T : MonoBehaviour
        {
            // Do nothing if localSolverHandler is disabled
            if (localObjectiveHandler.enabled == false)
                return;

            bool foundObjective = false;

            // Instead of always looping through all local objectives,
            // we could have a field variable for each objective type?
            foreach (LocalObjective objective in objectives)
            {
                if (objective is T)
                {
                    objective.enabled = !objective.enabled;
                    foundObjective = true;
                    break;
                }
            }

            // Should we add missing objectives?
            if (addMissingObjectives && foundObjective == false)
            {
                objectiveHolder.AddComponent<T>().enabled = true;
                objectives = objectiveHolder.GetComponents<LocalObjective>();
            }
        }
    }
}