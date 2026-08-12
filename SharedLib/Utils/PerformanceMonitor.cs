using System.Diagnostics;

namespace SharedLib.Utils
{

    /// <summary>
    /// 通用性能检测：CPU 使用率 + 内存占用
    /// </summary>
    public class PerformanceMonitor
    {
        private readonly Process _process;
        private TimeSpan _lastCpuTime;
        private DateTime _lastSampleTime;

        public float CpuPercent { get; private set; }
        public long MemoryMB { get; private set; }

        /// <summary>
        /// 初始化性能监控，记录当前进程的 CPU 时间基线
        /// </summary>
        public PerformanceMonitor()
        {
            _process = Process.GetCurrentProcess();
            _lastCpuTime = _process.TotalProcessorTime;
            _lastSampleTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 每次调用更新 CPU 和内存数据
        /// </summary>
        public void Update()
        {
            MemoryMB = _process.WorkingSet64 / 1024 / 1024;

            var now = DateTime.UtcNow;
            var currentCpuTime = _process.TotalProcessorTime;
            var elapsed = (now - _lastSampleTime).TotalMilliseconds;

            if (elapsed > 0)
            {
                var cpuUsed = (currentCpuTime - _lastCpuTime).TotalMilliseconds;
                CpuPercent = (float)(cpuUsed / (elapsed * Environment.ProcessorCount) * 100);
            }

            _lastCpuTime = currentCpuTime;
            _lastSampleTime = now;
        }
    }
}
