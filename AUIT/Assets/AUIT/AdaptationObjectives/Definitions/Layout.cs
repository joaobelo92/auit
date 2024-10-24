using System;
using Newtonsoft.Json;
using UnityEngine;

namespace AUIT.AdaptationObjectives.Definitions
{
    [Serializable]
    public class Layout
    {
        private string _id;
        [JsonProperty("id")]
        public string Id
        {
            get => _id;
            set => _id = value;
        }

        private Vector3 _position;
        [JsonProperty("position")]
        public Vector3 Position
        {
            get => _position;
            set => _position = value;
        }

        private Quaternion _rotation;
        [JsonProperty("rotation")]
        public Quaternion Rotation
        {
            get => _rotation;
            set => _rotation = value;
        }

        private Vector3 _scale;
        [JsonProperty("scale")]
        public Vector3 Scale
        {
            get => _scale;
            set => _scale = value;
        }
        
        public Layout()
        {
        }

        public Layout(string id, Transform transform)
        {
            _id = id;
            _position = transform.position;
            _rotation = transform.rotation;
            _scale = transform.localScale;
        }
        
        public Layout(string id, Vector3 position, Quaternion rotation)
        {
            _id = id;
            _position = position;
            _rotation = rotation;
        }

        public Layout(string id, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            _id = id;
            _position = position;
            _rotation = rotation;
            _scale = scale;
        }

        public Layout Clone()
        {
            return new Layout(_id, _position, _rotation, _scale);
        }

        public override string ToString()
        {
            return "Position: " + _position + ", Rotation: " + _rotation + ", Scale: " + _scale;
        }
    }
}