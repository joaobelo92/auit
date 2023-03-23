using AUIT.AdaptationObjectives;
using AUIT.AdaptationObjectives.Definitions;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;

namespace AUIT.Objectives
{
    public class ObjectiveButtonHolder : MonoBehaviour
    {
        [Header("Objective")]
        [SerializeField]
        private LocalObjectiveHandler localObjectiveHandler;

        // This makes assumptions about the current local objectives...
        enum ObjectiveType
        {
            None,
            DistanceIntervalObjective,
            FieldOfViewObjective,
            CollisionObjective,
            OcclusionObjective,
            RotationObjective
        };
        [SerializeField]
        private ObjectiveType objectiveType = ObjectiveType.None;
        [SerializeField]
        private bool addMissingObjectives = true;

        [Header("UI Objects")]
        [SerializeField]
        private GameObject button;
        [SerializeField]
        private TextMeshPro weightLabel;
        [SerializeField]
        private PinchSlider weightPinchSlider;
        [SerializeField]
        private TMP_Dropdown contextSourceDropDown;

        void OnEnable()
        {
            UpdateSliderValue();
            UpdateDropDownOptions();
        }

        void Start()
        {
            if (localObjectiveHandler == null)
            {
                Debug.LogWarning("'LocalObjectiveHandler' is not specified...");
                enabled = false;
                return;
            }
            if (button == null)
            {
                Debug.LogWarning("'Button' is not specified...");
                enabled = false;
                return;
            }
            if (weightLabel == null)
            {
                Debug.LogWarning("'WightTMP' is not specified...");
                enabled = false;
                return;
            }
            if (weightPinchSlider == null)
            {
                Debug.LogWarning("'WeightPinchSlider' is not specified...");
                enabled = false;
                return;
            }
            if (contextSourceDropDown == null)
            {
                Debug.LogWarning("'ContextSourceDropDown' is not specified...");
                enabled = false;
                return;
            }

            UpdateSliderValue();
            UpdateDropDownOptions();
        }

        void Update()
        {

        }

        void LateUpdate()
        {
            UpdateButtonState();
        }

        private void UpdateButtonState()
        {
            LocalObjective objective = (LocalObjective)localObjectiveHandler.GetComponent(System.Enum.GetName(typeof(ObjectiveType), objectiveType));
            if (objective is null)
                return;

            button.GetComponent<Interactable>().IsToggled = objective.enabled;
        }

        public void ToogleObjective()
        {
            if (this.enabled == false)
                return;

            LocalObjective objective = (LocalObjective)localObjectiveHandler.GetComponent(System.Enum.GetName(typeof(ObjectiveType), objectiveType));
            if (objective is null)
                return;

            ToggleObjectiveOfType(objective.GetType());
        }

        private void ToggleObjectiveOfType(System.Type t)
        {
            // Do nothing if localSolverHandler is disabled
            if (localObjectiveHandler.enabled == false)
                return;

            LocalObjective objectiveOfType = (LocalObjective)localObjectiveHandler.GetComponent(t);
            bool foundObjective = (objectiveOfType != null);
            if (foundObjective)
            {
                objectiveOfType.enabled = !objectiveOfType.enabled;
            }
            else if (addMissingObjectives) 
            {
                // Should we add missing objectives?
                ((LocalObjective)localObjectiveHandler.gameObject.AddComponent(t)).enabled = true;
            }
        }

        private void UpdateSliderValue()
        {
            LocalObjective objective = (LocalObjective)localObjectiveHandler.GetComponent(System.Enum.GetName(typeof(ObjectiveType), objectiveType));
            if (objective is null)
                return;

            weightPinchSlider.SliderValue = (float)System.Math.Round(objective.Weight, 2);
            weightLabel.text = $"Weight: {objective.Weight.ToString("F2")}";
        }

        public void UpdateObjectiveWeight(SliderEventData sliderEventData)
        {
            if (this.enabled == false)
                return;

            LocalObjective objective = (LocalObjective)localObjectiveHandler.GetComponent(System.Enum.GetName(typeof(ObjectiveType), objectiveType));
            if (objective is null)
                return;

            objective.Weight = (float)System.Math.Round(sliderEventData.NewValue, 2);
            weightLabel.text = $"Weight: {sliderEventData.NewValue.ToString("F2")}";
        }

        private void UpdateDropDownOptions()
        {
            LocalObjective objective = (LocalObjective)localObjectiveHandler.GetComponent(System.Enum.GetName(typeof(ObjectiveType), objectiveType));
            if (objective is null)
                return;

            contextSourceDropDown.ClearOptions();
            string[] contextSourceNames = System.Enum.GetNames(typeof(ContextSource));
            foreach (string contextSourceName in contextSourceNames)
                contextSourceDropDown.options.Add(new TMP_Dropdown.OptionData(contextSourceName));
            contextSourceDropDown.RefreshShownValue();

            contextSourceDropDown.SetValueWithoutNotify((int)objective.ContextSource);
        }

        public void UpdateSolverContextSource(System.Int32 index)
        {
            if (this.enabled == false)
                return;

            LocalObjective objective = (LocalObjective)localObjectiveHandler.GetComponent(System.Enum.GetName(typeof(ObjectiveType), objectiveType));
            if (objective is null)
                return;

            string currentSelectionContextSourceName = contextSourceDropDown.captionText.text;
            ContextSource currentSelectedContextSource;
            bool parseSucces = System.Enum.TryParse<ContextSource>(currentSelectionContextSourceName, out currentSelectedContextSource);
            if (parseSucces == false)
                return;

            objective.ContextSource = currentSelectedContextSource;
        }

    }
}