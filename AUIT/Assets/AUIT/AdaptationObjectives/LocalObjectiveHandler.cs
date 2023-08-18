using System;
using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.PropertyTransitions;
using UnityEngine;

namespace AUIT.AdaptationObjectives
{
    public class LocalObjectiveHandler : MonoBehaviour
    {
        public string Id { get; } = Guid.NewGuid().ToString();

        private readonly List<Type> _objectiveTypes = new ();

        /// <summary>
        /// List of objectives that this handler will manage
        /// Can't see yet why we will need a setter
        /// </summary>
        public List<LocalObjective> Objectives { get; } = new ();

        private readonly List<OptimizationTarget> _optimizationTargets = new ();

        public void RegisterObjective(LocalObjective objective)
        {
            if (Objectives.Contains(objective))
                return;

            if (_objectiveTypes.Contains(objective.GetType()))
            {
                Debug.LogWarning($"A objective of type {objective.GetType()} has already been added");
                Destroy(objective);
                return;
            }

            Objectives.Add(objective);
            _objectiveTypes.Add(objective.GetType());
            RegisterOptimizationTarget(objective.OptimizationTarget);
        }

        public void UnregisterObjective(LocalObjective objective)
        {
            if (!Objectives.Contains(objective))
                return;

            Objectives.Remove(objective);
            _objectiveTypes.Remove(objective.GetType());
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

        public void Transition(Layout layout)
        {
            // TODO: loop and apply all transitions, locally manage them
        }

        #endregion
    }
}