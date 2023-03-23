using Microsoft.MixedReality.Toolkit.UI;
using AUIT.AdaptationObjectives;
using UnityEngine;
using TMPro;
using AUIT.AdaptationObjectives.Definitions;

namespace AUIT.Objectives
{
    public class ObjectiveOptionsMenu : MonoBehaviour
    {
        [SerializeField]
        private TMP_Dropdown contextSourceDropDown;
        [SerializeField]
        private PinchSlider weightPinchSlider;

        [SerializeField]
        private GameObject objectiveHolder;
        [SerializeField]
        private GameObject buttonCollection;

        private LocalObjectiveHandler localObjectiveHandler;
        private LocalObjective[] objectives;
        private LocalObjective correctObjective;

        // This makes assumptions about the current local objectives...
        enum ObjectiveType
        {
            RadialViewObjective,
            FieldOfViewObjective,
            CollisionObjective,
            OcclusionObjective,
            RotationObjective,
            SmoothAdaptationObjective
        };
        [SerializeField]
        private ObjectiveType objectiveType;

        void OnEnable()
        {
            UpdateSliderValue();
            UpdateDropDownOptions();
        }

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

            foreach (var objective in objectives)
            {
                string objectiveClassName = objective.GetType().Name;
                string enumObjectiveTypeString = System.Enum.GetName(typeof(ObjectiveType), objectiveType);
                bool isObjectiveClassCorrect = objectiveClassName.Equals(enumObjectiveTypeString);
                if (isObjectiveClassCorrect)
                {
                    correctObjective = objective;
                    break;
                }
            }

            UpdateSliderValue();
            UpdateDropDownOptions();
        }

        private void UpdateSliderValue()
        {
            if (correctObjective == null)
                return;
            if (weightPinchSlider == null)
                return;

            weightPinchSlider.SliderValue = correctObjective.Weight;
        }

        public void UpdateSolverWeight(SliderEventData sliderEventData)
        {
            if (correctObjective == null)
                return;

            correctObjective.Weight = sliderEventData.NewValue;
        }

        private void UpdateDropDownOptions()
        {
            if (correctObjective == null)
                return;
            if (contextSourceDropDown == null)
                return;

            contextSourceDropDown.ClearOptions();
            string[] contextSourceNames = System.Enum.GetNames(typeof(ContextSource));
            foreach (string contextSourceName in contextSourceNames)
                contextSourceDropDown.options.Add(new TMP_Dropdown.OptionData(contextSourceName));
            contextSourceDropDown.RefreshShownValue();

            contextSourceDropDown.SetValueWithoutNotify((int)correctObjective.ContextSource);
        }

        public void UpdateSolverContextSource(System.Int32 index)
        {
            if (correctObjective == null)
                return;

            string currentSelectionContextSourceName = contextSourceDropDown.captionText.text;
            ContextSource currentSelectedContextSource;
            bool parseSucces = System.Enum.TryParse<ContextSource>(currentSelectionContextSourceName, out currentSelectedContextSource);
            if (parseSucces)
                correctObjective.ContextSource = currentSelectedContextSource;
        }


    }
}
