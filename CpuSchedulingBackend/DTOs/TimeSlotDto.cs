namespace CpuSchedulingBackend.DTOs
{
    public class TimeSlotDto
    {
        public string ProcessId { get; set; } = string.Empty;
        public int StartTime { get; set; }
        public int EndTime { get; set; }
    }
}
