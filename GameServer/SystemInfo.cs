
using System.Diagnostics;

namespace GameServer
{
    internal class SystemInfo
    {
        PerformanceCounter сpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        PerformanceCounter ramCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");

        public double GetCPUPercent()
        {
            return Math.Round(сpuCounter.NextValue());
        }

        public double GetMemoryPercent()
        {
            return  Math.Round(ramCounter.NextValue());
        }
    }
}
