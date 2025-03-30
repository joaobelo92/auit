using Numpy.Models;
using Numpy;
using UnityEngine;

public class RandomSample : MonoBehaviour
{
    public static NDarray UniformSampleSimplex(int m, int n)
    {
        NDarray samples = np.random.dirichlet(np.ones(n), size: new int[] { m });

        return samples.astype(np.float32);
    }
}
