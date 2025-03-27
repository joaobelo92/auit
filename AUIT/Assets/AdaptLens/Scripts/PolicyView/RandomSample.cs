using Numpy.Models;
using Numpy;
using UnityEngine;

public class RandomSample : MonoBehaviour
{
    public static NDarray UniformSampleSimplex(int m, int n)
    {
        // Method 1: Using exponential samples and normalization
        // Generate n independent exponential random variables for each of the m samples
        // This produces a sample from the Dirichlet distribution with parameters all equal to 1

        // Create an array to hold all the exponential samples
        /*
        NDarray samples = np.zeros(new Shape(m, n));

        // For each dimension, generate m exponential samples
        System.Random random = new System.Random();
        for (int i = 0; i < m; i++)
        {
            for (int j = 0; j < n; j++)
            {
                // Generate exponential random variable with rate parameter = 1
                // The formula is -log(U) where U is uniform(0,1)
                double u = random.NextDouble();
                double exponentialSample = -System.Math.Log(u);

                // Store in our array
                samples[i, j] = np.array(exponentialSample);
            }
        }

        // Sum across each row
        NDarray rowSums = samples.sum(1).reshape(-1, 1);

        // Normalize each row by its sum to get points on the simplex
        NDarray simplexPoints = samples / rowSums;
        simplexPoints = simplexPoints.astype(np.float32);
        */

        NDarray samples = np.random.dirichlet(np.ones(n), size: new int[] { m });

        return samples.astype(np.float32);
    }
}
