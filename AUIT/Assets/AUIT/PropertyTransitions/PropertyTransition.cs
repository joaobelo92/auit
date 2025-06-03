using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.PropertyTransitions
{
    public enum TransitionType 
    {
        Position,
        Rotation,
        Scale,
        CoordinateSystem
    }
    
    public abstract class PropertyTransition : MonoBehaviour
    {
        [HideInInspector]
        protected AUIT Auit;
        
        protected abstract TransitionType TransitionType { get; }
        public TransitionType GetTransitionType() => TransitionType;
        
        // TODO: refactor to work with local handler
        protected virtual void Awake()
        {
            if (Auit == null)
            {
                Auit = GetComponent<AUIT>();
            }
        }
        
        protected virtual void Start()
        {
            if (Auit != null)
            {
                Auit.RegisterTransition(this);
            }
        }
        protected virtual void OnDestroy()
        {
            if (Auit != null)
            {
                Auit.UnregisterTransition(this);
            }
        }
        
        
        public abstract void Adapt(Layout layout);
    }
    
}