using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.PropertyTransitions
{
    public abstract class PropertyTransition : MonoBehaviour
    {
        [HideInInspector]
        protected AdaptationManager AdaptationManager;
        
        // TODO: refactor to work with local handler
        protected virtual void Awake()
        {
            if (AdaptationManager == null)
            {
                AdaptationManager = GetComponent<AdaptationManager>();
            }
        }
        
        protected virtual void Start()
        {
            if (AdaptationManager != null)
            {
                AdaptationManager.RegisterPropertyTransition(this);
            }
        }
        protected virtual void OnDestroy()
        {
            if (AdaptationManager != null)
            {
                AdaptationManager.UnregisterPropertyTransition(this);
            }
        }
    }

    public interface IPositionAdaptation
    {
        public void Adapt(Transform objectTransform, Vector3 target);
        
        public void Adapt(GameObject ui, List<Layout> target);
    }

    public interface IRotationAdaptation
    {
        public void Adapt(Transform objectTransform, Quaternion target);
        public void Adapt(GameObject ui, List<Layout> target);
    }

    public interface IScaleAdaptation
    {
        public void Adapt(Transform objectTransform, Vector3 target);
        public void Adapt(GameObject ui, List<Layout> target);
    }
}