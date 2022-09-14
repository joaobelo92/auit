using UnityEngine;

namespace AUIT.AdaptationObjectives.Extras
{
    public class HelperMath
    {
        // Box-Muller transform
        public static float SampleNormalDistribution(float mean, float stdDev)
        {
            Vector2 v = Random.insideUnitCircle;
            float temp1 = -2.0f * Mathf.Log(Mathf.Abs(v.x));
            float temp2 = 2.0f * Mathf.PI * Mathf.Abs(v.y);
            float randStdNormal = Mathf.Sqrt(temp1) * Mathf.Sin(temp2);
            return mean + stdDev * randStdNormal;
        }

        public static Vector3 RandomVector(float mean, float stdDev)
        {
            float x = SampleNormalDistribution(mean, stdDev);
            float y = SampleNormalDistribution(mean, stdDev);
            float z = SampleNormalDistribution(mean, stdDev);
            return new Vector3(x, y, z);
        }

        public static Vector3 MultiplyVec3(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        }
    }
}
