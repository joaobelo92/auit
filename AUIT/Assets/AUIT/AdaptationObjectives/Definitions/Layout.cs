using UnityEngine;

namespace AUIT.AdaptationObjectives.Definitions
{
    public class Layout
    {
        private Vector3 position;
        public Vector3 Position
        {
            get => position;
            set => position = value;
        }

        private Quaternion rotation;
        public Quaternion Rotation
        {
            get => rotation;
            set => rotation = value;
        }

        private Vector3 scale;
        public Vector3 Scale
        {
            get => scale;
            set => scale = value;
        }

        public Layout(Transform transform)
        {
            position = transform.position;
            rotation = transform.rotation;
            scale = transform.localScale;
        }

        public Layout(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }

        public Layout Clone()
        {
            return new Layout(position, rotation, scale);
        }

        public override string ToString()
        {
            return "Position: " + position + ", Rotation: " + rotation + ", Scale: " + scale;
        }
    }
}