using System;
using System.Collections.Generic;
using System.Linq;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.PropertyTransitions;
using UnityEngine;

namespace AUIT.AdaptationObjectives
{
    public class LocalObjectiveHandler : MonoBehaviour
    {
        private PropertyTransition[] _propertyTransitions;
        private void Start()
        {
            _propertyTransitions = this.GetComponents<PropertyTransition>();
        }

        public string Id { get; } = Guid.NewGuid().ToString();
        
        public List<LocalObjective> Objectives { get; } = new ();

        private readonly List<OptimizationTarget> _optimizationTargets = new ();

        public void RegisterObjective(LocalObjective objective)
        {
            if (Objectives.Contains(objective))
                return;

            Objectives.Add(objective);
            RegisterOptimizationTarget(objective.OptimizationTarget);
        }

        public void UnregisterObjective(LocalObjective objective)
        {
            if (!Objectives.Contains(objective))
                return;

            Objectives.Remove(objective);
            //_objectiveTypes.Remove(objective.GetType());
            UnregisterOptimizationTarget(objective.OptimizationTarget);
        }

        private void RegisterOptimizationTarget(OptimizationTarget optimizationTarget)
        {
            if (!_optimizationTargets.Contains(optimizationTarget))
                _optimizationTargets.Add(optimizationTarget);
        }

        private void UnregisterOptimizationTarget(OptimizationTarget optimizationTarget)
        {
            if (_optimizationTargets.Contains(optimizationTarget))
                _optimizationTargets.Remove(optimizationTarget);
        }

        #region PropertyTransitionLogic

        /// <summary>
        /// Initiates a layout transition by invoking all registered <see cref="PropertyTransition"/>s.
        /// </summary>
        /// <param name="layout">The <see cref="Layout"/> containing target state information for the transition.</param>
        /// <remarks>
        /// This method performs the following steps:
        /// <list type="number">
        ///   <item>Checks if any property transitions are available; if not, the method exits early.</item>
        ///   <item>
        ///     Sorts the transitions so that those of type <see cref="TransitionType.CoordinateSystem"/>
        ///     are executed before other types. This ensures that any coordinate system-related
        ///     changes are applied first, which may be required for subsequent transitions to function correctly.
        ///   </item>
        ///   <item>Iterates over each <see cref="PropertyTransition"/> and calls its <c>Adapt()</c> method, passing the layout.</item>
        /// </list>
        /// </remarks>
        public void Transition(Layout layout)
        {
            if (_propertyTransitions == null || _propertyTransitions.Length == 0)
                return;
            
            _propertyTransitions = _propertyTransitions
                .OrderByDescending(t => t.GetTransitionType() == TransitionType.CoordinateSystem)
                .ToArray();
            
            foreach (PropertyTransition propertyTransition in _propertyTransitions)
            {
                propertyTransition.Adapt(layout);
            }
        }

        #endregion
    }
}