using AUIT.AdaptationObjectives.Definitions;
using AUIT.AdaptationObjectives.Extras;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AUIT.AdaptationObjectives
{
    public class TaskCenteredObjective : LocalObjective
    {
        [SerializeField]
        private ContextSource<TaskContextSource.Task> taskContextSource;

        [SerializeField]
        private ContextSource<Transform> userContextSource;

        [SerializeField]
        private float maxAngle = 90f;

        [Serializable]
        public class Relevance
        {
            [SerializeField]
            public TaskContextSource.Task task;

            [SerializeField]
            [Range(0,1)]
            public float value;
        }


        [SerializeField]
        private List<Relevance> taskRelevances;

        public override ObjectiveType ObjectiveType => throw new NotImplementedException();

        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            if (userContextSource == null)
            {
                Debug.LogError("FieldOfViewObjective.CostFunction(): User context source is not set.");
            }

            // Idea: get angle between gaze and object vectors on y and x axis
            // Then we define intervals that are acceptable, e.g. comprising near/mid/far peripheral view
            // Cost function increases the further is is from that interval
            Transform contextSourceTransform = userContextSource.GetValue();
            Vector3 target = contextSourceTransform.worldToLocalMatrix.MultiplyPoint3x4(optimizationTarget.Position);

            float angle = Vector3.Angle(Vector3.forward, target);
            if (angle > maxAngle)
            {
                return 1;
            }
            float cost = Mathf.Min(angle / maxAngle, 1);
            float inverseCost = 1 - cost;

            float taskRelevance = 1; 
            TaskContextSource.Task task = taskContextSource.GetValue();
            foreach (Relevance relevance in taskRelevances)
            {
                if (relevance.task == task)
                {
                    taskRelevance = relevance.value;
                    break;
                }
            }
            cost = taskRelevance * cost + (1 - taskRelevance) * inverseCost;

            return cost;
        }

        public override Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout)
        {
            if (userContextSource == null)
            {
                Debug.LogError("FieldOfViewObjective.OptimizationRule(): User context source is not set.");
            }

            Transform contextSourceTransform = userContextSource.GetValue();
            Vector3 target = contextSourceTransform.worldToLocalMatrix.MultiplyPoint3x4(optimizationTarget.Position);

            Layout result = optimizationTarget.Clone();
            if (Random.value < 0.5f)
            {
                Vector3 move = contextSourceTransform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(0, 0, target.magnitude)) - optimizationTarget.Position;
                result.Position = optimizationTarget.Position + move * HelperMath.SampleNormalDistribution(0.1f, 0.1f);
            }
            else 
            {
                result.Position = optimizationTarget.Position + Random.insideUnitSphere * HelperMath.SampleNormalDistribution(0.5f, 0.5f) * 0.01f;
            }

            return result;
        }

        public override Layout DirectRule(Layout optimizationTarget)
        {
            Transform contextSourceTransform = userContextSource.GetValue();
            // Would be efficient to cache rotation when cost function is computed
            Vector3 target = contextSourceTransform.worldToLocalMatrix.MultiplyPoint3x4(optimizationTarget.Position);
            
            Quaternion quaternion = Quaternion.FromToRotation(target, Vector3.forward);
            Layout result = optimizationTarget.Clone();
            result.Position = contextSourceTransform.localToWorldMatrix.MultiplyPoint(quaternion * target);

            return result;
        }

        public override float[] GetParameters()
        {
            throw new System.NotImplementedException();
        }

        public override void SetParameters(float[] parameters)
        {
            throw new System.NotImplementedException();
        }
    }
}