namespace CpuSchedulingBackend.Models
{
    public class Process
    {
        public string ProcessId { get; set; } = string.Empty;
        public int ArrivalTime { get; set; }
        public int BurstTime { get; set; }
        public int Priority { get; set; }
    }
}
