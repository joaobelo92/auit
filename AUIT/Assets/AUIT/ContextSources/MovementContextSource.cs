using AUIT.Extras.Datastructures;
using UnityEngine;

namespace AUIT.ContextSources
{
    public class MovementContextSource : ContextSource<bool>
    {

        private FixedSizeQueue<Vector3> _positionHistory;

        public int movementHistoryBufferSize = 10;
        public float movementHistoryUpdateRate = 0.05f; // 20hz

        private float _momentum = 0f;

        public int momentumUpdateRate = 5;

        private int _momentumCounter = 0;

        public float momentumIncrease = 5f;   // higher = reacts faster when moving
        public float momentumDecay = 4f;      // higher = stops faster when not moving
        public float momentumThreshold = 1f;  // crossing this = considered "moving"
        public float jitterDeadzone = 0.01f; // need to adjust if we change hz

        public float WalkingSpeedMetersPerSecond = 0.5f; // according to chatgpt a normal walking speed is roughly 1.2m/s

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
                var totalDistance = 0f;
                Vector3? prevPosition = null;
                foreach (Vector3 position in _positionHistory)
                {
                    if (prevPosition != null)
                    {
                        float stepDistance = Vector3.Distance(prevPosition.Value, position);
                        if (stepDistance > jitterDeadzone)
                            totalDistance += stepDistance;
                        else
                        {
                            totalDistance = 0;
                            continue;
                        }
                    }
                    prevPosition = position;
                }

                float totalTime = (movementHistoryBufferSize - 1) * movementHistoryUpdateRate;
                float avgSpeed = totalDistance / totalTime;

                // if (avgSpeed > WalkingSpeedMetersPerSecond)
                //     _momentum += momentumIncrease * movementHistoryUpdateRate * momentumUpdateRate;
                // else
                //     _momentum -= momentumDecay * movementHistoryUpdateRate * momentumUpdateRate;

                // _momentum = Mathf.Clamp(_momentum, 0f, 10f);

                _moving = avgSpeed > WalkingSpeedMetersPerSecond;
                print("User is " + (_moving ? "moving" : "not moving") + " (speed: " + avgSpeed.ToString("F2") + " m/s, momentum: " + _momentum.ToString("F2") + ")");
            }

            var currentPosition = movementContextSource.position;
            _positionHistory.Enqueue(currentPosition);
        }
    }
}
