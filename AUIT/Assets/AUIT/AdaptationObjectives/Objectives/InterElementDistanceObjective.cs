using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.AdaptationObjectives.Extras;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AUIT.AdaptationObjectives.Objectives
{
    public class InterElementDistanceObjective : MultiElementObjective
    {
        [SerializeField]
        private float targetDistance = 0.5f;

        [SerializeField]
        private float innerDistance = 0.3f;

        [SerializeField]
        private float outerDistance = 0.3f;

        public override float CostFunction(Layout optimizationTarget, Layout[] optimizationTargets, Layout initialLayout = null)
        {
            float cost = 0;
            float numDist = 0;

            int targetIndex = Array.IndexOf(optimizationTargets, initialLayout);

            for (int i = 0; i < optimizationTargets.Length; i++)
            {
                if (i != targetIndex)
                {
                    Layout layout = optimizationTargets[i];
                    float distance = (optimizationTarget.Position - layout.Position).magnitude;
                    cost += Mathf.Clamp01((Mathf.Abs(distance - targetDistance) - innerDistance) / outerDistance);
                    numDist++;
                }
            }

            float normalizationFactor = Mathf.Max(1, numDist);
            Debug.Log(cost);
            return cost / normalizationFactor;
        }


        public override float CostFunction(Layout[] optimizationTargets, Layout initialLayout = null)
        {
            float cost = 0f;
            float numDist = 0; 
            for (int i = 0; i < optimizationTargets.Length - 1; i++)
            {
                for (int j = i+1; j < optimizationTargets.Length; j++)
                {
                    Layout l1 = optimizationTargets[i];
                    Layout l2 = optimizationTargets[j];
                    float distance = (l1.Position - l2.Position).magnitude;
                    cost += Mathf.Clamp01((Mathf.Abs(distance - targetDistance) - innerDistance) / outerDistance);
                    numDist++;
                }
            }

            float normalizationFactor = Mathf.Max(1, numDist);
            return cost / normalizationFactor;
        }

        public override List<Layout> OptimizationRule(List<Layout> optimizationTargets, Layout initialLayout = null)
        {
            int iMin = -1;
            int jMin = -1;
            float distMin = Mathf.Infinity; 

            for (int i = 0; i < optimizationTargets.Count - 1; i++)
            {
                for (int j = i + 1; j < optimizationTargets.Count; j++)
                {
                    Layout l1 = optimizationTargets[i];
                    Layout l2 = optimizationTargets[j];
                    float distance = (l1.Position - l2.Position).magnitude;
                    if (distance < distMin)
                    {
                        distMin = distance;
                        iMin = i;
                        jMin = j;
                    }
                }
            }

            int iMove = -1;
            Vector3 move = Vector3.zero;
            if (iMin >= 0 && jMin >= 0)
            {
                int iAnchor;
                if (Random.value > 0.5)
                {
                    iMove = iMin;
                    iAnchor = jMin;
                }
                else
                {
                    iMove = jMin;
                    iAnchor = iMin;
                }
                Layout layoutMove = optimizationTargets[iMove];
                Layout layoutAnchor = optimizationTargets[iAnchor];
                Vector3 displacement = layoutAnchor.Position - layoutMove.Position;
                float distance = displacement.magnitude;
                float diff = distance - targetDistance;
                move = Mathf.Sign(diff) * displacement.normalized;

                if (Random.value > 0.5)
                {
                    move += Random.insideUnitSphere;
                }
            } else
            {
                iMove = Random.Range(0, optimizationTargets.Count);
                move = Random.insideUnitSphere;
            }

            optimizationTargets[iMove].Position += 0.05f *HelperMath.SampleNormalDistribution(0.5f, 0.5f) * move;


            return optimizationTargets;
        }

    }
}