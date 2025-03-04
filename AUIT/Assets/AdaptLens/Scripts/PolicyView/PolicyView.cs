using System.Collections.Generic;
using UnityEngine;
using Numpy; 

public class PolicyView : MonoBehaviour
{
    [Header("References")]
    public Parameters m_parameters;

    [Header("Settings")]
    public int m_numSamples = 10;

    public void SampleVariations()
    {
        List<Parameters.ParamReference<float>> parameters = m_parameters.GetParameters();

        int numParameters = parameters.Count;

        float[,] values = new float[numParameters, m_numSamples];
        for (int pi = 0; pi < numParameters; pi++)
        {
            Parameters.ParamReference<float> parameter = parameters[pi];
            if (parameter is Parameters.FloatParamReference)
            {
                Parameters.FloatParamReference floatParameter = (Parameters.FloatParamReference)parameter;
                float min = floatParameter.min;
                float max = floatParameter.max;
                (NDarray parameterValues, float num) = np.linspace(np.array(min), np.array(max), m_numSamples);
                for (int si = 0; si < m_numSamples; si++)
                {
                    values[pi, si] = (int)parameterValues[si];
                }
            }
        }

        // TODO: Compute optimal results given samples

    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
