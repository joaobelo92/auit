using System;
using Newtonsoft.Json;
using UnityEngine;

namespace AUIT.AdaptationObjectives.Definitions
{
    [Serializable]
    public class Layout
    {
        [SerializeField]
        private string id;
        public string Id
        {
            get => id;
            set => id = value;
        }

        [SerializeField]
        private Vector3 position;
        public Vector3 Position
        {
            get => position;
            set => position = value;
        }

        [SerializeField]
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

        public Layout(string id, Transform transform)
        {
            this.id = id;
            position = transform.position;
            rotation = transform.rotation;
            scale = transform.localScale;
        }

        public Layout(string id, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.id = id;
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }

        public Layout Clone()
        {
            return new Layout(id, position, rotation, scale);
        }

        public override string ToString()
        {
            return "Position: " + position + ", Rotation: " + rotation + ", Scale: " + scale;
        }
    }
}