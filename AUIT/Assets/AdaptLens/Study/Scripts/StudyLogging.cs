using System;
using System.IO;
using UnityEngine;
using AUIT.AdaptationObjectives;
using AUIT.AdaptationTriggers;
using System.Collections.Generic;
using Numpy;

public class StudyLogging : MonoBehaviour
{
    public PolicyView m_policyView;

    [Serializable]
    private class Objective
    {
        public string obj;
        public string objective;
        public float[] parameters;
    }

    public enum LogEvent
    {
        PolicyViewerHover, 
        PolicyViewerSelect, 
        PolicyViewerFilter,
        TriggerAdaptation
    }

    private StreamWriter m_sw;

    private string GetDateString(DateTime dt)
    {
        return dt.ToString("MM/dd/yyyy hh:mm:ss.fff tt");
    }

    private void InitLog()
    {
        DateTime t = DateTime.Now;

        m_sw = new StreamWriter($"study_{t.ToString("MM_dd_yyyy_hh_mm_ss")}.csv", true);
    }

    private void InitLoggingHooks()
    {
        m_policyView.onHover += LogHover;
        m_policyView.onSelect += LogSelect;
        m_policyView.onFilter += LogFilter;
    }

    private void ResetLog()
    {
        if (m_sw != null)
        {
            m_sw.Close();
        }
        m_sw = null;
    }

    private (string, string) GetObjective(int pi, List<List<LocalObjective>> localObjectives, List<MultiElementObjective> multiElementObjectives)
    {
        int vi = 0;
        foreach (List<LocalObjective> objLocalObjectives in localObjectives)
        {
            foreach (LocalObjective localObjective in objLocalObjectives)
            {
                if (vi == pi)
                {
                    GameObject objectiveObj = localObjective.gameObject;
                    Type objectiveType = localObjective.GetType();
                    return (objectiveObj.name, objectiveType.ToString());
                }
                vi++;
            }
        }
        foreach (MultiElementObjective multiElementObjective in multiElementObjectives)
        {
            if (vi == pi)
            {
                GameObject objectiveObj = multiElementObjective.gameObject;
                Type objectiveType = multiElementObjective.GetType();
                return (objectiveObj.name, objectiveType.ToString());
            }
            vi++;
        }
        return (null, null);
    }

    private string GetPolicyString(List<List<LocalObjective>> localObjectives, List<MultiElementObjective> multiElementObjectives, NDarray values)
    {
        string log = "";
        int vi = 0;
        foreach (List<LocalObjective> objLocalObjectives in localObjectives)
        {
            foreach (LocalObjective localObjective in objLocalObjectives)
            {
                GameObject objectiveObj = localObjective.gameObject;
                Type objectiveType = localObjective.GetType();
                float[] parameters = localObjective.GetParameters();
                log += $"{objectiveObj.name},{objectiveType.ToString()},{(float)values[vi++]},";
                for (int pi = 1; pi < parameters.Length; pi++)
                {
                    log += $"{parameters[pi]},";
                }
            }
        }
        foreach (MultiElementObjective multiElementObjective in multiElementObjectives)
        {
            GameObject objectiveObj = multiElementObjective.gameObject;
            Type objectiveType = multiElementObjective.GetType();
            float[] parameters = multiElementObjective.GetParameters();
            log += $"{objectiveObj.name},{objectiveType.ToString()},{(float)values[vi++]},";
            for (int pi = 1; pi < parameters.Length; pi++)
            {
                log += $"{parameters[pi]},";
            }
        }

        return log;
    }

    private void LogHover(int hoverIndex, List<List<LocalObjective>> localObjectives, List <MultiElementObjective> multiElementObjectives, NDarray values)
    {
        string ts = GetDateString(DateTime.Now);
        string log = $"{ts},{LogEvent.PolicyViewerHover},{hoverIndex},{GetPolicyString(localObjectives, multiElementObjectives, values)}";
        m_sw.WriteLine(log);
    }
    
    private void LogSelect(int selectIndex, List<List<LocalObjective>> localObjectives, List<MultiElementObjective> multiElementObjectives, NDarray values)
    {
        string ts = GetDateString(DateTime.Now);
        string log = $"{ts},{LogEvent.PolicyViewerSelect},{selectIndex},{GetPolicyString(localObjectives, multiElementObjectives, values)}";
        m_sw.WriteLine(log);
    }

    private void LogFilter(int pi, float min, float max, PolicyView.SACValues filterValue, List<List<LocalObjective>> localObjectives, List<MultiElementObjective> multiElementObjectives)
    {
        string ts = GetDateString(DateTime.Now);
        var objectiveInfo = GetObjective(pi, localObjectives, multiElementObjectives);
        if (objectiveInfo.Item1 == null)
        {
            return;
        }
        if (objectiveInfo.Item2 == null)
        {
            return;
        }

        string log = $"{ts},{LogEvent.PolicyViewerFilter},{pi},{min},{max},{filterValue},{objectiveInfo.Item1},{objectiveInfo.Item2}";
        m_sw.WriteLine(log);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitLoggingHooks();
        InitLog();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnApplicationQuit()
    {
        ResetLog();
    }
}
