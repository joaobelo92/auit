using UnityEngine;

namespace AUIT.ContextSources
{
    public class CameraContextSource : ContextSource<Camera>
    {
        public Camera contextSource;

        public override Camera GetValue()
        {
            return contextSource;
        }
    }
}