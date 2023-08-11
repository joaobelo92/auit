using System;
using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.AdaptationObjectives
{
    public class LocalObjectiveHandler : MonoBehaviour
    {
        public string Id { get; } = Guid.NewGuid().ToString();

        protected readonly List<Type> objectiveTypes = new ();
        /// <summary>
        /// List of objective types that this handler will manage
        /// </summary>
        public IReadOnlyCollection<Type> ObjectiveTypes => objectiveTypes.AsReadOnly();

        protected readonly List<LocalObjective> objectives = new ();
        /// <summary>
        /// List of objectives that this handler will manage
        /// Can't see yet why we will need a setter
        /// </summary>
        public List<LocalObjective> Objectives => objectives;
        
        protected readonly List<OptimizationTarget> optimizationTargets = new ();

        public IReadOnlyCollection<OptimizationTarget> OptimizationTargets => optimizationTargets.AsReadOnly();

        // In contrast to the older objectives, properties we optimize for (e.g. GoalPosition, GoalRotation) 
        // might have to be more flexible and will be dependent on the objectives.
        // What does this element optimize for and how to make that clear? 
        // Some possibilities: Position, Rotation, Scale, LoD, Visibility and Modality
        // Note: Scale is dependent on distance of the object from the user. Probably need something better
        // "Target" or source of context will also be moved to the objective

        public void RegisterObjective(LocalObjective objective)
        {
            if (objectives.Contains(objective))
                return;

            if (objectiveTypes.Contains(objective.GetType()))
            {
                Debug.LogWarning($"A objective of type {objective.GetType()} has already been added");
                Destroy(objective);
                return;
            }

            objectives.Add(objective);
            objectiveTypes.Add(objective.GetType());
            RegisterOptimizationTarget(objective.OptimizationTarget);
        }

        public void UnregisterObjective(LocalObjective objective)
        {
            if (!objectives.Contains(objective))
                return;

            objectives.Remove(objective);
            objectiveTypes.Remove(objective.GetType());
            UnregisterOptimizationTarget(objective.OptimizationTarget);
        }

        private void RegisterOptimizationTarget(OptimizationTarget optimizationTarget)
        {
            if (!optimizationTargets.Contains(optimizationTarget))
                optimizationTargets.Add(optimizationTarget);
        }

        private void UnregisterOptimizationTarget(OptimizationTarget optimizationTarget)
        {
            if (optimizationTargets.Contains(optimizationTarget))
                optimizationTargets.Remove(optimizationTarget);
        }
    }
}