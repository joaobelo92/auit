using System;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.AdaptationObjectives
{
    // Local Objectives have to derive from MonoBehaviour, so I'm unsure an interface for Global and Local is possible.
    [RequireComponent(typeof(LocalObjectiveHandler))]
    public abstract class LocalObjective : MonoBehaviour
    {
        #region Context Source Logic

        protected LocalObjectiveHandler ObjectiveHandler;

        [SerializeField]
        private ContextSource contextSource = ContextSource.Gaze;

        // In this initial version we will support Head (gaze), Player position and Hands
        // Player position will be retrieved from gaze, but some more sophisticated heuristic can be used to retrieve
        // body pose for example
        protected ContextSource ContextSource
        {
            get => contextSource;
            set
            {
                if (contextSource != value)
                {
                    contextSource = value;
                    // Need updated logic when supporting more than Transforms
                    RefreshContextSource();
                }
            }
        }

        public OptimizationTarget OptimizationTarget { get; set; } = OptimizationTarget.Position;

        // For context sources that are transforms, we follow MRTK's approach and add a Child GameObject to the 
        // GameObject with the transform of interest
        private object _contextSourceTarget;

        /// <summary>
        /// In this initial iteration we will limit ourselves to Transforms, but the ContextSource must support other
        /// types in the future. Context sources, besides transforms such as gaze and position of limbs/objects
        /// can be geometry, cognitive load, lighting conditions and so on.
        /// </summary>
        protected object ContextSourceTransformTarget
        {
            get
            {
                // As it is done in MRTK, getter must check if context source is still valid (e.g. hand pos unknown).
                // We will need a strategy in the optimization process when it is not valid. Easiest will be to ignore
                // the objective.
                if (IsContextSourceTransform(contextSource))
                {
                    GameObject contextSourceGameObject = (GameObject)_contextSourceTarget;
                    // Create copies of position and rotation

                    if (contextSource == ContextSource.PlayerPose)
                    {
                        return contextSourceGameObject.transform.parent.position;
                    }
                    if (contextSource == ContextSource.CustomTransform)
                    {
                        if (transformOverride != null)
                            return transformOverride;
                        
                        Debug.LogWarning("Please set 'TransformOverride' before changing 'ContextSource'...");
                        contextSource = ContextSource.Gaze;
                        return ContextSourceTransformTarget;
                    }

                    if (contextSource == ContextSource.Gaze)
                    {
                        return contextSourceGameObject.transform.parent;
                    }
                }

                throw new Exception("No context Transform");
            }
        }

        [SerializeField]
        [Tooltip("Only used if Context Source is set to Custom Transform.")]
        protected Transform transformOverride;

        public Transform TransformOverride
        {
            set
            {
                if (value != null && transformOverride != value)
                {
                    transformOverride = value;
                    RefreshContextSource();
                }
            }
        }

        [SerializeField]
        [Range(0, 1)]
        private float weight = 0.5f;
        public float Weight { get { return weight; } set { weight = value; } }

        public abstract float CostFunction(Layout optimizationTarget, Layout initialLayout = null);

        /// <summary>
        /// We will want to apply rules with different odds. Still need to figure out if that should be defined
        /// per rule and hardcoded or not.
        /// For some objectives we know how to compute the ground truth for the optimal values in regards to a
        /// particular loss function, perhaps that should be also in the objectives for greedy optimization approaches
        /// </summary>
        /// <param name="optimizationTarget"></param>
        /// <returns></returns>
        public abstract Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout = null);

        /// <summary>
        /// For now, greedy rules are optional. 
        /// </summary>
        /// <param name="optimizationTarget"></param>
        /// <returns></returns>
        public abstract Layout DirectRule(Layout optimizationTarget);

        private void RefreshContextSource()
        {
            DetachFromCurrentTrackedObject();
            // Add logic for other sources of context in the future
            AttachToNewTrackedObject();
        }

        private void DetachFromCurrentTrackedObject()
        {
            if (_contextSourceTarget != null)
            {
                GameObject contextSourceGameObject = (GameObject)_contextSourceTarget;
                Destroy(contextSourceGameObject);
                _contextSourceTarget = null;
            }
        }

        /// <summary>
        /// This approach might be a lot of overhead in the future. Perhaps for each source of context used in the
        /// application, a Objective System should have it easily available as planned initially.
        /// However, a different logic will be necessary if we wish the ObjectiveHandler to be able to work
        /// even if such a System is not enabled
        /// </summary>
        private void AttachToNewTrackedObject()
        {
            Transform target = null;
            if (ContextSource == ContextSource.Gaze)
            {
                target = Camera.main.transform;
            }
            else if (ContextSource == ContextSource.PlayerPose)
            {
                // As of now, camera pos will be used as an approximation, but user pose should be different 
                target = Camera.main.transform;
            }
            else if (ContextSource == ContextSource.CustomTransform)
            {
                target = transformOverride;
            }

            TrackTransform(target);
        }

        private void TrackTransform(Transform target)
        {
            if (_contextSourceTarget != null || target == null) 
                return;

            string name = $"LocalObjective Target on {target.gameObject.name}";
            GameObject tracker = new GameObject(name);

            tracker.transform.parent = target;

            _contextSourceTarget = tracker;
        }

        private static bool IsContextSourceTransform(ContextSource contextSource)
        {
            return contextSource <= ContextSource.CustomTransform;
        }

        #endregion

        #region MonoBehaviour Implementation

        protected virtual void Awake()
        {
            if (ObjectiveHandler == null)
                ObjectiveHandler = GetComponent<LocalObjectiveHandler>();
        }

        protected virtual void OnEnable()
        {
            if (ObjectiveHandler == null)
                ObjectiveHandler = GetComponent<LocalObjectiveHandler>();
            ObjectiveHandler.RegisterObjective(this);
            RefreshContextSource();
        }

        protected virtual void Start()
        {
            RefreshContextSource();
        }

        protected virtual void OnDisable()
        {
            if (ObjectiveHandler != null)
                ObjectiveHandler.UnregisterObjective(this);
        }

        #endregion




    }
}