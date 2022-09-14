using System;
using System.Collections.Generic;
using AUIT.AdaptationTriggers;
using AUIT.AdaptationObjectives.Definitions;
using DataStructures.ViliWonka.KDTree;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AUIT.AdaptationObjectives
{
    [RequireComponent(typeof(AdaptationManager))]
    public class SpatialCoherenceObjective : LocalObjective, AdaptationListener
    {
        // Get the AdaptationManager on this object
        private AdaptationManager adaptationManager;

        // Store known adaptations
        private List<Vector3> knownOptimizationsCloud;
        
        // To avoid unnecessary initialization steps, voxel grid will refer to usage of position 
        // https://docs.unity3d.com/ScriptReference/Vector3.Equals.html
        private (int score, Vector3 position)[,,] voxelUsage;

        private int sceneDimensionsX = 10;
        private int sceneDimensionsY = 3;
        private int sceneDimensionsZ = 10;

        private float voxelSize = 0.1f;
        
        [SerializeField, Tooltip("K Nearest points to choose from in optimization strategy")]
        private int kNearestPoints = 3;

        [SerializeField, Tooltip("Number of updates in the UI allowed before a position is forgotten")]
        private int updatesAllowed = 10;

        [SerializeField]
        private GameObject test;

        protected override void Start()
        {
            base.Start();
            knownOptimizationsCloud = new List<Vector3>();
            voxelUsage = new (int, Vector3)[(int) (sceneDimensionsX / voxelSize), (int) (sceneDimensionsY / voxelSize), (int) (sceneDimensionsZ / voxelSize)];

            Vector3 startPosition = transform.position;
            (int x, int y, int z)? indexGrid = mapPositionToVoxelGrid(startPosition);
            if (indexGrid != null)
            {
                voxelUsage[indexGrid.Value.x, indexGrid.Value.y, indexGrid.Value.z] = (updatesAllowed, startPosition);
                knownOptimizationsCloud.Add(startPosition);
            }
            else
            {
                Debug.Log("Current adaptation position voxel out of bounds for spatial coherence solver.");
            }
        }
        
        // Idea - Use voxel grid to store cost and KD-Tree to store concrete positions already used
        // Mapping to look into voxel grid - map from pos to index
        // What position to store in KD-Tree? 
        // Cost - Highest value if not far from previous position
        private (int x, int y, int z)? mapPositionToVoxelGrid(Vector3 pos)
        {
            // check if out of bounds 
            if (pos.x > sceneDimensionsX / 2f || pos.x < -sceneDimensionsX / 2f ||
                pos.y > sceneDimensionsY / 2f || pos.y < -sceneDimensionsY / 2f ||
                pos.z > sceneDimensionsZ / 2f || pos.z < -sceneDimensionsZ / 2f)
            {
                Debug.Log("Position out of bounds for spatial coherence objective");
                return null;
            }

            Vector3 positionOffset = new Vector3(sceneDimensionsX / 2f, sceneDimensionsY / 2f, sceneDimensionsZ / 2f);
            Vector3 gridPosition = (pos + positionOffset) / voxelSize;

            (int x, int y, int z) index = ((int)Math.Floor(gridPosition.x), (int)Math.Floor(gridPosition.y),
                (int)Math.Floor(gridPosition.z));

            return index;
        }

        private Vector3 GetClosestOptimization(Vector3 position, int k = 1)
        {
            // Use the KD-Tree data structure to efficiently search for closest positions
            KDTree tree = new KDTree(knownOptimizationsCloud.ToArray());
            KDQuery query = new KDQuery();
            List<int> results = new List<int>();

            if (k != 1)
            {
                k = kNearestPoints >= knownOptimizationsCloud.Count ? kNearestPoints : knownOptimizationsCloud.Count;
            }

            // Find the closest known optimization target
            query.KNearest(tree, position, k, results);

            return knownOptimizationsCloud[results[Random.Range(0, results.Count)]];
        }

        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            if (GetClosestOptimization(optimizationTarget.Position) == optimizationTarget.Position)
            {
                return 0;
            }

            return 1;
            // If no closest optimization, cost = 0, else cost is distance normalized
            // If we don't penalize new positions with a high cost the solver will 
            // still pick new positions that are close but never used before.
            // I'd say this objective should penalize new positions, independently of
            // how far they are from a previous one.
        }

        public override Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout = null)
        {
            Layout result = optimizationTarget.Clone();

            result.Position = GetClosestOptimization(optimizationTarget.Position, kNearestPoints);
            return result;
        }

        public override Layout DirectRule(Layout optimizationTarget)
        {
            throw new System.NotImplementedException();
        }

        public void AdaptationUpdated(Layout adaptation)
        {
            Vector3 closestKnownPosition = GetClosestOptimization(adaptation.Position);

            // Check if position already exists
            if (closestKnownPosition == adaptation.Position)
                return;

            (int x, int y, int z)? index = mapPositionToVoxelGrid(adaptation.Position);

            if (index == null)
            {
                Debug.Log("Current adaptation position voxel out of bounds for spatial coherence solver.");
                return;
            }

            if (index == mapPositionToVoxelGrid(closestKnownPosition))
            {
                Debug.Log("Current adaptation position voxel already occupied in the past." +
                          "Hint: Increase weight of spatial coherence solver or disable it");
                return;
            }

            // ugly, but can't use foreach here
            for (int i = 0; i < voxelUsage.GetLength(0); i++)
            {
                for (int j = 0; j < voxelUsage.GetLength(1); j++)
                {
                    for (int k = 0; k < voxelUsage.GetLength(2); k++)
                    {
                        if (voxelUsage[i, j, k].score > 1)
                        {
                            voxelUsage[i, j, k].score--;
                            if (voxelUsage[i, j, k].score == 0)
                            {
                                knownOptimizationsCloud.Remove(voxelUsage[i, j, k].position);
                            }
                        }
                    }
                }
            }

            voxelUsage[index.Value.x, index.Value.y, index.Value.z] = (updatesAllowed, adaptation.Position);
            knownOptimizationsCloud.Add(adaptation.Position);
        }

        protected override void Awake()
        {
            base.Awake();
            if (adaptationManager == null)
                adaptationManager = GetComponent<AdaptationManager>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (adaptationManager != null)
                adaptationManager.RegisterAdaptationListener(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            

            if (adaptationManager != null)
                adaptationManager.UnregisterAdaptationListener(this);
        }
    }
}
