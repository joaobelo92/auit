using AUIT.Extras.Datastructures;
using UnityEngine;

namespace AUIT.ContextSources
{
    public class MovementContextSource : ContextSource<bool>
    {

        private FixedSizeQueue<Vector3> _positionHistory;

        public int movementHistoryBufferSize = 15;
        public float movementHistoryUpdateRate = 0.05f; // 20hz

        public int momentumUpdateRate = 5;

        private int _momentumCounter = 0;

        public float momentumIncrease = 5f;   // higher = reacts faster when moving
        public float momentumDecay = 4f;      // higher = stops faster when not moving
        public float momentumThreshold = 0.8f;  // crossing this = considered "moving"
        public float jitterDeadzone = 0.01f; // need to adjust if we change hz

        public float WalkingSpeedMetersPerSecond = 0.4f; // according to chatgpt a normal walking speed is roughly 1.2m/s

        private float _movementMomentum = 0f;

        private bool _moving = false;

        private void Start()
        {
            _positionHistory = new FixedSizeQueue<Vector3>(movementHistoryBufferSize);
            InvokeRepeating(nameof(AddCurrentPositionToQueue), 0f, movementHistoryUpdateRate);
        }

        public Transform movementContextSource;

        public override bool GetValue()
        {
            return _moving;
        }

        private void AddCurrentPositionToQueue()
        {
            if (!movementContextSource)
            {
                Debug.LogError("MovementContextSource is not set.");
                return;
            }

            _momentumCounter += 1;
            if (_momentumCounter % momentumUpdateRate == 0 && _momentumCounter > movementHistoryBufferSize)
            {
                Vector3 velocitySum = Vector3.zero;
                int velocityCount = 0;
                Vector3? prev = null;
                foreach (var pos in _positionHistory)
                {
                    if (prev.HasValue)
                    {
                        var delta = pos - prev.Value;
                        if (delta.magnitude > jitterDeadzone)
                        {
                            velocitySum += delta / movementHistoryUpdateRate;
                            velocityCount++;
                        }
                    }
                    prev = pos;
                }

                float avgSpeed = velocityCount > 0 ? velocitySum.magnitude / velocityCount : 0f;


                if (avgSpeed > WalkingSpeedMetersPerSecond)
                {
                    _movementMomentum += momentumIncrease * movementHistoryUpdateRate;
                }
                else
                {
                    _movementMomentum -= momentumDecay * movementHistoryUpdateRate;
                }


                _movementMomentum = Mathf.Clamp(_movementMomentum, 0f, 1f);
                _moving = _movementMomentum > momentumThreshold;
                
                
                // print("Average speed: " + avgSpeed + " Moving: " + _moving + " Momentum: " + _movementMomentum);

                // var totalDistance = 0f;
                // Vector3? prevPosition = null;
                // foreach (Vector3 position in _positionHistory)
                // {
                //     if (prevPosition != null)
                //     {
                //         float stepDistance = Vector3.Distance(prevPosition.Value, position);
                //         if (stepDistance > jitterDeadzone)
                //             totalDistance += stepDistance;
                //         else
                //         {
                //             totalDistance = 0;
                //             continue;
                //         }
                //     }
                //     prevPosition = position;
                // }

                // float totalTime = (movementHistoryBufferSize - 1) * movementHistoryUpdateRate;
                // float avgSpeed = totalDistance / totalTime;

                // _moving = avgSpeed > WalkingSpeedMetersPerSecond;
            }

            var currentPosition = movementContextSource.position;
            _positionHistory.Enqueue(currentPosition);
        }
    }
}
