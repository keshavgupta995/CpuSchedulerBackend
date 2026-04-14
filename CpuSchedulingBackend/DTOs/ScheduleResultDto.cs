namespace CpuSchedulingBackend.DTOs
{
    public class ScheduleResultDto
    {
        public List<TimeSlotDto> Timeline { get; set; } = new();
        public List<ProcessStatsDto> ProcessStats { get; set; } = new();
        public double AverageWaitingTime { get; set; }
        public double AverageTurnaroundTime { get; set; }
        public double CpuUtilization { get; set; }
    }
    public class ProcessStatsDto
    {
        public string ProcessId { get; set; } = string.Empty;
        public int WaitingTime { get; set; }
        public int TurnaroundTime { get; set; }
        public int CompletionTime { get; set; }
    }
}
