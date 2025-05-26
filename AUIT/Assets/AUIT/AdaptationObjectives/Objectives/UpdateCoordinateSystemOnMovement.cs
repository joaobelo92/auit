using AUIT.AdaptationObjectives.Definitions;
using AUIT.Extras.Datastructures;
using UnityEngine;

namespace AUIT.AdaptationObjectives.Objectives
{
    public class UpdateCoordinateSystemOnMovement : LocalObjective
    {
        private FixedSizeQueue<Vector3> _positionHistory;
    
        public int movementHistoryBufferSize = 5;
        public float movementHistoryUpdateRate = 0.2f;
    
        public float walkingSpeedThreshold = 1f; 
    
        public ContextSource<Transform> userContextSource;
    
        public CoordinateSystem coordinateSystemWhileMoving = CoordinateSystem.Torso;
        public CoordinateSystem coordinateSystemWhileNotMoving = CoordinateSystem.World;
    
        private bool _moving;
    
        protected override void Start()
        {
            base.Start();
            _positionHistory = new FixedSizeQueue<Vector3>(movementHistoryBufferSize);
            InvokeRepeating(nameof(AddCurrentPositionToQueue), 0f, movementHistoryUpdateRate);
        }

        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            if (_positionHistory.Count < movementHistoryBufferSize)
                return 0;
        
            var distance = 0f;
        
            Vector3? currentPosition = null;
            foreach (Vector3 position in _positionHistory)
            {
                if (currentPosition != null)
                {
                    distance += Vector3.Distance(currentPosition.Value, position);
                }
                currentPosition = position;
            }
        
            _moving = distance < walkingSpeedThreshold * movementHistoryBufferSize * movementHistoryUpdateRate;
            
            Debug.Log(distance);

            if (_moving) 
                return optimizationTarget.CoordinateSystem == coordinateSystemWhileMoving ? 0 : 1;
        
            return optimizationTarget.CoordinateSystem == coordinateSystemWhileNotMoving ? 0 : 1;
        }

        public override Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout = null)
        {
            return DirectRule(optimizationTarget);
        }

        public override Layout DirectRule(Layout optimizationTarget)
        {
            optimizationTarget.CoordinateSystem = _moving ? coordinateSystemWhileMoving : coordinateSystemWhileNotMoving;

            return optimizationTarget;
        }

        public override float[] GetParameters()
        {
            throw new System.NotImplementedException();
        }

        public override void SetParameters(float[] parameters)
        {
            throw new System.NotImplementedException();
        }
    
        private void AddCurrentPositionToQueue()
        {
            if (!userContextSource)
            {
                Debug.LogError("UpdateCoordinateSystemOnMovement: userContextSource is not set.");
                return;
            }
        
            var currentPosition = userContextSource.GetValue().position;
            _positionHistory.Enqueue(currentPosition);
        }
    
    }
}
