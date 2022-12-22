
using System.Diagnostics;

namespace MasterServer.Utils
{
    public class SystemInfo
    {
        PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        PerformanceCounter ramCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");

        public double GetCPUPercent()
        {
            return cpuCounter.NextValue();
        }

        public double GetMemoryPercent()
        {
            return ramCounter.NextValue();
        }

        public override string ToString()
        {
            return $"CPU: {GetCPUPercent():F2}% Mem: {GetMemoryPercent():F2}%";
        }
    }
}
