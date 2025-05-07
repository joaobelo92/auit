using System.Collections.Generic;
using System;
using UnityEngine;
using Numpy;

public class IntervalSampling : MonoBehaviour
{
    public int M;
    public float increment; 

    public static NDarray GenerateSamples(float increment, int M)
    {
        int totalUnits = (int)Math.Round(1.0 / increment);
        var compositions = new HashSet<string>();
        var results = new List<float[]>();

        GenerateCompositions(totalUnits, M, new int[M], 0, compositions, results, increment);

        // Convert results to NDArray
        int numSamples = results.Count;
        float[,] samples = new float[numSamples, M];
        for (int i = 0; i < numSamples; i++)
        {
            for (int j = 0; j < M; j++)
            {
                samples[i, j] = results[i][j];
            }
        }

        return np.array(samples).astype(np.float32);
    }

    static void GenerateCompositions(int target, int M, int[] current, int position,
        HashSet<string> seen, List<float[]> results, float increment)
    {
        if (position == M - 1)
        {
            current[position] = target;
            var key = string.Join(",", current);
            if (!seen.Contains(key))
            {
                seen.Add(key);
                var converted = new float[M];
                for (int i = 0; i < M; i++)
                {
                    converted[i] = current[i] * increment;
                }
                results.Add(converted);
            }
            return;
        }

        for (int i = 0; i <= target; i++)
        {
            current[position] = i;
            GenerateCompositions(target - i, M, current, position + 1, seen, results, increment);
        }
    }

    static int[] Sorted(int[] array)
    {
        var copy = (int[])array.Clone();
        Array.Sort(copy);
        Array.Reverse(copy); // For descending sort to match Python behavior
        return copy;
    }

    private void Start()
    {
        if (M > 0 && increment > 0)
        {
            NDarray result = GenerateSamples(increment, M);
            Debug.Log(result.shape);
        }
        
    }

}
