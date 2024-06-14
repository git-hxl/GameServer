
using System.Diagnostics;

namespace SharedLibrary.Utils
{
    public class SystemInfo
    {
        PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        PerformanceCounter ramCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");

        public int GetCPUPercent()
        {
            return (int)cpuCounter.NextValue();
        }

        public int GetMemoryPercent()
        {
            return (int)ramCounter.NextValue();
        }

        public override string ToString()
        {
            return $"CPU: {GetCPUPercent()}% Mem: {GetMemoryPercent()}%";
        }
    }
}