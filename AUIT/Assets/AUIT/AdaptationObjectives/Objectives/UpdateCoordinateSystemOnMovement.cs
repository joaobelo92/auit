using AUIT.AdaptationObjectives.Definitions;
using AUIT.ContextSources;
using AUIT.Extras.Datastructures;
using UnityEngine;

namespace AUIT.AdaptationObjectives.Objectives
{
    public class UpdateCoordinateSystemOnMovement : LocalObjective
    {
        // private FixedSizeQueue<Vector3> _positionHistory;
    
        // public int movementHistoryBufferSize = 20;
        // public float movementHistoryUpdateRate = 0.05f; // 20hz

        // private float _momentum = 0f;

        // public int momentumUpdateRate = 5;

        // private int _momentumCounter = 0;

        // public float momentumIncrease = 5f;   // higher = reacts faster when moving
        // public float momentumDecay = 4f;      // higher = stops faster when not moving
        // public float momentumThreshold = 1f;  // crossing this = considered "moving"
        // public float jitterDeadzone = 0.01f; // need to adjust if we change hz

        // public float WalkingSpeedMetersPerSecond = 0.5f; // according to chatgpt a normal walking speed is roughly 1.2m/s

        public MovementContextSource movementContextSource;
    
        public CoordinateSystem coordinateSystemWhileMoving = CoordinateSystem.Torso;
        public CoordinateSystem coordinateSystemWhileNotMoving = CoordinateSystem.World;

        // private bool _moving = false;

        public override ObjectiveType ObjectiveType => ObjectiveType.UpdateCoordinateSystemOnMovement;

        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            // if (_positionHistory.Count < movementHistoryBufferSize)
            //     return 0;

            // print("User is " + (_moving ? "moving" : "not moving"));

            // var totalDistance = 0f;

            // Vector3? prevPosition = null;
            // foreach (Vector3 position in _positionHistory)
            // {
            //     if (prevPosition != null)
            //     {
            //         float stepDistance = Vector3.Distance(prevPosition.Value, position);
            //         if (stepDistance > WalkingSpeedInCmPerSecond * movementHistoryUpdateRate / 4) // ignore jitter
            //             totalDistance += stepDistance;
            //     }
            //     prevPosition = position;
            // }

            // float totalTime = (movementHistoryBufferSize - 1) * movementHistoryUpdateRate;
            // float avgSpeed = totalDistance / totalTime;

            // // _moving = distance > walkingSpeedThreshold * (movementHistoryBufferSize-1) * movementHistoryUpdateRate;
            // _moving = avgSpeed > walkingSpeedThreshold;

            // Debug.Log(distance + " " + _moving + " " + walkingSpeedThreshold * movementHistoryBufferSize * movementHistoryUpdateRate);

            if (movementContextSource.GetValue())
            {
                return optimizationTarget.CoordinateSystem == coordinateSystemWhileMoving ? 0 : 1;
            }

            return optimizationTarget.CoordinateSystem == coordinateSystemWhileNotMoving ? 0 : 1;
        }

        public override Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout = null)
        {
            return DirectRule(optimizationTarget);
        }

        public override Layout DirectRule(Layout optimizationTarget)
        {
            optimizationTarget.CoordinateSystem = movementContextSource.GetValue() ? coordinateSystemWhileMoving : coordinateSystemWhileNotMoving;

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
    
        // private void AddCurrentPositionToQueue()
        // {
        //     if (!userContextSource)
        //     {
        //         Debug.LogError("UpdateCoordinateSystemOnMovement: userContextSource is not set.");
        //         return;
        //     }
            
        //     _momentumCounter += 1;
        //     if (_momentumCounter % momentumUpdateRate == 0 && _momentumCounter > movementHistoryBufferSize)
        //     {
        //         var totalDistance = 0f;
        //         Vector3? prevPosition = null;
        //         foreach (Vector3 position in _positionHistory)
        //         {
        //             if (prevPosition != null)
        //             {
        //                 float stepDistance = Vector3.Distance(prevPosition.Value, position);
        //                 if (stepDistance > jitterDeadzone) // filter noise
        //                     totalDistance += stepDistance;
        //             }
        //             prevPosition = position;
        //         }

        //         float totalTime = (movementHistoryBufferSize - 1) * movementHistoryUpdateRate;
        //         float avgSpeed = totalDistance / totalTime;

        //         if (avgSpeed > WalkingSpeedMetersPerSecond)
        //             _momentum += momentumIncrease * movementHistoryUpdateRate * momentumUpdateRate;
        //         else
        //             _momentum -= momentumDecay * movementHistoryUpdateRate * momentumUpdateRate;

        //         _momentum = Mathf.Clamp(_momentum, 0f, 10f);

        //         _moving = _momentum > momentumThreshold;
        //         print("User is " + (_moving ? "moving" : "not moving") + " (speed: " + avgSpeed.ToString("F2") + " m/s, momentum: " + _momentum.ToString("F2") + ")");
        //     }

        //     var currentPosition = userContextSource.GetValue().position;
        //     _positionHistory.Enqueue(currentPosition);
        // }
    
    }
}
