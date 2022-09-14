using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UnityStandardAssets.Utility
{
    public class FPSCounter : MonoBehaviour
    {
        [SerializeField]
        private TextMeshPro m_Text;

        [SerializeField]
        private float fpsMeasurePeriod = 0.1f;

        private int m_FpsAccumulator = 0;
        private float m_FpsNextPeriod = 0;
        private int m_CurrentFps;

        const string display = "{0} FPS";

        private string startupTimestamp;

        private void Start()
        {
            m_FpsNextPeriod = Time.realtimeSinceStartup + fpsMeasurePeriod;

            startupTimestamp = GetTimestamp();
            string data = $"SystemMillis\t fps";
            LoggingDataWriter.WriteLines(LoggingDataWriter.FileType.debug, $"fps_data_{startupTimestamp}.txt", new string[] { data });
        }


        private void Update()
        {
            // measure average frames per second
            m_FpsAccumulator++;
            if (Time.realtimeSinceStartup >= m_FpsNextPeriod)
            {
                m_CurrentFps = (int)(m_FpsAccumulator / fpsMeasurePeriod);
                m_FpsAccumulator = 0;
                m_FpsNextPeriod += fpsMeasurePeriod;
                if (m_Text != null)
                    m_Text.text = string.Format(display, m_CurrentFps);
                string data = $"{GetSystemTimeMillis()}\t {m_CurrentFps}";
                LoggingDataWriter.WriteLines(LoggingDataWriter.FileType.debug, $"fps_data_{startupTimestamp}.txt", new string[] { data });
            }
        }

        public string GetTimestamp()
        {
            return System.DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss.fff");
        }

        private long GetSystemTimeMillis()
        {
            return System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
        }
    }
}